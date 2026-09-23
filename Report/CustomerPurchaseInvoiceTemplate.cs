using System.Globalization;
using System.Text;
using Medshop.Modules.Sales.Application.DTOs.Response;

namespace Medshop.Report;

public static class CustomerPurchaseInvoiceTemplate
{
    public static string Build(SaleResponse sale, ShopInvoiceDetails shop)
    {
        ArgumentNullException.ThrowIfNull(sale);
        ArgumentNullException.ThrowIfNull(shop);

        var itemRows = sale.Items.Any()
            ? sale.Items.Select(item =>
                $"<tr><td>{EscapeHtml(item.ProductName)}</td><td>{item.Quantity}</td><td>{FormatCurrency(item.Price)}</td><td>{FormatCurrency(item.Total)}</td></tr>")
                .ToList()
            : new List<string> { "<tr><td colspan='4'>No items found.</td></tr>" };

        var shopLocation = string.IsNullOrWhiteSpace(shop.State)
            ? shop.City
            : $"{shop.City}, {shop.State}";
        var customerAddress = sale.Customer.Address ?? "N/A";

        var builder = new StringBuilder();
        builder.AppendLine("<!DOCTYPE html>");
        builder.AppendLine("<html lang='en'>");
        builder.AppendLine("<head>");
        builder.AppendLine("  <meta charset='utf-8' />");
        builder.AppendLine("  <title>Invoice</title>");
        builder.AppendLine("  <style>");
        builder.AppendLine("    body { font-family: Arial, sans-serif; margin: 24px; color: #1f2937; }");
        builder.AppendLine("    .invoice-box { max-width: 900px; margin: 0 auto; border: 1px solid #e5e7eb; padding: 30px; border-radius: 10px; }");
        builder.AppendLine("    .header { display: flex; justify-content: space-between; align-items: flex-start; border-bottom: 2px solid #d1d5db; padding-bottom: 18px; margin-bottom: 20px; }");
        builder.AppendLine("    .shop-name { font-size: 28px; font-weight: 700; margin-bottom: 6px; }");
        builder.AppendLine("    .meta { font-size: 13px; line-height: 1.6; color: #4b5563; }");
        builder.AppendLine("    .invoice-title { font-size: 28px; font-weight: 700; letter-spacing: 1px; text-align: right; }");
        builder.AppendLine("    .section { margin-top: 20px; }");
        builder.AppendLine("    .section-title { font-size: 13px; font-weight: 700; color: #374151; text-transform: uppercase; letter-spacing: 0.08em; margin-bottom: 8px; }");
        builder.AppendLine("    .details-grid { display: grid; grid-template-columns: 1fr 1fr; gap: 20px; }");
        builder.AppendLine("    .card { border: 1px solid #e5e7eb; border-radius: 8px; padding: 14px; background: #f9fafb; }");
        builder.AppendLine("    table { width: 100%; border-collapse: collapse; margin-top: 18px; }");
        builder.AppendLine("    th, td { border: 1px solid #e5e7eb; padding: 10px 12px; text-align: left; }");
        builder.AppendLine("    th { background: #f3f4f6; font-size: 12px; text-transform: uppercase; letter-spacing: 0.05em; }");
        builder.AppendLine("    .totals { width: 300px; margin-left: auto; margin-top: 18px; border: 1px solid #e5e7eb; border-radius: 8px; overflow: hidden; }");
        builder.AppendLine("    .totals-row { display: flex; justify-content: space-between; padding: 10px 12px; border-bottom: 1px solid #e5e7eb; }");
        builder.AppendLine("    .grand-total { background: #111827; color: #fff; font-weight: 700; }");
        builder.AppendLine("  </style>");
        builder.AppendLine("</head>");
        builder.AppendLine("<body>");
        builder.AppendLine("  <div class='invoice-box'>");
        builder.AppendLine("    <div class='header'>");
        builder.AppendLine("      <div>");
        builder.AppendLine($"        <div class='shop-name'>{EscapeHtml(shop.Name)}</div>");
        builder.AppendLine("        <div class='meta'>");
        builder.AppendLine($"          {EscapeHtml(shop.Address)}<br />");
        builder.AppendLine($"          {EscapeHtml(shopLocation)}<br />");
        builder.AppendLine($"          Phone: {EscapeHtml(shop.Phone)}<br />");
        builder.AppendLine($"          Email: {EscapeHtml(shop.Email)}<br />");
        builder.AppendLine($"          GST ID: {EscapeHtml(shop.GstId)}");
        builder.AppendLine("        </div>");
        builder.AppendLine("      </div>");
        builder.AppendLine("      <div class='invoice-title'>INVOICE</div>");
        builder.AppendLine("    </div>");
        builder.AppendLine("    <div class='details-grid'>");
        builder.AppendLine("      <div class='card'>");
        builder.AppendLine("        <div class='section-title'>Customer Details</div>");
        builder.AppendLine("        <div class='meta'>");
        builder.AppendLine($"          <strong>{EscapeHtml(sale.Customer.Name)}</strong><br />");
        builder.AppendLine($"          Phone: {EscapeHtml(sale.Customer.Mobile)}<br />");
        builder.AppendLine($"          Address: {EscapeHtml(customerAddress)}<br />");
        builder.AppendLine("        </div>");
        builder.AppendLine("      </div>");
        builder.AppendLine("      <div class='card'>");
        builder.AppendLine("        <div class='section-title'>Invoice Details</div>");
        builder.AppendLine("        <div class='meta'>");
        builder.AppendLine($"          Invoice No: <strong>{EscapeHtml(sale.BillNo)}</strong><br />");
        builder.AppendLine($"          Date: {sale.BillDate:dd-MM-yyyy}<br />");
        builder.AppendLine($"          Payment Mode: {EscapeHtml(sale.PaymentMode)}");
        builder.AppendLine("        </div>");
        builder.AppendLine("      </div>");
        builder.AppendLine("    </div>");
        builder.AppendLine("    <div class='section'>");
        builder.AppendLine("      <div class='section-title'>Items</div>");
        builder.AppendLine("      <table>");
        builder.AppendLine("        <thead>");
        builder.AppendLine("          <tr>");
        builder.AppendLine("            <th>Product Name</th>");
        builder.AppendLine("            <th>Purchased Quantity</th>");
        builder.AppendLine("            <th>Selling Price</th>");
        builder.AppendLine("            <th>Item Total</th>");
        builder.AppendLine("          </tr>");
        builder.AppendLine("        </thead>");
        builder.AppendLine("        <tbody>");
        foreach (var row in itemRows)
        {
            builder.AppendLine(row);
        }
        builder.AppendLine("        </tbody>");
        builder.AppendLine("      </table>");
        builder.AppendLine("    </div>");
        builder.AppendLine("    <div class='totals'>");
        builder.AppendLine("      <div class='totals-row'>");
        builder.AppendLine($"        <span>Subtotal</span><span>{FormatCurrency(sale.Subtotal)}</span>");
        builder.AppendLine("      </div>");
        builder.AppendLine("      <div class='totals-row'>");
        builder.AppendLine($"        <span>Discount</span><span>-{FormatCurrency(sale.Discount)}</span>");
        builder.AppendLine("      </div>");
        builder.AppendLine("      <div class='totals-row'>");
        builder.AppendLine($"        <span>Tax</span><span>{FormatCurrency(sale.Tax)}</span>");
        builder.AppendLine("      </div>");
        builder.AppendLine("      <div class='totals-row grand-total'>");
        builder.AppendLine($"        <span>Grand Total</span><span>{FormatCurrency(sale.GrandTotal)}</span>");
        builder.AppendLine("      </div>");
        builder.AppendLine("    </div>");
        builder.AppendLine("  </div>");
        builder.AppendLine("</body>");
        builder.AppendLine("</html>");

        return builder.ToString();
    }

    public static string BuildPlainText(SaleResponse sale, ShopInvoiceDetails shop)
    {
        var itemLines = sale.Items.Select(item =>
            $"- {item.ProductName} | Qty: {item.Quantity} | Unit Price: {FormatCurrency(item.Price)} | Total: {FormatCurrency(item.Total)}");

        return string.Join(Environment.NewLine, new[]
        {
            shop.Name,
            $"{shop.Address}, {shop.City}",
            $"Phone: {shop.Phone} | Email: {shop.Email} | GST ID: {shop.GstId}",
            string.Empty,
            $"Invoice No: {sale.BillNo}",
            $"Date: {sale.BillDate:dd-MM-yyyy}",
            $"Customer: {sale.Customer.Name} | Phone: {sale.Customer.Mobile}",
            $"Address: {sale.Customer.Address ?? "N/A"}",
            string.Empty,
            "Items:",
            string.Join(Environment.NewLine, itemLines),
            string.Empty,
            $"Subtotal: {FormatCurrency(sale.Subtotal)}",
            $"Discount: -{FormatCurrency(sale.Discount)}",
            $"Tax: {FormatCurrency(sale.Tax)}",
            $"Grand Total: {FormatCurrency(sale.GrandTotal)}"
        });
    }

    private static string EscapeHtml(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? string.Empty : System.Net.WebUtility.HtmlEncode(value);
    }

    private static string FormatCurrency(decimal value)
    {
        return value.ToString("C", new CultureInfo("en-IN"));
    }
}

public class ShopInvoiceDetails
{
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string GstId { get; set; } = string.Empty;
}