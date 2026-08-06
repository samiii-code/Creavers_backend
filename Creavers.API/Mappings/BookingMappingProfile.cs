using AutoMapper;
using Creavers.API.DTOs.Bookings;
using Creavers.API.Models;

namespace Creavers.API.Mappings
{
    public class BookingMappingProfile : Profile
    {
        public BookingMappingProfile()
        {
            CreateMap<Booking, BookingResponse>()
                .ForMember(dest => dest.TaskTitle,
                    opt => opt.MapFrom(src => src.Task != null ? src.Task.Title : string.Empty))
                .ForMember(dest => dest.ProviderName,
                    opt => opt.MapFrom(src => src.Provider != null && src.Provider.ApplicationUser != null ? src.Provider.ApplicationUser.FullName : string.Empty))
                .ForMember(dest => dest.CustomerName,
                    opt => opt.MapFrom(src => src.Customer != null ? src.Customer.FullName : string.Empty));

            CreateMap<CreateBookingRequest, Booking>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.CustomerId, opt => opt.Ignore())
                .ForMember(dest => dest.BookingStatus, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.IsDeleted, opt => opt.Ignore())
                .ForMember(dest => dest.Task, opt => opt.Ignore())
                .ForMember(dest => dest.Provider, opt => opt.Ignore())
                .ForMember(dest => dest.Customer, opt => opt.Ignore());
        }
    }
}
