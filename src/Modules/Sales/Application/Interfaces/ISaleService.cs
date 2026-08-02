using Medshop.BuildingBlocks.Common;
using Medshop.Modules.Sales.Application.DTOs.Request;
using Medshop.Modules.Sales.Application.DTOs.Response;

namespace Medshop.Modules.Sales.Application.Interfaces;

public interface ISaleService
{
    Task<SaleResponse> CreateAsync(CreateSaleRequest request, Guid loginId, CancellationToken cancellationToken);
    Task<PagedResult<SaleResponse>> GetPagedAsync(GetSalesRequest request, Guid loginId, CancellationToken cancellationToken);
    Task<SaleResponse> GetByIdAsync(long saleIdPk, Guid loginId, CancellationToken cancellationToken);
    Task SoftDeleteAsync(long saleIdPk, Guid loginId, CancellationToken cancellationToken);
    Task<SalesReportResponse> GetTodayReportAsync(Guid loginId, CancellationToken cancellationToken);
    Task<SalesReportResponse> GetYesterdayReportAsync(Guid loginId, CancellationToken cancellationToken);
    Task<SalesReportResponse> GetLast7DaysReportAsync(Guid loginId, CancellationToken cancellationToken);
    Task<SalesReportResponse> GetLast30DaysReportAsync(Guid loginId, CancellationToken cancellationToken);
    Task<SalesReportResponse> GetThisYearReportAsync(Guid loginId, CancellationToken cancellationToken);
    Task<SalesReportResponse> GetCustomReportAsync(CustomSalesReportRequest request, Guid loginId, CancellationToken cancellationToken);
    Task<DashboardResponse> GetDashboardAsync(Guid loginId, CancellationToken cancellationToken);
}
