using AutoMapper;
using FairShare.Application.DTOs.Groups;
using FairShare.Domain.Entities;

namespace FairShare.Application.Mappings
{
    public class GroupMappingProfile : Profile
    {
        public GroupMappingProfile()
        {
            // Members are mapped element by element using the GroupMember map below.
            CreateMap<Group, GroupResponse>();

            CreateMap<GroupMember, GroupMemberResponse>()
                .ForMember(dest => dest.FirstName, opt => opt.MapFrom(src => src.User.FirstName))
                .ForMember(dest => dest.LastName, opt => opt.MapFrom(src => src.User.LastName))
                .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.User.Email));

            CreateMap<CreateGroupRequest, Group>()
                .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name.Trim()))
                .ForMember(dest => dest.Currency, opt => opt.MapFrom(src => src.Currency.ToUpperInvariant()));
        }
    }
}
