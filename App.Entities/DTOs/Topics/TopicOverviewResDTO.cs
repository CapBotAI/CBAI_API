using App.Entities.Entities.App;
using App.Entities.Enums;

namespace App.Entities.DTOs.Topics;

public class TopicOverviewResDTO
{
    public int Id { get; set; }
    public string EN_Title { get; set; } = null!;
    public string? Abbreviation { get; set; }
    public string? VN_title { get; set; }
    public string? Problem { get; set; }

    public string? Context { get; set; }
    public string? Content { get; set; }
    public string? Description { get; set; }
    public string SupervisorName { get; set; } = null!;
    public string CategoryName { get; set; } = null!;
    public string SemesterName { get; set; } = null!;
    public int MaxStudents { get; set; }
    public bool IsApproved { get; set; }
    public bool IsLegacy { get; set; }
    public string? PotentialDuplicate { get; set; }

    public int? CurrentVersionNumber { get; set; }
    public TopicStatus? CurrentVersionStatus { get; set; }

    public bool HasSubmitted { get; set; }

    public SubmissionStatus? LatestSubmissionStatus { get; set; }
    public DateTime? LatestSubmittedAt { get; set; }

    public List<SubmissionInTopicOverviewResDTO> Submissions { get; set; } = new List<SubmissionInTopicOverviewResDTO>();
    public DateTime CreatedAt { get; set; }

    public TopicOverviewResDTO() { }

    public TopicOverviewResDTO(Topic topic)
    {
        Id = topic.Id;
        EN_Title = topic.EN_Title;
        Abbreviation = topic.Abbreviation;
        VN_title = topic.VN_title;
        Problem = topic.Problem;
        Context = topic.Context;
        Content = topic.Content;
        Description = topic.Description;
        SupervisorName = topic.Supervisor?.UserName ?? "";
        CategoryName = topic.Category?.Name ?? "";
        SemesterName = topic.Semester?.Name ?? "";
        MaxStudents = topic.MaxStudents;
        IsApproved = topic.IsApproved;
        IsLegacy = topic.IsLegacy;
        PotentialDuplicate= topic.PotentialDuplicate;

        CreatedAt = topic.CreatedAt;

        CurrentVersionNumber = topic.TopicVersions?
            .Where(v => v.IsActive && v.DeletedAt == null)
            .OrderByDescending(v => v.VersionNumber)
            .Select(v => (int?)v.VersionNumber)
            .FirstOrDefault();

        CurrentVersionStatus = topic.TopicVersions?
            .Where(v => v.IsActive && v.DeletedAt == null)
            .OrderByDescending(v => v.VersionNumber)
            .Select(v => (TopicStatus?)v.Status)
            .FirstOrDefault();

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

        Submissions = topic.Submissions?.Select(s => new SubmissionInTopicOverviewResDTO(s)).ToList() ?? new List<SubmissionInTopicOverviewResDTO>();
    }
    public class SubmissionInTopicOverviewResDTO
    {
        public int Id { get; set; }

        public int TopicId { get; set; }
        public int? TopicVersionId { get; set; }

        public int PhaseId { get; set; }

        public int SubmittedBy { get; set; }

        public int SubmissionRound { get; set; } = 1;

        public string? DocumentUrl { get; set; }

        public string? AdditionalNotes { get; set; }

        public AiCheckStatus AiCheckStatus { get; set; } = AiCheckStatus.Pending;

        public decimal? AiCheckScore { get; set; }

        public string? AiCheckDetails { get; set; }

        public SubmissionStatus Status { get; set; }

        public SubmissionInTopicOverviewResDTO() { }

        public SubmissionInTopicOverviewResDTO(Submission submission)
        {
            Id = submission.Id;
            TopicId = submission.TopicId;
            TopicVersionId = submission.TopicVersionId;
            PhaseId = submission.PhaseId;
            SubmittedBy = submission.SubmittedBy;
            SubmissionRound = submission.SubmissionRound;
            DocumentUrl = submission.DocumentUrl;
            AdditionalNotes = submission.AdditionalNotes;
            AiCheckStatus = submission.AiCheckStatus;
            AiCheckScore = submission.AiCheckScore;
            AiCheckDetails = submission.AiCheckDetails;
            Status = submission.Status;
        }
    }
}