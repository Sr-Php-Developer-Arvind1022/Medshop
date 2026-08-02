using AutoMapper;
using Medshop.Modules.Categories.Application.DTOs.Request;
using Medshop.Modules.Categories.Application.DTOs.Response;
using Medshop.Modules.Categories.Domain.Entities;

namespace Medshop.Modules.Categories.Application.Mapping;

public class CategoryProfile : Profile
{
    public CategoryProfile()
    {
        CreateMap<CreateCategoryRequest, Category>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(_ => Guid.NewGuid()))
            .ForMember(dest => dest.LoginId, opt => opt.Ignore())
            .ForMember(dest => dest.CategoryImage, opt => opt.Ignore())
            .ForMember(dest => dest.IsDeleted, opt => opt.MapFrom(_ => false))
            .ForMember(dest => dest.DeletedAt, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(_ => DateTime.UtcNow))
            .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(_ => DateTime.UtcNow));

        CreateMap<Category, CategoryResponse>();
    }
}
