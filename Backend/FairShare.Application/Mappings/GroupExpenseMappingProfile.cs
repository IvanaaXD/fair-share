using AutoMapper;
using FairShare.Application.DTOs.GroupExpenses;
using FairShare.Domain.Entities;

namespace FairShare.Application.Mappings
{
    public class GroupExpenseMappingProfile : Profile
    {
        public GroupExpenseMappingProfile()
        {
            CreateMap<GroupExpense, GroupExpenseResponse>()
                .ForMember(dest => dest.CategoryName, opt => opt.MapFrom(src => src.Category.Name))
                .ForMember(dest => dest.PaidByName,
                    opt => opt.MapFrom(src => src.PaidByUser.FirstName + " " + src.PaidByUser.LastName));

            CreateMap<ExpenseSplit, ExpenseSplitResponse>()
                .ForMember(dest => dest.FirstName, opt => opt.MapFrom(src => src.User.FirstName))
                .ForMember(dest => dest.LastName, opt => opt.MapFrom(src => src.User.LastName));

            // Splits are calculated by ExpenseSplitCalculator, never copied from the request.
            CreateMap<CreateGroupExpenseRequest, GroupExpense>()
                .ForMember(dest => dest.Date, opt => opt.MapFrom(src => src.Date.AsUtc()))
                .ForMember(dest => dest.Splits, opt => opt.Ignore());
        }
    }
}
