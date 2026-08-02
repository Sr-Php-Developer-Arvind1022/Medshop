using Medshop.Modules.Sales.Application.DTOs.Response;

namespace Medshop.Modules.Sales.Application.Interfaces;

public interface IDashboardService
{
    Task<DashboardDto> GetDashboardAsync(
        Guid loginId,
        string? filter,
        DateTime? startDate,
        DateTime? endDate,
        CancellationToken cancellationToken);
}
