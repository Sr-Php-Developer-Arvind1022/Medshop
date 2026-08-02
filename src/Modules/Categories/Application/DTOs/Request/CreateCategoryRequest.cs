using Microsoft.AspNetCore.Http;

namespace Medshop.Modules.Categories.Application.DTOs.Request;

public class CreateCategoryRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public IFormFile? CategoryImage { get; set; }
}
