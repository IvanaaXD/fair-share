using AutoMapper;
using FairShare.Application.DTOs.Admin;
using FairShare.Domain.Entities;

namespace FairShare.Application.Mappings
{
    public class AuditLogMappingProfile : Profile
    {
        public AuditLogMappingProfile()
        {
            CreateMap<AuditLog, AuditLogResponse>()
                .ForMember(dest => dest.UserFullName,
                    opt => opt.MapFrom(src => src.User != null ? src.User.FirstName + " " + src.User.LastName : string.Empty))
                .ForMember(dest => dest.UserEmail,
                    opt => opt.MapFrom(src => src.User != null ? src.User.Email : string.Empty))
                // Guid.Empty is stored when an action has no specific entity - expose it as null.
                .ForMember(dest => dest.EntityId,
                    opt => opt.MapFrom(src => src.EntityId == Guid.Empty ? (Guid?)null : src.EntityId));
        }
    }
}
