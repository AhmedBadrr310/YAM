using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace PostService.Dtos
{
    public class GetPostsParamsDto
    {
       [RegularExpression(@"asc|desc",ErrorMessage = "Value must be either asc or desc")]
       public string? sort { get; set; }

       public string? search { get; set; }

       public int pageNumber { get; set; } = 1; // Default to page 1

       public int pageSize { get; set; } = 10;
    }
}
