using Yam.AuthService.Core.Dtos;
using Yam.AuthService.Responses;
using Yam.Core.sql.Entities;

namespace Yam.AuthService.Core.Interfaces
{
    public interface IAuthService
    {
        Task<ApiResponse> RegisterAsync(ApplicationUser user, string password);

        Task<string> GenerateToken(ApplicationUser user);

        Task<ApiResponse> LoginAsync(LoginDto dto, string password);

        Task<ApiResponse> SendVerificationCodeAsync(string usernameOrEmail);

        Task<ApiResponse> VerifyCodeAsync(string userId, string code);

        Task<ApiResponse> ChangePasswordAsync(string token, string refreshToken); 
    }
}
