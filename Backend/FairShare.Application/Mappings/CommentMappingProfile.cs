using AutoMapper;
using FairShare.Application.DTOs.Comments;
using FairShare.Domain.Entities;

namespace FairShare.Application.Mappings
{
    public class CommentMappingProfile : Profile
    {
        public CommentMappingProfile()
        {
            CreateMap<Comment, CommentResponse>()
                .ForMember(dest => dest.FirstName, opt => opt.MapFrom(src => src.User.FirstName))
                .ForMember(dest => dest.LastName, opt => opt.MapFrom(src => src.User.LastName));

            CreateMap<CreateCommentRequest, Comment>()
                .ForMember(dest => dest.Text, opt => opt.MapFrom(src => src.Text.Trim()));
        }
    }
}
