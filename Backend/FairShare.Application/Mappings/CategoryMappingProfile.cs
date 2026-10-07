using AutoMapper;
using FairShare.Application.DTOs.Categories;
using FairShare.Domain.Entities;

namespace FairShare.Application.Mappings
{
    public class CategoryMappingProfile : Profile
    {
        public CategoryMappingProfile()
        {
            CreateMap<Category, CategoryResponse>();

            // IsSystemDefined is never taken from a request - users can only create custom categories.
            CreateMap<CreateCategoryRequest, Category>()
                .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name.Trim()))
                .ForMember(dest => dest.IsSystemDefined, opt => opt.Ignore());

            CreateMap<UpdateCategoryRequest, Category>()
                .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name.Trim()))
                .ForMember(dest => dest.IsSystemDefined, opt => opt.Ignore());
        }
    }
}
