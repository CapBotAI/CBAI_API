using App.Entities.DTOs.ReviewerAssignment;

namespace App.BLL.Interfaces;

public interface IPerformanceMatchingService
{
    /// <summary>
    /// Tính performance score tổng hợp của reviewer
    /// </summary>
    Task<decimal> CalculateOverallPerformanceScoreAsync(int reviewerId, int? semesterId = null);

    /// <summary>
    /// Tìm reviewer tốt nhất dựa trên performance
    /// </summary>
    Task<List<ReviewerMatchingResult>> FindBestPerformingReviewersAsync(
        int submissionId,
        AutoAssignReviewerDTO criteria);

    /// <summary>
    /// Tính reliability score (độ tin cậy) của reviewer
    /// </summary>
    Task<decimal> CalculateReliabilityScoreAsync(int reviewerId, int? semesterId = null);

    /// <summary>
    /// Tính efficiency score (hiệu quả) của reviewer
    /// </summary>
    Task<decimal> CalculateEfficiencyScoreAsync(int reviewerId, int? semesterId = null);

    /// <summary>
    /// Tính consistency score (tính nhất quán) của reviewer
    /// </summary>
    Task<decimal> CalculateConsistencyScoreAsync(int reviewerId, int? semesterId = null);
}