using Microsoft.AspNetCore.Http;

namespace Medshop.Modules.Categories.Application.DTOs.Request;

public class UpdateCategoryRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public IFormFile? CategoryImage { get; set; }
}
