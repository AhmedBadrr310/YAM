namespace PostService.Responses
{
    public class ApiResponse<T> 
    {
        public int Code { get; set; }
        public string Message { get; set; } = null!;
        public T Data { get; set; }
    }
}
