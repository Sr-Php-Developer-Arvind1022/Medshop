using AutoMapper;
using Medshop.Modules.Sales.Application.DTOs.Response;
using Medshop.Modules.Sales.Domain.Entities;

namespace Medshop.Modules.Sales.Application.Mapping;

public class SaleProfile : Profile
{
    public SaleProfile()
    {
        CreateMap<Sale, SaleResponse>()
            .ForMember(dest => dest.SalePk, opt => opt.MapFrom(src => src.SaleIdPk));

        CreateMap<SaleItem, SaleItemResponse>()
            .ForMember(dest => dest.SaleItemPk, opt => opt.MapFrom(src => src.SaleItemIdPk))
            .ForMember(dest => dest.ProductName, opt => opt.MapFrom(src => src.Product != null ? src.Product.Name : string.Empty))
            .ForMember(dest => dest.Profit, opt => opt.MapFrom(src => (src.Price - src.PurchasePrice) * src.Quantity));

        CreateMap<Medshop.Modules.Customers.Domain.Entities.Customer, SaleCustomerResponse>()
            .ForMember(dest => dest.CustomerPk, opt => opt.MapFrom(src => src.CustomerIdPk));
    }
}
