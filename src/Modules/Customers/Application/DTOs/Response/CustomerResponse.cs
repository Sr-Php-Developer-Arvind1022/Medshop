namespace Medshop.Modules.Customers.Application.DTOs.Response;

public class CustomerResponse
{
    public long CustomerPk { get; set; }
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Mobile { get; set; } = string.Empty;
    public string? Address { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
