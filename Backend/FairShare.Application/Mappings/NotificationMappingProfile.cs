using AutoMapper;
using FairShare.Application.DTOs.Notifications;
using FairShare.Domain.Entities;

namespace FairShare.Application.Mappings
{
    public class NotificationMappingProfile : Profile
    {
        public NotificationMappingProfile()
        {
            CreateMap<Notification, NotificationResponse>();
        }
    }
}
