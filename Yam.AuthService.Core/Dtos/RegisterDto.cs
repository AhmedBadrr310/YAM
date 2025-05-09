using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Yam.AuthService.Core.Dtos
{
    public class RegisterDto
    {
        [Required(ErrorMessage = "Username is required")]
        public string Username { get; set; } = null!;
        
        [Required(ErrorMessage = "Display name is required")]
        public string DisplayName { get; set; } = null!;
        
        [Required(ErrorMessage = "Email is required")]
        public string Email { get; set; } = null!;

        public IReadOnlyList<byte> ProfilePicture { get; set; } = new List<byte>();
        
        [Required(ErrorMessage = "Password is required")]
        public string Password { get; set; } = null!;
        
        [Required(ErrorMessage = "Gender is required")]
        public string? Gender { get; set; }
        
        [Required(ErrorMessage = "Birth date is required")]
        public DateOnly BirthDate { get; set; }

        [JsonIgnore]
        public DateOnly JoinDate => DateOnly.FromDateTime(DateTime.UtcNow);

        public string? Bio { get; set; }
        
    }
}
