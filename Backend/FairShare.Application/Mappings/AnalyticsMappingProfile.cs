using AutoMapper;
using FairShare.Application.DTOs.Analytics;
using FairShare.Domain.Models;

namespace FairShare.Application.Mappings
{
    public class AnalyticsMappingProfile : Profile
    {
        public AnalyticsMappingProfile()
        {
            CreateMap<ExpenseLocation, ExpenseLocationResponse>()
                .ForMember(dest => dest.ExpenseId, opt => opt.MapFrom(src => src.Id));
        }
    }
}
