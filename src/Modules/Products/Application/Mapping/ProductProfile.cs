using AutoMapper;
using Medshop.Modules.Products.Application.DTOs.Request;
using Medshop.Modules.Products.Application.DTOs.Response;
using Medshop.Modules.Products.Domain.Entities;

namespace Medshop.Modules.Products.Application.Mapping;

public class ProductProfile : Profile
{
    public ProductProfile()
    {
        CreateMap<CreateProductRequest, Product>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(_ => Guid.NewGuid()))
            .ForMember(dest => dest.LoginId, opt => opt.Ignore())
            .ForMember(dest => dest.ProductImage, opt => opt.Ignore())
            .ForMember(dest => dest.IsDeleted, opt => opt.MapFrom(_ => false))
            .ForMember(dest => dest.DeletedAt, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(_ => DateTime.UtcNow))
            .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(_ => DateTime.UtcNow));

        CreateMap<Product, ProductResponse>();
    }
}