using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Yam.AuthService.Core.Dtos
{
    public class LoginDto
    {
        [Required(ErrorMessage = "Username or email is required")]
        public string? EmailOrUser { get; set; }

        [Required(ErrorMessage = "Password is required")]
        public string? Password { get; set; }
    }
}
