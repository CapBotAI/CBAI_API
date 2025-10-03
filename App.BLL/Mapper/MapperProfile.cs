using App.Entities.DTOs.Accounts;
using App.Entities.DTOs.EvaluationCriteria;
using App.Entities.DTOs.Review;
using App.Entities.DTOs.ReviewComment;
using App.Entities.DTOs.ReviewerAssignment;
using App.Entities.DTOs.Topics;
using App.Entities.DTOs.TopicVersions;
using App.Entities.Entities.App;
using App.Entities.Entities.Core;
using AutoMapper;

namespace App.BLL.Mapper;

public class MapperProfile : Profile
{
    public MapperProfile()
    {

        //CreateMap<ReviewerAssignment, ReviewerAssignmentResponseDTO>()
        //    .ForMember(dest => dest.Reviewer, opt => opt.MapFrom(src => src.Reviewer))
        //    .ForMember(dest => dest.AssignedByUser, opt => opt.MapFrom(src => src.AssignedByUser))
        //    // Make mapping null-safe: some navigation properties may be missing in certain DB states
        //    .ForMember(dest => dest.SubmissionTitle, opt => opt.MapFrom(src =>
        //        src.Submission != null
        //            ? (src.Submission.TopicVersion != null
        //                ? (src.Submission.TopicVersion.Topic != null
        //                    ? src.Submission.TopicVersion.Topic.EN_Title
        //                    : src.Submission.Topic != null ? src.Submission.Topic.EN_Title : null)
        //                : src.Submission.Topic != null ? src.Submission.Topic.EN_Title : null)
        //            : null))
        //    .ForMember(dest => dest.TopicTitle, opt => opt.MapFrom(src =>
        //        src.Submission != null
        //            ? (src.Submission.TopicVersion != null
        //                ? (src.Submission.TopicVersion.Topic != null
        //                    ? src.Submission.TopicVersion.Topic.EN_Title
        //                    : src.Submission.Topic != null ? src.Submission.Topic.EN_Title : null)
        //                : src.Submission.Topic != null ? src.Submission.Topic.EN_Title : null)
        //            : null));
        CreateMap<ReviewerAssignment, ReviewerAssignmentResponseDTO>()
    .ForMember(dest => dest.Reviewer, opt => opt.MapFrom(src => src.Reviewer))
    .ForMember(dest => dest.AssignedByUser, opt => opt.MapFrom(src => src.AssignedByUser))
    // Make mapping null-safe: some navigation properties may be missing in certain DB states
    .ForMember(dest => dest.SubmissionTitle, opt => opt.MapFrom(src =>
        src.Submission != null
            ? (src.Submission.TopicVersion != null
                ? (src.Submission.TopicVersion.Topic != null
                    ? src.Submission.TopicVersion.Topic.EN_Title
                    : src.Submission.Topic != null ? src.Submission.Topic.EN_Title : null)
                : src.Submission.Topic != null ? src.Submission.Topic.EN_Title : null)
            : null))
    .ForMember(dest => dest.TopicTitle, opt => opt.MapFrom(src =>
        src.Submission != null
            ? (src.Submission.TopicVersion != null
                ? (src.Submission.TopicVersion.Topic != null
                    ? src.Submission.TopicVersion.Topic.EN_Title
                    : src.Submission.Topic != null ? src.Submission.Topic.EN_Title : null)
                : src.Submission.Topic != null ? src.Submission.Topic.EN_Title : null)
            : null))
    .ForMember(dest => dest.TopicId, opt => opt.MapFrom(src =>
        src.Submission != null
            ? (src.Submission.TopicVersion != null
                ? (src.Submission.TopicVersion.Topic != null
                    ? src.Submission.TopicVersion.Topic.Id
                    : src.Submission.Topic != null ? src.Submission.Topic.Id : (int?)null)
                : src.Submission.Topic != null ? src.Submission.Topic.Id : (int?)null)
            : (int?)null))
    .ForMember(dest => dest.Topic, opt => opt.MapFrom(src =>
    src.Submission != null
        ? (src.Submission.TopicVersion != null
            ? src.Submission.TopicVersion.Topic
            : src.Submission.Topic)
        : null))
    .ForMember(dest => dest.TopicVersion, opt => opt.MapFrom(src =>
    src.Submission != null
        ? src.Submission.TopicVersion
        : null));

        CreateMap<User, AvailableReviewerDTO>()
            .ForMember(dest => dest.Skills, opt => opt.MapFrom(src =>
                src.LecturerSkills.Select(ls => ls.SkillTag).ToList()))
            .ForMember(dest => dest.CurrentAssignments, opt => opt.Ignore())
            .ForMember(dest => dest.IsAvailable, opt => opt.Ignore());

        // EvaluationCriteria mappings
        CreateMap<CreateEvaluationCriteriaDTO, EvaluationCriteria>();
        CreateMap<UpdateEvaluationCriteriaDTO, EvaluationCriteria>();
        CreateMap<EvaluationCriteria, EvaluationCriteriaResponseDTO>()
            .ForMember(dest => dest.SemesterName, opt => opt.MapFrom(src => src.Semester != null ? src.Semester.Name : null));

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
        
        CreateMap<User, UserOverviewDTO>()
            .ConstructUsing((src, context) => 
            {
                // Lấy roles từ UserRoles navigation property and ensure non-nullable Role list
                var roles = src.UserRoles?.Select(ur => ur.Role).Where(r => r != null).Select(r => r!).ToList() ?? new List<Role>();
                return new UserOverviewDTO(src, roles);
            });
        CreateMap<Topic, TopicDetailDTO>();
        CreateMap<TopicVersion, TopicVersionDetailDTO>();
        

    }
}