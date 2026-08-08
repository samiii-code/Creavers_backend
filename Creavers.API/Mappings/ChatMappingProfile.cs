using AutoMapper;
using Creavers.API.DTOs.Chat;
using Creavers.API.Models;

namespace Creavers.API.Mappings
{
    public class ChatMappingProfile : Profile
    {
        public ChatMappingProfile()
        {
            CreateMap<ChatMessage, ChatMessageResponse>()
                .ForMember(dest => dest.SenderName,
                    opt => opt.MapFrom(src =>
                        src.Sender != null ? src.Sender.FullName : string.Empty));
        }
    }
}
