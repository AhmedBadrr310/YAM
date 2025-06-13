namespace PostService.Dtos
{
    public class PostDtoToGet
    {
        public string Content { get; set; } = null!;

        public IFormFile Image { get; set; } = null!;
    }
}
