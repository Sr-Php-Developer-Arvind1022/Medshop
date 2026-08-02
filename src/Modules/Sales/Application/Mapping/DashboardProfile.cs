using AutoMapper;
using Medshop.Modules.Sales.Application.DTOs.Response;
using Medshop.Modules.Sales.Domain.Interfaces;

namespace Medshop.Modules.Sales.Application.Mapping;

public class DashboardProfile : Profile
{
    public DashboardProfile()
    {
        CreateMap<DashboardCardsReadModel, DashboardCardsDto>();
        CreateMap<DashboardStockSummaryReadModel, DashboardStockSummaryDto>();
        CreateMap<DashboardLowStockProductReadModel, DashboardLowStockProductDto>();
        CreateMap<DashboardOutOfStockProductReadModel, DashboardOutOfStockProductDto>();
        CreateMap<DashboardExpiryProductReadModel, DashboardExpiryProductDto>();
        CreateMap<DashboardTopSellingProductReadModel, DashboardTopSellingProductDto>();
        CreateMap<DashboardRecentSaleReadModel, DashboardRecentSaleDto>();
        CreateMap<DashboardStatisticsReadModel, DashboardStatisticsDto>();
        CreateMap<DashboardStockValueSummaryReadModel, DashboardStockValueSummaryDto>();
    }
}
