using App.Entities.DTOs.TopicVersions;
using App.Entities.Entities.App;
using App.Entities.Enums;

namespace App.Entities.DTOs.Topics;

public class TopicDetailDTO
{
    public int Id { get; set; }
    public string EN_Title { get; set; } = null!;

    public string? Abbreviation { get; set; }
    public string? VN_title { get; set; }
    public string? Problem { get; set; }

    public string? Context { get; set; }
    public string? Content { get; set; }
    public string? Description { get; set; }
    public string? Objectives { get; set; }
    public int SupervisorId { get; set; }
    public string SupervisorName { get; set; } = null!;
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = null!;
    public int SemesterId { get; set; }
    public string SemesterName { get; set; } = null!;
    public int MaxStudents { get; set; }
    public bool IsApproved { get; set; }
    public bool IsLegacy { get; set; }
    public long? FileId { get; set; }
    public string? DocumentUrl { get; set; }
 
    public int TotalVersions { get; set; }
   
    public DateTime CreatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? LastModifiedAt { get; set; }
    public string? LastModifiedBy { get; set; }
    public TopicVersionDetailDTO? CurrentVersion { get; set; }

    public int TotalSubmissions { get; set; }

    public bool HasSubmitted { get; set; }
    public string? PotentialDuplicate { get; set; }

    public SubmissionStatus? LatestSubmissionStatus { get; set; }
    public DateTime? LatestSubmittedAt { get; set; }

    public List<SubmissionInTopicDetailDTO> Submissions { get; set; } = new List<SubmissionInTopicDetailDTO>();


    public TopicDetailDTO() { }

    public TopicDetailDTO(Topic topic, EntityFile? entityFile)
    {
        Id = topic.Id;
        EN_Title = topic.EN_Title;
        Abbreviation = topic.Abbreviation;
        VN_title = topic.VN_title;
        Problem = topic.Problem;
        Context = topic.Context;
        Content = topic.Content;
        Description = topic.Description;
        Objectives = topic.Objectives;
        SupervisorId = topic.SupervisorId;
        SupervisorName = topic.Supervisor?.UserName ?? "";
        CategoryId = topic.CategoryId ?? 0;
        CategoryName = topic.Category?.Name ?? "";
        SemesterId = topic.SemesterId;
        SemesterName = topic.Semester?.Name ?? "";
        MaxStudents = topic.MaxStudents;
        IsApproved = topic.IsApproved;
        IsLegacy = topic.IsLegacy;
        TotalVersions = topic.TopicVersions?.Count ?? 0;
        PotentialDuplicate = topic.PotentialDuplicate;
        

        CreatedAt = topic.CreatedAt;
        CreatedBy = topic.CreatedBy;
        LastModifiedAt = topic.LastModifiedAt;
        LastModifiedBy = topic.LastModifiedBy;

        FileId = entityFile?.FileId;
        DocumentUrl = entityFile?.File?.Url;

        var latestVersion = topic.TopicVersions?.OrderByDescending(v => v.VersionNumber).FirstOrDefault();
        CurrentVersion = latestVersion != null ? new TopicVersionDetailDTO(latestVersion, null) : null;


        LatestSubmittedAt = topic.Submissions
            .Where(s => s.IsActive && s.DeletedAt == null)
            .OrderByDescending(s => s.SubmittedAt ?? s.CreatedAt)
            .Select(s => s.SubmittedAt)
            .FirstOrDefault();

        LatestSubmissionStatus = topic.Submissions
            .Where(s => s.IsActive && s.DeletedAt == null)
            .OrderByDescending(s => s.SubmittedAt ?? s.CreatedAt)
            .Select(s => (SubmissionStatus?)s.Status)
            .FirstOrDefault();

        HasSubmitted = LatestSubmittedAt.HasValue;

        TotalSubmissions = topic.Submissions?.Count ?? 0;
        Submissions = topic.Submissions?.Select(s => new SubmissionInTopicDetailDTO(s)).ToList() ?? new List<SubmissionInTopicDetailDTO>();
    }
}
public class SubmissionInTopicDetailDTO
{
    public int Id { get; set; }
    public int TopicId { get; set; }
    public int? TopicVersionId { get; set; }
    public int PhaseId { get; set; }
    public string? PhaseName { get; set; }
    public int SubmittedBy { get; set; }
    public string? SubmittedByUserName { get; set; }
    public int SubmissionRound { get; set; } = 1;
    public string? DocumentUrl { get; set; }
    public string? AdditionalNotes { get; set; }
    public AiCheckStatus AiCheckStatus { get; set; } = AiCheckStatus.Pending;
    public decimal? AiCheckScore { get; set; }
    public string? AiCheckDetails { get; set; }
    public SubmissionStatus Status { get; set; }
    public DateTime? SubmittedAt { get; set; }

    public List<ReviewInfoDTO> Reviews { get; set; } = new List<ReviewInfoDTO>();
    public int TotalReviews { get; set; }
    public int CompletedReviews { get; set; }
    public decimal? AverageScore { get; set; }

    public SubmissionInTopicDetailDTO() { }

    public SubmissionInTopicDetailDTO(Submission submission)
    {
        Id = submission.Id;
        TopicId = submission.TopicId;
        TopicVersionId = submission.TopicVersionId;
        PhaseId = submission.PhaseId;
        PhaseName = submission.Phase?.Name;
        SubmittedBy = submission.SubmittedBy;
        SubmittedByUserName = submission.SubmittedByUser?.UserName;
        SubmissionRound = submission.SubmissionRound;
        DocumentUrl = submission.DocumentUrl;
        AdditionalNotes = submission.AdditionalNotes;
        AiCheckStatus = submission.AiCheckStatus;
        AiCheckScore = submission.AiCheckScore;
        AiCheckDetails = submission.AiCheckDetails;
        Status = submission.Status;
        SubmittedAt = submission.SubmittedAt;

        if (submission.ReviewerAssignments != null)
        {
            Reviews = submission.ReviewerAssignments
                .Where(ra => ra.Reviews != null && ra.Reviews.Any())
                .SelectMany(ra => ra.Reviews.Where(r => r.IsActive).Select(r => new ReviewInfoDTO
                {
                    ReviewId = r.Id,
                    ReviewerId = ra.ReviewerId,
                    ReviewerName = ra.Reviewer?.UserName ?? "Unknown",
                    AssignmentType = ra.AssignmentType,
                    OverallScore = r.OverallScore,
                    OverallComment = r.OverallComment,
                    Recommendation = r.Recommendation,
                    Status = r.Status,
                    TimeSpentMinutes = r.TimeSpentMinutes,
                    SubmittedAt = r.SubmittedAt,
                    CreatedAt = r.CreatedAt
                }))
                .OrderByDescending(r => r.SubmittedAt ?? r.CreatedAt)
                .ToList();

            TotalReviews = Reviews.Count;
            CompletedReviews = Reviews.Count(r => r.Status == ReviewStatus.Submitted);

            var submittedReviews = Reviews.Where(r => r.OverallScore.HasValue).ToList();
            if (submittedReviews.Any())
            {
                AverageScore = submittedReviews.Average(r => r.OverallScore!.Value);
            }
        }
    }
}

public class ReviewInfoDTO
{
    public int ReviewId { get; set; }
    public int ReviewerId { get; set; }
    public string ReviewerName { get; set; } = null!;
    public AssignmentTypes AssignmentType { get; set; }
    public decimal? OverallScore { get; set; }
    public string? OverallComment { get; set; }
    public ReviewRecommendations Recommendation { get; set; }
    public ReviewStatus Status { get; set; }
    public int? TimeSpentMinutes { get; set; }
    public DateTime? SubmittedAt { get; set; }
    public DateTime CreatedAt { get; set; }
}