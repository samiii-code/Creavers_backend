using AutoMapper;
using Creavers.API.DTOs.Notifications;
using Creavers.API.Models;

namespace Creavers.API.Mappings
{
    public class NotificationMappingProfile : Profile
    {
        public NotificationMappingProfile()
        {
            CreateMap<Notification, NotificationResponse>();
        }
    }
}
