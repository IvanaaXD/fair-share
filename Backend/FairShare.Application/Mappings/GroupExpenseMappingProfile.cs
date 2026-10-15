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
                    opt => opt.MapFrom(src => src.PaidByUser.FirstName + " " + src.PaidByUser.LastName))
                // Counted in the database by GroupExpenseService, not by loading every comment.
                .ForMember(dest => dest.CommentCount, opt => opt.Ignore());

            CreateMap<ExpenseSplit, ExpenseSplitResponse>()
                .ForMember(dest => dest.FirstName, opt => opt.MapFrom(src => src.User.FirstName))
                .ForMember(dest => dest.LastName, opt => opt.MapFrom(src => src.User.LastName));

            // Splits are calculated by ExpenseSplitCalculator, never copied from the request.
            CreateMap<CreateGroupExpenseRequest, GroupExpense>()
                .ForMember(dest => dest.Date, opt => opt.MapFrom(src => src.Date.AsUtc()))
                .ForMember(dest => dest.Splits, opt => opt.Ignore())
                // Amount and currency conversion are set by GroupExpenseService (the amount may
                // have to be converted to the group's currency first).
                .ForMember(dest => dest.OriginalCurrency, opt => opt.Ignore())
                .ForMember(dest => dest.OriginalAmount, opt => opt.Ignore())
                .ForMember(dest => dest.ExchangeRate, opt => opt.Ignore())
                .ForMember(dest => dest.ExchangeRateDate, opt => opt.Ignore());

            // Used as _mapper.Map(request, existingExpense). IncludeBase reuses the configuration
            // above (UTC date, ignored splits), so the two maps cannot drift apart.
            CreateMap<UpdateGroupExpenseRequest, GroupExpense>()
                .IncludeBase<CreateGroupExpenseRequest, GroupExpense>();
        }
    }
}
