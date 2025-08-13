using App.Entities.DTOs.ReviewerAssignment;

namespace App.BLL.Interfaces;

public interface ISkillMatchingService
{
    /// <summary>
    /// Tính skill matching score giữa reviewer và đề tài
    /// </summary>
    Task<decimal> CalculateSkillMatchScoreAsync(int reviewerId, List<string> topicSkillTags);
    
    /// <summary>
    /// Lấy skill tags của đề tài từ submission
    /// </summary>
    Task<List<string>> ExtractTopicSkillTagsAsync(int submissionId);
    
    /// <summary>
    /// Tìm reviewer phù hợp nhất cho submission
    /// </summary>
    Task<List<ReviewerMatchingResult>> FindBestMatchingReviewersAsync(
        int submissionId, 
        AutoAssignReviewerDTO criteria);
    
    /// <summary>
    /// Tính performance score của reviewer
    /// </summary>
    Task<decimal> CalculatePerformanceScoreAsync(int reviewerId, int? semesterId = null);
    
    /// <summary>
    /// Tính workload score của reviewer (càng thấp càng tốt)
    /// </summary>
    Task<decimal> CalculateWorkloadScoreAsync(int reviewerId);
}