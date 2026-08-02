namespace Medshop.Modules.Categories.Domain.Entities;

public class Category
{
    public long CategoryIdPk { get; set; }
    public Guid Id { get; set; }
    public Guid LoginId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? CategoryImage { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
