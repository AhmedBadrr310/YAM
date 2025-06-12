using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Neo4jClient;
using StackExchange.Redis;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Yam.AuthService.Core.Dtos;
using Yam.AuthService.Core.Interfaces;
using Yam.AuthService.Responses;
using Yam.Core.sql;
using Yam.Core.sql.Entities;
using Yam.Core.neo4j.Entities;

namespace Yam.AuthService.Services
{
    public class AuthServices(UserManager<ApplicationUser> userManager, IConfiguration config, ILogger logger, ApplicationDbContext dbContext, IConnectionMultiplexer redis, IGraphClient graph) : IAuthService
    {
        private readonly UserManager<ApplicationUser> _userManager = userManager;
        private readonly IConfiguration config = config;
        private readonly ILogger _logger = logger;
        private readonly ApplicationDbContext _dbContext = dbContext;
        private readonly IGraphClient _graph = graph;
        private readonly IDatabase _redis = redis.GetDatabase();



        public async Task<string> GenerateToken(ApplicationUser user)
        {
            var claims = new List<Claim>();
            var roles = await _userManager.GetRolesAsync(user);

            foreach (var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            claims.AddRange(new List<Claim>()
            {
                new Claim(ClaimTypes.NameIdentifier, user.UserName!),
                new Claim(ClaimTypes.Email, user.Email!),
                new Claim("user-id", user.Id)
            });
            var symmetricKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config["jwt:secretKey"]!));
            var signingCredentials = new SigningCredentials(symmetricKey, SecurityAlgorithms.HmacSha256);

            var jwtToken = new JwtSecurityToken(
                issuer: config["jwt:issuer"],
                audience: config["jwt:audience"],
                claims: claims,
                expires: DateTime.Now.AddMinutes(double.Parse(config["jwt:durationInMinutes"]!)),
                signingCredentials: signingCredentials
            );

            return new JwtSecurityTokenHandler().WriteToken(jwtToken);
        }

        public async Task<ApiResponse> LoginAsync(LoginDto dto, string password)
        {
            var isEmail = dto.EmailOrUser.Contains("@");
            ApplicationUser? matchedUser;
            if (isEmail)
            {
                matchedUser = await _userManager.FindByEmailAsync(dto.EmailOrUser);
                
            }
            else
            {
                matchedUser = await _userManager.FindByNameAsync(dto.EmailOrUser);
            }
            if (matchedUser is null || !await _userManager.CheckPasswordAsync(matchedUser, password))
            {
                return new ApiResponse
                {
                    Code = 401,
                    Messasge = "Invalid credentials"
                };
            }

            var token = await GenerateToken(matchedUser);
            var activeRefreshToken = matchedUser.RefreshTokens.FirstOrDefault(u => u.IsActive);
            var refreshToken = GenerateRefreshToken();

            if (activeRefreshToken == null)
            {
                matchedUser.RefreshTokens.Add(refreshToken);
                _dbContext.SaveChangesAsync();
            }
            else
            {
                activeRefreshToken.RevokedAt = DateTime.UtcNow;
                _dbContext.SaveChangesAsync();
            }

            return new ApiResponse
            {
                Code = 200,
                Messasge = "Login successful",
                UserId = matchedUser.Id,
                Token = token,
                ExpirationDate = DateTime.UtcNow.AddMinutes(double.Parse(config["jwt:durationInMinutes"]!)),
                RefreshToken = refreshToken.Token,
                RefreshTokenExpirationDate = refreshToken.ExpiresAt
            };
        }

        public async Task<ApiResponse> RegisterAsync(ApplicationUser user, string password)
        {
            var tasks = new[]
            {
                _userManager.FindByEmailAsync(user.Email!),
                _dbContext.Users.FirstOrDefaultAsync(u => u.UserName == user.UserName)
            };

            await Task.WhenAll(tasks);

            // Check results
            if (tasks[0].Result != null)
            {
                return new ApiResponse
                {
                    Code = 409,
                    Messasge = "Email already exists"
                };
            }

            if (tasks[1].Result != null)
            {
                return new ApiResponse
                {
                    Code = 409,
                    Messasge = "Username already exists"
                };
            }

            // 3. Prepare refresh token during user creation to avoid extra DB call later
            var refreshToken = GenerateRefreshToken();
            user.RefreshTokens = new List<RefreshToken> { refreshToken };

            // 4. Create user
            var result = await _userManager.CreateAsync(user, password);

            // 5. Check for errors
            if (!result.Succeeded)
            {
                return new ApiResponse
                {
                    Code = 400,
                    Messasge = string.Join("\n", result.Errors.Select(e => $"{e.Code} : {e.Description}"))
                };
            }


            // 7. Generate token concurrently with role assignment if possible
            var tokenTask = GenerateToken(user);

            // 8. Build response
            double expirationMinutes = double.Parse(config["jwt:durationInMinutes"]!);

            _graph.Cypher.Create("(u:User $param)")
                .WithParam("param", new User()
                {
                    UserId = user.Id,
                    Username = user.UserName,
                    Email = user.Email
                })
                .ExecuteWithoutResultsAsync();

            return new ApiResponse
            {
                Code = 200,
                Messasge = "User created successfully",
                UserId = user.Id,
                Token = await tokenTask,
                ExpirationDate = DateTime.UtcNow.AddMinutes(expirationMinutes),
                RefreshToken = refreshToken.Token,
                RefreshTokenExpirationDate = refreshToken.ExpiresAt
            };
        }

        public async Task<ApiResponse> SendVerificationCodeAsync(string usernameOrEmail)
        {
            var matchedUser = await _dbContext.Users.FirstOrDefaultAsync(u => u.NormalizedEmail == usernameOrEmail.ToUpper() || u.NormalizedUserName == usernameOrEmail.ToUpper());
            if (matchedUser == null)
            {
                return new ApiResponse
                {
                    Code = 404,
                    Messasge = "User not found"
                };
            }

            var verificationCode = Guid.NewGuid().ToString().Substring(0, 6);



            var result = _redis.StringSet(matchedUser.Id, verificationCode, TimeSpan.FromMinutes(10));

            if (!result)
            {
                return new ApiResponse
                {
                    Code = 500,
                    Messasge = "Failed to store verification code in Redis"
                };

            }

            var token = await GenerateToken(matchedUser);

            try
            {
                HttpClient client = new HttpClient();

                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
                    "Bearer", token);
                
                var content = new StringContent(
                    "",
                    Encoding.UTF8,
                    "application/json"
                );

                // Basic GET request
                client.PostAsync($"http://yam-notification.runasp.net/api/Mail/send-verification-mail?verificationCode={verificationCode}",content);
                
                // If you need to parse JSON
                // Use System.Text.Json or Newtonsoft.Json to deserialize
                // var data = JsonSerializer.Deserialize<YourModel>(responseBody);
            }
            catch (HttpRequestException e)
            {
                _logger.LogError($"Request error: {e.Message}");
            }

            return new ApiResponse
            {
                Code = 200,
                Messasge = "Verification code sent successfully",
                Token = token,
                UserId = matchedUser.Id
            };
        }

        public async Task<ApiResponse> VerifyCodeAsync(string userId, string code)
        {
            var matchedUser = await _userManager.FindByIdAsync(userId);

            if (matchedUser == null)
            {
                return new ApiResponse
                {
                    Code = 404,
                    Messasge = "User not found"
                };
            }

            //get the verification code from redis
            var result = await _redis.StringGetAsync(userId);

            //check if the verification code exists and valid
            if (result.IsNullOrEmpty || result.ToString() != code)
            {
                return new ApiResponse
                {
                    Code = 400,
                    Messasge = "Invalid verification code"
                };
            }

            var token = await GenerateToken(matchedUser);
            matchedUser.EmailConfirmed = true; //if the endpoint was used for verification
            await _userManager.UpdateAsync(matchedUser);

            await _redis.StringSetAsync(userId, "verified", TimeSpan.FromMinutes(10)); //if the endpoint was used for changing the password it gives the user 10 mins to change it

            return new ApiResponse
            {
                Code = 200,
                Messasge = "User verified",
                UserId = matchedUser.Id,
                Token = token,
                ExpirationDate = DateTime.UtcNow.AddMinutes(double.Parse(config["jwt:durationInMinutes"]!))
            };

        }

        public async Task<ApiResponse> ChangePasswordAsync(string userId, string newPassword)
        {
            //1-see if the user is verified
            var isverifed = await _redis.StringGetAsync(userId);

            if (isverifed.IsNullOrEmpty || isverifed != "verified")
            {
                return new ApiResponse
                {
                    Code = 400,
                    Messasge = "User not verified"
                };
            }
            //2-get the user
            var matchedUser = await _userManager.FindByIdAsync(userId);
            //3-check if the user exists
            if (matchedUser == null)
            {
                return new ApiResponse
                {
                    Code = 404,
                    Messasge = "User not found"
                };
            }
            //4-check if the password is valid
            var reserToken = await _userManager.GeneratePasswordResetTokenAsync(matchedUser);
            var result = await _userManager.ResetPasswordAsync(matchedUser, reserToken, newPassword);
            //6-check if the update was successful
            if (!result.Succeeded)
            {
                return new ApiResponse
                {
                    Code = 400,
                    Messasge = string.Join("\n", result.Errors.Select(e => $"{e.Code} : {e.Description}"))
                };
            }
            //7-delete the verification code from redis
            await _redis.KeyDeleteAsync(userId);
            
            //8- return response
            return new ApiResponse
            {
                Code = 200,
                Messasge = "Password changed successfully",
                UserId = matchedUser.Id,
                Token = await GenerateToken(matchedUser),
                ExpirationDate = DateTime.UtcNow.AddMinutes(double.Parse(config["jwt:durationInMinutes"]!))
            };
        }

        private RefreshToken GenerateRefreshToken()
        {
            var randomNumber = new byte[32];

            using var generator = new RNGCryptoServiceProvider();

            generator.GetBytes(randomNumber);

            return new RefreshToken()
            {
                Token = Convert.ToBase64String(randomNumber),
                ExpiresAt = DateTime.UtcNow.AddDays(7),
                CreatedAt = DateTime.UtcNow,
                RevokedAt = null
            };
        }
    }
}
