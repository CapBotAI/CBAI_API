using App.Entities.DTOs.ReviewerAssignment;
using App.Entities.Entities.App;
using App.Entities.Entities.Core;
using AutoMapper;

namespace App.BLL.Mapper;

public class MapperProfile : Profile
{
    public MapperProfile()
    {
        CreateMap<ReviewerAssignment, ReviewerAssignmentResponseDTO>()
            .ForMember(dest => dest.Reviewer, opt => opt.MapFrom(src => src.Reviewer))
            .ForMember(dest => dest.AssignedByUser, opt => opt.MapFrom(src => src.AssignedByUser))
            .ForMember(dest => dest.SubmissionTitle, opt => opt.MapFrom(src =>
                src.Submission.TopicVersion.Topic.Title))
            .ForMember(dest => dest.TopicTitle, opt => opt.MapFrom(src =>
                src.Submission.TopicVersion.Topic.Title));

        CreateMap<User, AvailableReviewerDTO>()
            .ForMember(dest => dest.Skills, opt => opt.MapFrom(src =>
                src.LecturerSkills.Select(ls => ls.SkillTag).ToList()))
            .ForMember(dest => dest.CurrentAssignments, opt => opt.Ignore())
            .ForMember(dest => dest.CompletedAssignments, opt => opt.Ignore())
            .ForMember(dest => dest.IsAvailable, opt => opt.Ignore());
    }
}