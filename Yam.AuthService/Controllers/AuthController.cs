using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Yam.AuthService.Core.Dtos;
using Yam.AuthService.Core.Interfaces;
using Yam.AuthService.Responses;
using Yam.AuthService.Services;
using Yam.Core.sql.Entities;

namespace Yam.AuthService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController(IAuthService service, IMapper mapper, UserManager<ApplicationUser> userManager) : ControllerBase
    {
        private readonly IAuthService _service = service;
        private readonly IMapper _mapper = mapper;

        [HttpPost("register")]
        public async Task<ActionResult<ApiResponse>> Register([FromBody]RegisterDto mdl)
        {
            var user = _mapper.Map<ApplicationUser>(mdl);
            var result = await _service.RegisterAsync(user, mdl.Password);
           
            if (result.Code != 200)
                return StatusCode(result.Code, result);
            else
            {
                var cookiesOptions = new CookieOptions
                {
                    HttpOnly = true,
                    Expires = result.RefreshTokenExpirationDate
                };
                Response.Cookies.Append("refreshToken", result.RefreshToken, cookiesOptions);
                return Ok(result);
            }
        }

        [HttpPost("login")]
        public async Task<ActionResult<ApiResponse>> Login([FromBody] LoginDto mdl)
        {
            var result = await _service.LoginAsync(mdl, mdl.Password);

            if (result.Code != 200)
                return StatusCode(result.Code, result);
            else
            {
                var cookiesOptions = new CookieOptions
                {
                    HttpOnly = true,
                    Expires = result.RefreshTokenExpirationDate
                };
                Response.Cookies.Append("refreshToken", result.RefreshToken, cookiesOptions);
                return Ok(result);
            }
        }

        [HttpPost("send-code")]
        public async Task<ActionResult<ApiResponse>> SendVerificationCode([FromQuery] string usernameOrEmail)
        {
            var result = await _service.SendVerificationCodeAsync(usernameOrEmail);
            if (result.Code != 200)
                return StatusCode(result.Code, result);
            else
                return Ok(result);
        }

        [Authorize(AuthenticationSchemes = "Bearer")]
        [HttpPost("verify")]
        public async Task<ActionResult<ApiResponse>> VerifyCode([FromQuery]string code)
        {
            var identity = HttpContext.User.Identity as ClaimsIdentity;
            if (identity == null || !identity.IsAuthenticated)
            {
                return Unauthorized();
            }
            var userId = identity.FindFirst("user-id")?.Value;
            var result = await _service.VerifyCodeAsync(userId, code);
            if (result.Code != 200)
                return StatusCode(result.Code, result);
            else
                return Ok(result);
        }

        [Authorize(AuthenticationSchemes = "Bearer")]
        [HttpPost("reset-password")]
        public async Task<ActionResult<ApiResponse>> ChangePassword([FromBody] ResetPasswordDto mdl)
        {
            var identity = HttpContext.User.Identity as ClaimsIdentity;
            var userId = identity!.FindFirst("user-id")?.Value;
            if(mdl.Password != mdl.ConfirmPassword)
                return BadRequest("Passwords do not match");

            var result = await _service.ChangePasswordAsync(userId, mdl.Password);
            return StatusCode(result.Code, result);
        }

        [HttpGet]
        public IEnumerable<string> Get()
        {
            return new string[] { "value1", "value2" };
        }

    }
}
