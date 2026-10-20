using AutoMapper;
using FairShare.Application.DTOs.Categories;
using FairShare.Domain.Entities;

namespace FairShare.Application.Mappings
{
    /// <summary>
    /// Only entity -> response. Categories are created and changed through the methods of the
    /// Category entity (CreateApproved, Propose, Approve...), never mapped from a request, so a
    /// request can never set the status or IsSystemDefined.
    /// </summary>
    public class CategoryMappingProfile : Profile
    {
        public CategoryMappingProfile()
        {
            CreateMap<Category, CategoryResponse>();
            CreateMap<Category, CategoryProposalResponse>();

            CreateMap<Category, AdminCategoryResponse>()
                .ForMember(dest => dest.ProposedByName, opt => opt.MapFrom(src =>
                    src.ProposedByUser == null ? null : (src.ProposedByUser.FirstName + " " + src.ProposedByUser.LastName).Trim()))
                .ForMember(dest => dest.ProposedByEmail, opt => opt.MapFrom(src =>
                    src.ProposedByUser == null ? null : src.ProposedByUser.Email))
                // Filled in by CategoryService from one grouped query.
                .ForMember(dest => dest.UsageCount, opt => opt.Ignore());
        }
    }
}
