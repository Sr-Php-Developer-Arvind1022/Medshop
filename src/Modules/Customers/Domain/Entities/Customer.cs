namespace Medshop.Modules.Customers.Domain.Entities;

public class Customer
{
    public long CustomerIdPk { get; set; }
    public Guid Id { get; set; }
    public Guid LoginId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Mobile { get; set; } = string.Empty;
    public string? Address { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
