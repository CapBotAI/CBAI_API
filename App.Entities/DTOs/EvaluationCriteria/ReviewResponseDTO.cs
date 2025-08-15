using App.Entities.DTOs.EvaluationCriteria;
using App.Entities.Enums;

namespace App.Entities.DTOs.Review;

public class ReviewResponseDTO
{
    public int Id { get; set; }
    public int AssignmentId { get; set; }
    public decimal? OverallScore { get; set; }
    public string? OverallComment { get; set; }
    public ReviewRecommendations Recommendation { get; set; }
    public int? TimeSpentMinutes { get; set; }
    public ReviewStatus Status { get; set; }
    public DateTime? SubmittedAt { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime? ModifiedDate { get; set; }
    public string? CreatedBy { get; set; }
    public string? ModifiedBy { get; set; }

    public List<CriteriaScoreResponseDTO> CriteriaScores { get; set; } = new List<CriteriaScoreResponseDTO>();
}

public class CriteriaScoreResponseDTO
{
    public int Id { get; set; }
    public int CriteriaId { get; set; }
    public decimal Score { get; set; }
    public string? Comment { get; set; }
    public EvaluationCriteriaResponseDTO Criteria { get; set; } = null!;
}