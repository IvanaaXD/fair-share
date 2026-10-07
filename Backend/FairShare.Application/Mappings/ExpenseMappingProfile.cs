using AutoMapper;
using FairShare.Application.DTOs.Expenses;
using FairShare.Domain.Entities;

namespace FairShare.Application.Mappings
{
    public class ExpenseMappingProfile : Profile
    {
        public ExpenseMappingProfile()
        {
            CreateMap<Expense, ExpenseResponse>()
                .ForMember(dest => dest.CategoryName,
                    opt => opt.MapFrom(src => src.Category != null ? src.Category.Name : string.Empty));

            CreateMap<CreateExpenseRequest, Expense>()
                .ForMember(dest => dest.Date, opt => opt.MapFrom(src => src.Date.AsUtc()))
                .ForMember(dest => dest.Currency, opt => opt.MapFrom(src => src.Currency.ToUpperInvariant()));

            // Used as _mapper.Map(request, existingExpense) - only request fields are overwritten.
            CreateMap<UpdateExpenseRequest, Expense>()
                .ForMember(dest => dest.Date, opt => opt.MapFrom(src => src.Date.AsUtc()))
                .ForMember(dest => dest.Currency, opt => opt.MapFrom(src => src.Currency.ToUpperInvariant()));
        }
    }
}
