using AutoMapper;
using FairShare.Application.DTOs.Settlements;
using FairShare.Domain.Entities;

namespace FairShare.Application.Mappings
{
    public class SettlementMappingProfile : Profile
    {
        public SettlementMappingProfile()
        {
            CreateMap<SettlementTransaction, SettlementTransactionResponse>()
                .ForMember(dest => dest.DebtorName,
                    opt => opt.MapFrom(src => src.DebtorUser.FirstName + " " + src.DebtorUser.LastName))
                .ForMember(dest => dest.CreditorName,
                    opt => opt.MapFrom(src => src.CreditorUser.FirstName + " " + src.CreditorUser.LastName));

            // Nested QrPaymentData property is mapped automatically through this map.
            CreateMap<QrPaymentData, QrPaymentDataResponse>();
        }
    }
}
