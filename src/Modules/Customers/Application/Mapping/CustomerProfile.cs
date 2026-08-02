using AutoMapper;
using Medshop.Modules.Customers.Application.DTOs.Response;
using Medshop.Modules.Customers.Domain.Entities;

namespace Medshop.Modules.Customers.Application.Mapping;

public class CustomerProfile : Profile
{
    public CustomerProfile()
    {
        CreateMap<Customer, CustomerResponse>()
            .ForMember(dest => dest.CustomerPk, opt => opt.MapFrom(src => src.CustomerIdPk));
    }
}
