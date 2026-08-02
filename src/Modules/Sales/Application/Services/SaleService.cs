using AutoMapper;
using FluentValidation;
using Medshop.BuildingBlocks.Common;
using Medshop.Modules.Customers.Domain.Entities;
using Medshop.Modules.Customers.Domain.Interfaces;
using Medshop.Modules.Identity.Persistence;
using Medshop.Modules.Products.Domain.Interfaces;
using Medshop.Modules.Sales.Application.DTOs.Request;
using Medshop.Modules.Sales.Application.DTOs.Response;
using Medshop.Modules.Sales.Application.Interfaces;
using Medshop.Modules.Sales.Domain.Entities;
using Medshop.Modules.Sales.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Medshop.Modules.Sales.Application.Services;

public class SaleService : ISaleService
{
    private readonly ISaleRepository _saleRepository;
    private readonly ICustomerRepository _customerRepository;
    private readonly IProductRepository _productRepository;
    private readonly IMapper _mapper;
    private readonly IValidator<CreateSaleRequest> _createValidator;
    private readonly IValidator<GetSalesRequest> _getSalesValidator;
    private readonly IValidator<CustomSalesReportRequest> _customReportValidator;
    private readonly MedshopDbContext _context;

    public SaleService(
        ISaleRepository saleRepository,
        ICustomerRepository customerRepository,
        IProductRepository productRepository,
        IMapper mapper,
        IValidator<CreateSaleRequest> createValidator,
        IValidator<GetSalesRequest> getSalesValidator,
        IValidator<CustomSalesReportRequest> customReportValidator,
        MedshopDbContext context)
    {
        _saleRepository = saleRepository;
        _customerRepository = customerRepository;
        _productRepository = productRepository;
        _mapper = mapper;
        _createValidator = createValidator;
        _getSalesValidator = getSalesValidator;
        _customReportValidator = customReportValidator;
        _context = context;
    }

    public async Task<SaleResponse> CreateAsync(CreateSaleRequest request, Guid loginId, CancellationToken cancellationToken)
    {
        await _createValidator.ValidateAndThrowAsync(request, cancellationToken);

        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            var customer = await EnsureCustomerAsync(request, loginId, cancellationToken);
            var billNo = await _saleRepository.GenerateNextBillNoAsync(loginId, cancellationToken);

            var saleItems = new List<SaleItem>();
            decimal subtotal = 0;

            foreach (var itemRequest in request.Items)
            {
                var product = await ResolveProductForSaleAsync(itemRequest, loginId, cancellationToken);
                if (product is null)
                {
                    throw new KeyNotFoundException("Product not found for provided product reference.");
                }

                if (product.StockQuantity < itemRequest.Quantity)
                {
                    throw new InvalidOperationException($"Insufficient stock for product '{product.Name}'.");
                }

                var itemTotal = product.SellingPrice * itemRequest.Quantity;
                subtotal += itemTotal;

                saleItems.Add(new SaleItem
                {
                    ProductFk = product.ProductIdPk,
                    Quantity = itemRequest.Quantity,
                    Price = product.SellingPrice,
                    PurchasePrice = product.PurchasePrice,
                    Total = itemTotal,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                });

                product.StockQuantity -= itemRequest.Quantity;
                await _productRepository.UpdateAsync(product, cancellationToken);
            }

            var sale = new Sale
            {
                Id = Guid.NewGuid(),
                LoginId = loginId,
                CustomerFk = customer.CustomerIdPk,
                BillNo = billNo,
                Subtotal = subtotal,
                Discount = request.Discount,
                Tax = request.Tax,
                GrandTotal = subtotal - request.Discount + request.Tax,
                PaymentMode = request.PaymentMode,
                BillDate = (request.BillDate ?? DateTime.UtcNow).Date,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _saleRepository.AddAsync(sale, cancellationToken);

            foreach (var saleItem in saleItems)
            {
                saleItem.SaleFk = sale.SaleIdPk;
            }

            await _saleRepository.AddItemsAsync(saleItems, cancellationToken);

            await transaction.CommitAsync(cancellationToken);

            var createdSale = await _saleRepository.GetByIdAndLoginIdAsync(sale.SaleIdPk, loginId, cancellationToken)
                ?? throw new KeyNotFoundException("Created sale not found.");

            return _mapper.Map<SaleResponse>(createdSale);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<PagedResult<SaleResponse>> GetPagedAsync(GetSalesRequest request, Guid loginId, CancellationToken cancellationToken)
    {
        await _getSalesValidator.ValidateAndThrowAsync(request, cancellationToken);

        var pageNumber = request.PageNumber <= 0 ? 1 : request.PageNumber;
        var pageSize = request.PageSize <= 0 ? 10 : Math.Min(request.PageSize, 100);

        var (items, totalCount) = await _saleRepository.GetPagedAsync(
            pageNumber,
            pageSize,
            request.Search,
            request.PaymentMode,
            request.CustomerMobile,
            request.FromDate,
            request.ToDate,
            loginId,
            cancellationToken);

        return new PagedResult<SaleResponse>
        {
            Items = _mapper.Map<IReadOnlyCollection<SaleResponse>>(items),
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = totalCount == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)pageSize)
        };
    }

    public async Task<SaleResponse> GetByIdAsync(long saleIdPk, Guid loginId, CancellationToken cancellationToken)
    {
        var sale = await _saleRepository.GetByIdAndLoginIdAsync(saleIdPk, loginId, cancellationToken);
        if (sale is null)
        {
            throw new KeyNotFoundException("Sale not found for this login id.");
        }

        return _mapper.Map<SaleResponse>(sale);
    }

    public async Task SoftDeleteAsync(long saleIdPk, Guid loginId, CancellationToken cancellationToken)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            var sale = await _saleRepository.GetByIdAndLoginIdAsync(saleIdPk, loginId, cancellationToken);
            if (sale is null)
            {
                throw new KeyNotFoundException("Sale not found for this login id.");
            }

            foreach (var item in sale.Items)
            {
                var product = await _productRepository.GetByPrimaryKeyAndLoginIdAsync(item.ProductFk, loginId, cancellationToken);
                if (product is not null)
                {
                    product.StockQuantity += item.Quantity;
                    await _productRepository.UpdateAsync(product, cancellationToken);
                }
            }

            await _saleRepository.DeleteAsync(sale, cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public Task<SalesReportResponse> GetTodayReportAsync(Guid loginId, CancellationToken cancellationToken)
    {
        var from = DateTime.UtcNow.Date;
        var to = from.AddDays(1);
        return BuildReportAsync(from, to, loginId, cancellationToken);
    }

    public Task<SalesReportResponse> GetYesterdayReportAsync(Guid loginId, CancellationToken cancellationToken)
    {
        var to = DateTime.UtcNow.Date;
        var from = to.AddDays(-1);
        return BuildReportAsync(from, to, loginId, cancellationToken);
    }

    public Task<SalesReportResponse> GetLast7DaysReportAsync(Guid loginId, CancellationToken cancellationToken)
    {
        var to = DateTime.UtcNow.Date.AddDays(1);
        var from = DateTime.UtcNow.Date.AddDays(-6);
        return BuildReportAsync(from, to, loginId, cancellationToken);
    }

    public Task<SalesReportResponse> GetLast30DaysReportAsync(Guid loginId, CancellationToken cancellationToken)
    {
        var to = DateTime.UtcNow.Date.AddDays(1);
        var from = DateTime.UtcNow.Date.AddDays(-29);
        return BuildReportAsync(from, to, loginId, cancellationToken);
    }

    public Task<SalesReportResponse> GetThisYearReportAsync(Guid loginId, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var from = new DateTime(now.Year, 1, 1);
        var to = from.AddYears(1);
        return BuildReportAsync(from, to, loginId, cancellationToken);
    }

    public async Task<SalesReportResponse> GetCustomReportAsync(CustomSalesReportRequest request, Guid loginId, CancellationToken cancellationToken)
    {
        await _customReportValidator.ValidateAndThrowAsync(request, cancellationToken);

        var from = request.FromDate.Date;
        var to = request.ToDate.Date.AddDays(1);

        return await BuildReportAsync(from, to, loginId, cancellationToken);
    }

    public async Task<DashboardResponse> GetDashboardAsync(Guid loginId, CancellationToken cancellationToken)
    {
        var todayFrom = DateTime.UtcNow.Date;
        var todayTo = todayFrom.AddDays(1);

        var todaySaleAmount = await _saleRepository.SumGrandTotalAsync(todayFrom, todayTo, loginId, cancellationToken);

        var todayPurchaseAmount = await _context.SaleItems
            .AsNoTracking()
            .Where(i => i.Sale!.LoginId == loginId && i.Sale.BillDate >= todayFrom && i.Sale.BillDate < todayTo)
            .SumAsync(i => (decimal?)i.PurchasePrice * i.Quantity, cancellationToken) ?? 0;

        var currentStockPurchaseValue = await _context.Products
            .AsNoTracking()
            .Where(p => p.LoginId == loginId && !p.IsDeleted)
            .SumAsync(p => (decimal?)p.StockQuantity * p.PurchasePrice, cancellationToken) ?? 0;

        var currentStockSellingValue = await _context.Products
            .AsNoTracking()
            .Where(p => p.LoginId == loginId && !p.IsDeleted)
            .SumAsync(p => (decimal?)p.StockQuantity * p.SellingPrice, cancellationToken) ?? 0;

        var lowStockProducts = await _context.Products
            .AsNoTracking()
            .Where(p => p.LoginId == loginId && !p.IsDeleted && p.StockQuantity > 0 && p.StockQuantity <= 10)
            .OrderBy(p => p.StockQuantity)
            .Take(20)
            .ToListAsync(cancellationToken);

        var outOfStockProducts = await _context.Products
            .AsNoTracking()
            .Where(p => p.LoginId == loginId && !p.IsDeleted && p.StockQuantity <= 0)
            .OrderBy(p => p.Name)
            .Take(20)
            .ToListAsync(cancellationToken);

        var lowStockCount = await _context.Products
            .AsNoTracking()
            .CountAsync(p => p.LoginId == loginId && !p.IsDeleted && p.StockQuantity > 0 && p.StockQuantity <= 10, cancellationToken);

        var outOfStockCount = await _context.Products
            .AsNoTracking()
            .CountAsync(p => p.LoginId == loginId && !p.IsDeleted && p.StockQuantity <= 0, cancellationToken);

        var expiryWithin30Days = await _context.Products
            .AsNoTracking()
            .Where(p => p.LoginId == loginId && !p.IsDeleted)
            .Where(p => p.ExpiryDate.HasValue)
            .Where(p => p.ExpiryDate!.Value.Date >= DateTime.UtcNow.Date && p.ExpiryDate.Value.Date <= DateTime.UtcNow.Date.AddDays(30))
            .CountAsync(cancellationToken);

        var topSelling = await _context.SaleItems
            .AsNoTracking()
            .Where(i => i.Sale!.LoginId == loginId)
            .GroupBy(i => new { i.ProductFk, ProductName = i.Product!.Name })
            .Select(g => new TopSellingProductResponse
            {
                ProductFk = g.Key.ProductFk,
                ProductName = g.Key.ProductName,
                TotalQuantity = g.Sum(x => x.Quantity),
                TotalSaleAmount = g.Sum(x => x.Total)
            })
            .OrderByDescending(x => x.TotalQuantity)
            .Take(10)
            .ToListAsync(cancellationToken);

        var recentSales = await _saleRepository.GetRecentAsync(loginId, 10, cancellationToken);

        return new DashboardResponse
        {
            TodayPurchaseAmount = todayPurchaseAmount,
            TodaySaleAmount = todaySaleAmount,
            TodayProfit = todaySaleAmount - todayPurchaseAmount,
            CurrentStockPurchaseValue = currentStockPurchaseValue,
            CurrentStockSellingValue = currentStockSellingValue,
            ExpectedProfit = currentStockSellingValue - currentStockPurchaseValue,
            TotalProducts = await _context.Products.CountAsync(p => p.LoginId == loginId && !p.IsDeleted, cancellationToken),
            LowStockCount = lowStockCount,
            OutOfStockCount = outOfStockCount,
            ExpiryWithin30Days = expiryWithin30Days,
            TopSellingProducts = topSelling,
            RecentSales = _mapper.Map<IReadOnlyCollection<SaleResponse>>(recentSales),
            LowStockProducts = lowStockProducts.Select(p => new DashboardProductResponse
            {
                ProductPk = p.ProductIdPk,
                Name = p.Name,
                StockQuantity = p.StockQuantity,
                PurchasePrice = p.PurchasePrice,
                SellingPrice = p.SellingPrice,
                ExpiryDate = p.ExpiryDate
            }).ToList(),
            OutOfStockProducts = outOfStockProducts.Select(p => new DashboardProductResponse
            {
                ProductPk = p.ProductIdPk,
                Name = p.Name,
                StockQuantity = p.StockQuantity,
                PurchasePrice = p.PurchasePrice,
                SellingPrice = p.SellingPrice,
                ExpiryDate = p.ExpiryDate
            }).ToList()
        };
    }

    private async Task<Customer> EnsureCustomerAsync(CreateSaleRequest request, Guid loginId, CancellationToken cancellationToken)
    {
        var mobile = request.Customer.Mobile.Trim();
        var customer = await _customerRepository.GetByMobileAsync(loginId, mobile, cancellationToken);
        if (customer is not null)
        {
            return customer;
        }

        customer = new Customer
        {
            Id = Guid.NewGuid(),
            LoginId = loginId,
            Name = request.Customer.Name,
            Mobile = mobile,
            Address = request.Customer.Address,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _customerRepository.AddAsync(customer, cancellationToken);
        return customer;
    }

    private async Task<Medshop.Modules.Products.Domain.Entities.Product?> ResolveProductForSaleAsync(
        CreateSaleItemRequest itemRequest,
        Guid loginId,
        CancellationToken cancellationToken)
    {
        var token = itemRequest.ProductFk.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null
            ? itemRequest.ProductFkCamel
            : itemRequest.ProductFk;

        if (token.ValueKind == JsonValueKind.Number && token.TryGetInt64(out var productPk) && productPk > 0)
        {
            return await _productRepository.GetByPrimaryKeyAndLoginIdAsync(productPk, loginId, cancellationToken);
        }

        if (token.ValueKind == JsonValueKind.String)
        {
            var value = token.GetString();
            if (!string.IsNullOrWhiteSpace(value))
            {
                if (long.TryParse(value, out var parsedProductPk) && parsedProductPk > 0)
                {
                    return await _productRepository.GetByPrimaryKeyAndLoginIdAsync(parsedProductPk, loginId, cancellationToken);
                }

                if (Guid.TryParse(value, out var productId))
                {
                    return await _productRepository.GetByIdAndLoginIdAsync(productId, loginId, cancellationToken);
                }
            }
        }

        return null;
    }

    private async Task<SalesReportResponse> BuildReportAsync(DateTime fromDateInclusive, DateTime toDateExclusive, Guid loginId, CancellationToken cancellationToken)
    {
        var sales = await _context.Sales
            .AsNoTracking()
            .Include(s => s.Customer)
            .Include(s => s.Items)
                .ThenInclude(i => i.Product)
            .Where(s => s.LoginId == loginId && s.BillDate >= fromDateInclusive && s.BillDate < toDateExclusive)
            .OrderByDescending(s => s.SaleIdPk)
            .ToListAsync(cancellationToken);

        return new SalesReportResponse
        {
            FromDate = fromDateInclusive,
            ToDate = toDateExclusive.AddDays(-1),
            TotalBills = sales.Count,
            Subtotal = sales.Sum(s => s.Subtotal),
            Discount = sales.Sum(s => s.Discount),
            Tax = sales.Sum(s => s.Tax),
            GrandTotal = sales.Sum(s => s.GrandTotal),
            Sales = _mapper.Map<IReadOnlyCollection<SaleResponse>>(sales)
        };
    }
}
