using System.ComponentModel.DataAnnotations;

namespace PostService.Dtos
{
    public class PostDtoToGet
    {
        [Required]
        public string Content { get; set; } = null!;

        public IFormFile? Image { get; set; } = null!;
    }
}
