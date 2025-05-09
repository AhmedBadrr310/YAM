using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Yam.NotificationService.Core.Interfaces;

namespace Yam.NotificationService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MailController(IMailService mailService) : ControllerBase
    {
        private readonly IMailService _mailService = mailService;

        [Authorize]
        [HttpPost("send-verification-mail")]
        public async Task<IActionResult> SendVerificationEmail([FromQuery]string verificationCode)
        {
            var identity = HttpContext.User.Identity as ClaimsIdentity;
            if (identity == null || !identity.IsAuthenticated)
            {
                return Unauthorized();
            }
            var email = identity.FindFirst(ClaimTypes.Email)?.Value;
            await _mailService.SendVerificationMail(email!, verificationCode);
            return Ok();
        }
    }
}
