using AutoMapper;
using Creavers.API.DTOs.Jobs;
using Creavers.API.Models;

namespace Creavers.API.Mappings
{
    public class JobMappingProfile : Profile
    {
        public JobMappingProfile()
        {
            // CompletionEvidence → Response
            CreateMap<CompletionEvidence, CompletionEvidenceResponse>();

            // JobTimeline → Response
            CreateMap<JobTimeline, JobTimelineResponse>()
                .ForMember(dest => dest.Status,
                    opt => opt.MapFrom(src => src.Status.ToString()))
                .ForMember(dest => dest.ChangedByName,
                    opt => opt.MapFrom(src =>
                        src.ChangedByUser != null ? src.ChangedByUser.FullName : string.Empty));
        }
    }
}
