using System.Text.Json.Serialization;
using System.Text.Json;

namespace Medshop.Modules.Sales.Application.DTOs.Request;

public class CreateSaleRequest
{
    [JsonPropertyName("customer")]
    public CreateSaleCustomerRequest Customer { get; set; } = new();

    [JsonPropertyName("discount")]
    public decimal Discount { get; set; }

    [JsonPropertyName("tax")]
    public decimal Tax { get; set; }

    [JsonPropertyName("payment_mode")]
    public string PaymentMode { get; set; } = string.Empty;

    [JsonPropertyName("bill_date")]
    public DateTime? BillDate { get; set; }

    [JsonPropertyName("items")]
    public List<CreateSaleItemRequest> Items { get; set; } = new();
}

public class CreateSaleCustomerRequest
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("mobile")]
    public string Mobile { get; set; } = string.Empty;

    [JsonPropertyName("address")]
    public string? Address { get; set; }
}

public class CreateSaleItemRequest
{
    [JsonPropertyName("product_fk")]
    public JsonElement ProductFk { get; set; }

    [JsonPropertyName("productFk")]
    public JsonElement ProductFkCamel { get; set; }

    [JsonPropertyName("quantity")]
    public int Quantity { get; set; }
}
