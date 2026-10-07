using AutoMapper;
using FairShare.Application.DTOs.Budgets;
using FairShare.Domain.Entities;

namespace FairShare.Application.Mappings
{
    public class BudgetMappingProfile : Profile
    {
        public BudgetMappingProfile()
        {
            // CurrentSpending and PercentageUsed are calculated in BudgetService from expenses.
            CreateMap<Budget, BudgetResponse>()
                .ForMember(dest => dest.CategoryName,
                    opt => opt.MapFrom(src => src.Category != null ? src.Category.Name : string.Empty))
                .ForMember(dest => dest.CurrentSpending, opt => opt.Ignore())
                .ForMember(dest => dest.PercentageUsed, opt => opt.Ignore());

            CreateMap<CreateBudgetRequest, Budget>();

            CreateMap<UpdateBudgetRequest, Budget>();
        }
    }
}
