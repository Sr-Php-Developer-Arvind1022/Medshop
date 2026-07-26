using AutoMapper;
using Medshop.Modules.Identity.Application.DTOs.Request;
using Medshop.Modules.Identity.Application.DTOs.Response;
using Medshop.Modules.Identity.Domain.Entities;

namespace Medshop.Modules.Identity.Application.Mapping;

public class IdentityProfile : Profile
{
    public IdentityProfile()
    {
        CreateMap<RegisterRequest, User>()
            .ForMember(dest => dest.PasswordHash, opt => opt.Ignore())
            .ForMember(dest => dest.ProfileImage, opt => opt.Ignore())
            .ForMember(dest => dest.Id, opt => opt.MapFrom(_ => Guid.NewGuid()))
            .ForMember(dest => dest.IsActive, opt => opt.MapFrom(_ => true))
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(_ => DateTime.UtcNow))
            .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(_ => DateTime.UtcNow));

        CreateMap<User, RegisterResponse>();
    }
}
