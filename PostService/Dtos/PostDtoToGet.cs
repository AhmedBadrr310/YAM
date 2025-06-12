namespace PostService.Dtos
{
    public class PostDtoToGet
    {
        public string Content { get; set; } = null!;

        public List<byte>? Image { get; set; }
    }
}
