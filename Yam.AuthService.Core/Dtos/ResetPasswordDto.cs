using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Yam.AuthService.Core.Dtos
{
    public class ResetPasswordDto
    {
        public string Password { get; set; }

        public string ConfirmPassword { get; set; }
    }
}
