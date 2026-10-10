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

            // ReceiptImageUrl is never taken from a request: it is set only by ReceiptService after
            // a successful upload. Otherwise a client could point its expense at any address, and
            // every update without the field would silently remove the receipt.
            CreateMap<CreateExpenseRequest, Expense>()
                .ForMember(dest => dest.Date, opt => opt.MapFrom(src => src.Date.AsUtc()))
                .ForMember(dest => dest.Currency, opt => opt.MapFrom(src => src.Currency.ToUpperInvariant()))
                .ForMember(dest => dest.ReceiptImageUrl, opt => opt.Ignore());

            // Used as _mapper.Map(request, existingExpense) - only request fields are overwritten.
            CreateMap<UpdateExpenseRequest, Expense>()
                .ForMember(dest => dest.Date, opt => opt.MapFrom(src => src.Date.AsUtc()))
                .ForMember(dest => dest.Currency, opt => opt.MapFrom(src => src.Currency.ToUpperInvariant()))
                .ForMember(dest => dest.ReceiptImageUrl, opt => opt.Ignore());
        }
    }
}
