# Medical Store Management API (.NET Microservice Style)

## Authentication

All endpoints require `Authorization: Bearer <token>`.

## Product APIs

- `GET /api/products/search?name=paracetamol`
  - Searches products by name for current login.
  - Returns stock, selling price, purchase price and product details.

## Customer APIs

- `GET /api/customers/search?mobile=9876543210`
  - Searches customer by mobile for current login.

## Sales APIs

- `POST /api/sales`
- `GET /api/sales`
- `GET /api/sales/{salePk}`
- `DELETE /api/sales/{salePk}`

### Create Sale Request

```json
{
  "customer": {
    "name": "Ravi",
    "mobile": "9876543210",
    "address": "Mumbai"
  },
  "discount": 20,
  "tax": 10,
  "paymentMode": "Cash",
  "items": [
    {
      "productFk": 1,
      "quantity": 2
    },
    {
      "productFk": 5,
      "quantity": 3
    }
  ]
}
```

### Create Sale Flow

1. Find customer by mobile.
2. Create customer if not exists.
3. Generate bill number: `INV000001`, `INV000002`, ...
4. Validate stock for each item.
5. Snapshot `selling_price` and `purchase_price` from product.
6. Compute subtotal and grand total.
7. Insert sale and sale items.
8. Decrease product stock.
9. Entire operation runs in DB transaction with rollback on failure.

## Sales Reports APIs

- `GET /api/reports/sales/today`
- `GET /api/reports/sales/yesterday`
- `GET /api/reports/sales/last-7-days`
- `GET /api/reports/sales/last-30-days`
- `GET /api/reports/sales/this-year`
- `GET /api/reports/sales/custom?fromDate=2026-08-01&toDate=2026-08-31`

## Dashboard API

- `GET /api/dashboard`

Returns:

- TodayPurchaseAmount
- TodaySaleAmount
- TodayProfit
- CurrentStockPurchaseValue
- CurrentStockSellingValue
- ExpectedProfit
- TotalProducts
- LowStockCount
- OutOfStockCount
- ExpiryWithin30Days
- TopSellingProducts
- RecentSales
- LowStockProducts
- OutOfStockProducts

## Formulas

- Profit per item: `(selling_price - purchase_price) * quantity`
- Purchase stock value: `SUM(stock * purchase_price)`
- Selling stock value: `SUM(stock * selling_price)`
- Expected profit: `selling stock value - purchase stock value`
