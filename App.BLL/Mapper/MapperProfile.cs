using App.Entities.DTOs.EvaluationCriteria;
using App.Entities.DTOs.Review;
using App.Entities.DTOs.ReviewComment;
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

        // EvaluationCriteria mappings
        CreateMap<CreateEvaluationCriteriaDTO, EvaluationCriteria>();
        CreateMap<UpdateEvaluationCriteriaDTO, EvaluationCriteria>();
        CreateMap<EvaluationCriteria, EvaluationCriteriaResponseDTO>();

        // Review mappings
        CreateMap<CreateReviewDTO, Review>()
            .ForMember(dest => dest.ReviewCriteriaScores, opt => opt.Ignore());
        CreateMap<UpdateReviewDTO, Review>()
            .ForMember(dest => dest.ReviewCriteriaScores, opt => opt.Ignore());
        CreateMap<Review, ReviewResponseDTO>()
            .ForMember(dest => dest.CriteriaScores, opt => opt.MapFrom(src =>
                src.ReviewCriteriaScores.Where(x => x.IsActive)));

        // ReviewCriteriaScore mappings
        CreateMap<CriteriaScoreDTO, ReviewCriteriaScore>();
        CreateMap<ReviewCriteriaScore, CriteriaScoreResponseDTO>();
        // ReviewComment mappings
        CreateMap<ReviewComment, ReviewCommentResponseDTO>()
            .ForMember(dest => dest.CommentTypeName, opt => opt.MapFrom(src => src.CommentType.ToString()))
            .ForMember(dest => dest.PriorityName, opt => opt.MapFrom(src => src.Priority.ToString()));

        CreateMap<CreateReviewCommentDTO, ReviewComment>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.IsResolved, opt => opt.MapFrom(src => false))
            .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => true))
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => DateTime.UtcNow))
            .ForMember(dest => dest.LastModifiedAt, opt => opt.MapFrom(src => DateTime.UtcNow));
    }
}