using Microsoft.EntityFrameworkCore;
using App.BLL.Interfaces;
using App.DAL.UnitOfWork;
using App.DAL.Queries;
using App.Entities.DTOs.ReviewerAssignment;
using App.Entities.Entities.App;
using App.Entities.Entities.Core;
using App.Entities.Enums;
using App.Entities.Constants;
using System.Linq.Expressions;

namespace App.BLL.Implementations;

public class PerformanceMatchingService : IPerformanceMatchingService
{
    private readonly IUnitOfWork _unitOfWork;

    public PerformanceMatchingService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<decimal> CalculateOverallPerformanceScoreAsync(int reviewerId, int? semesterId = null)
    {
        var performanceOptions = new QueryOptions<ReviewerPerformance>
        {
            Predicate = rp => rp.ReviewerId == reviewerId && 
                            (!semesterId.HasValue || rp.SemesterId == semesterId.Value)
        };
        var performances = await _unitOfWork.GetRepo<ReviewerPerformance>().GetAllAsync(performanceOptions);

        if (!performances.Any())
            return 2.5m; // Default neutral score for new reviewers

        // Lấy performance gần nhất hoặc của semester cụ thể
        var latestPerformance = performances.OrderByDescending(p => p.LastUpdated).First();

        // Tính các component scores
        var reliabilityScore = await CalculateReliabilityScoreAsync(reviewerId, semesterId);
        var efficiencyScore = await CalculateEfficiencyScoreAsync(reviewerId, semesterId);
        var consistencyScore = await CalculateConsistencyScoreAsync(reviewerId, semesterId);
        var qualityScore = (decimal)(latestPerformance.QualityRating ?? 2.5m);

        // Weighted average: Reliability 40%, Quality 30%, Efficiency 20%, Consistency 10%
        var overallScore = (reliabilityScore * 0.4m) + 
                          (qualityScore * 0.3m) + 
                          (efficiencyScore * 0.2m) + 
                          (consistencyScore * 0.1m);

        return Math.Min(5.0m, Math.Max(0.0m, overallScore));
    }

    public async Task<decimal> CalculateReliabilityScoreAsync(int reviewerId, int? semesterId = null)
    {
        var performanceOptions = new QueryOptions<ReviewerPerformance>
        {
            Predicate = rp => rp.ReviewerId == reviewerId && 
                            (!semesterId.HasValue || rp.SemesterId == semesterId.Value)
        };
        var performances = await _unitOfWork.GetRepo<ReviewerPerformance>().GetAllAsync(performanceOptions);

        if (!performances.Any())
            return 2.5m;

        var latestPerformance = performances.OrderByDescending(p => p.LastUpdated).First();

        decimal reliabilityScore = 0;
        int factors = 0;

        // On-time completion rate (0-1) -> (0-5)
        if (latestPerformance.OnTimeRate.HasValue)
        {
            reliabilityScore += (decimal)latestPerformance.OnTimeRate.Value * 5;
            factors++;
        }

        // Completion rate
        if (latestPerformance.TotalAssignments > 0)
        {
            decimal completionRate = (decimal)latestPerformance.CompletedAssignments / latestPerformance.TotalAssignments;
            reliabilityScore += completionRate * 5;
            factors++;
        }

        // Consistency in assignment acceptance (bonus for experienced reviewers)
        if (latestPerformance.TotalAssignments >= 10)
        {
            reliabilityScore += 0.5m; // Experience bonus
        }

        return factors > 0 ? Math.Min(5.0m, reliabilityScore / factors) : 2.5m;
    }

    public async Task<decimal> CalculateEfficiencyScoreAsync(int reviewerId, int? semesterId = null)
    {
        var performanceOptions = new QueryOptions<ReviewerPerformance>
        {
            Predicate = rp => rp.ReviewerId == reviewerId && 
                            (!semesterId.HasValue || rp.SemesterId == semesterId.Value)
        };
        var performances = await _unitOfWork.GetRepo<ReviewerPerformance>().GetAllAsync(performanceOptions);

        if (!performances.Any())
            return 2.5m;

        var latestPerformance = performances.OrderByDescending(p => p.LastUpdated).First();

        // Average time efficiency (lower time = higher score)
        // Assume ideal review time is 90 minutes, max acceptable is 180 minutes
        var averageTime = latestPerformance.AverageTimeMinutes;
        if (averageTime <= 0)
            return 2.5m;

        decimal efficiencyScore;
        if (averageTime <= 60) // Very fast
            efficiencyScore = 5.0m;
        else if (averageTime <= 90) // Ideal time
            efficiencyScore = 4.5m;
        else if (averageTime <= 120) // Good time
            efficiencyScore = 4.0m;
        else if (averageTime <= 150) // Acceptable time
            efficiencyScore = 3.0m;
        else if (averageTime <= 180) // Slow but acceptable
            efficiencyScore = 2.0m;
        else // Too slow
            efficiencyScore = 1.0m;

        return efficiencyScore;
    }

    public async Task<decimal> CalculateConsistencyScoreAsync(int reviewerId, int? semesterId = null)
    {
        var performanceOptions = new QueryOptions<ReviewerPerformance>
        {
            Predicate = rp => rp.ReviewerId == reviewerId && 
                            (!semesterId.HasValue || rp.SemesterId == semesterId.Value)
        };
        var performances = await _unitOfWork.GetRepo<ReviewerPerformance>().GetAllAsync(performanceOptions);

        if (!performances.Any())
            return 2.5m;

        var latestPerformance = performances.OrderByDescending(p => p.LastUpdated).First();

        // Consistency based on average score given (not too high, not too low)
        // Ideal range is 7.0 - 8.5
        var avgScore = latestPerformance.AverageScoreGiven;
        if (!avgScore.HasValue)
            return 2.5m;

        decimal consistencyScore;
        var score = (decimal)avgScore.Value;

        if (score >= 7.0m && score <= 8.5m) // Ideal range
            consistencyScore = 5.0m;
        else if (score >= 6.5m && score <= 9.0m) // Good range
            consistencyScore = 4.0m;
        else if (score >= 6.0m && score <= 9.5m) // Acceptable range
            consistencyScore = 3.0m;
        else if (score >= 5.5m && score <= 10.0m) // Wide range but acceptable
            consistencyScore = 2.0m;
        else // Too extreme (too harsh or too lenient)
            consistencyScore = 1.0m;

        return consistencyScore;
    }

    public async Task<List<ReviewerMatchingResult>> FindBestPerformingReviewersAsync(
        int submissionId, AutoAssignReviewerDTO criteria)
    {
        // Get all reviewers with their performance data
        var userRoleOptions = new QueryOptions<UserRole>
        {
            IncludeProperties = new List<Expression<Func<UserRole, object>>>
            {
                ur => ur.User,
                ur => ur.User.ReviewerPerformances,
                ur => ur.Role
            },
            Predicate = ur => ur.Role.Name == SystemRoleConstants.Reviewer
        };
        var userRoles = await _unitOfWork.GetRepo<UserRole>().GetAllAsync(userRoleOptions);
        var reviewers = userRoles.Select(ur => ur.User).Distinct().ToList();

        // Get already assigned reviewers for this submission
        var assignedReviewerOptions = new QueryOptions<ReviewerAssignment>
        {
            Predicate = ra => ra.SubmissionId == submissionId
        };
        var assignedReviewers = await _unitOfWork.GetRepo<ReviewerAssignment>().GetAllAsync(assignedReviewerOptions);
        var assignedReviewerIds = assignedReviewers.Select(ra => ra.ReviewerId).ToHashSet();

        var matchingResults = new List<ReviewerMatchingResult>();

        foreach (var reviewer in reviewers)
        {
            // Skip if already assigned
            if (assignedReviewerIds.Contains(reviewer.Id))
                continue;

            var matchingResult = new ReviewerMatchingResult
            {
                ReviewerId = reviewer.Id,
                ReviewerName = reviewer.UserName,
                ReviewerEmail = reviewer.Email
            };

            // Calculate performance-based scores
            matchingResult.PerformanceScore = await CalculateOverallPerformanceScoreAsync(reviewer.Id);
            matchingResult.WorkloadScore = await CalculateWorkloadScoreAsync(reviewer.Id);

            // Get performance details
            var performance = reviewer.ReviewerPerformances.FirstOrDefault();
            if (performance != null)
            {
                matchingResult.CompletedAssignments = performance.CompletedAssignments;
                matchingResult.AverageScoreGiven = performance.AverageScoreGiven;
                matchingResult.OnTimeRate = performance.OnTimeRate;
                matchingResult.QualityRating = performance.QualityRating;

                // Set skill match score based on performance (higher performance = better "match")
                matchingResult.SkillMatchScore = matchingResult.PerformanceScore;
            }
            else
            {
                // New reviewer - neutral scores
                matchingResult.SkillMatchScore = 2.5m;
                matchingResult.CompletedAssignments = 0;
            }

            // Get current workload
            var activeAssignmentOptions = new QueryOptions<ReviewerAssignment>
            {
                Predicate = ra => ra.ReviewerId == reviewer.Id && 
                               (ra.Status == AssignmentStatus.Assigned || ra.Status == AssignmentStatus.InProgress)
            };
            var activeAssignments = await _unitOfWork.GetRepo<ReviewerAssignment>().GetAllAsync(activeAssignmentOptions);
            matchingResult.CurrentActiveAssignments = activeAssignments.Count();

            // Check eligibility based on performance criteria
            matchingResult.IsEligible = IsReviewerEligibleByPerformance(matchingResult, criteria);
            if (!matchingResult.IsEligible)
            {
                matchingResult.IneligibilityReasons = GetPerformanceIneligibilityReasons(matchingResult, criteria);
            }

            // Calculate overall score (performance-focused)
            matchingResult.OverallScore = CalculatePerformanceBasedOverallScore(matchingResult, criteria);

            matchingResults.Add(matchingResult);
        }

        return matchingResults.OrderByDescending(r => r.OverallScore).ToList();
    }

    #region Private Helper Methods

    private async Task<decimal> CalculateWorkloadScoreAsync(int reviewerId)
    {
        var activeAssignmentOptions = new QueryOptions<ReviewerAssignment>
        {
            Predicate = ra => ra.ReviewerId == reviewerId && 
                           (ra.Status == AssignmentStatus.Assigned || ra.Status == AssignmentStatus.InProgress)
        };
        var activeAssignments = await _unitOfWork.GetRepo<ReviewerAssignment>().GetAllAsync(activeAssignmentOptions);
        
        int activeCount = activeAssignments.Count();
        
        // Workload score: lower load = higher score
        // 0 assignments = 5.0, 10+ assignments = 0.0
        return Math.Max(0, 5.0m - (activeCount * 0.5m));
    }

    private bool IsReviewerEligibleByPerformance(ReviewerMatchingResult result, AutoAssignReviewerDTO criteria)
    {
        // Check performance threshold instead of skill match
        if (result.PerformanceScore < criteria.MinimumSkillMatchScore)
            return false;

        // Check maximum workload
        if (result.CurrentActiveAssignments >= criteria.MaxWorkload)
            return false;

        // Additional performance criteria
        if (criteria.PrioritizeHighPerformance)
        {
            // Require minimum quality rating for high-performance mode
            if (result.QualityRating.HasValue && result.QualityRating < 3.0m)
                return false;

            // Require reasonable on-time rate
            if (result.OnTimeRate.HasValue && result.OnTimeRate < 0.7m)
                return false;
        }

        return true;
    }

    private List<string> GetPerformanceIneligibilityReasons(ReviewerMatchingResult result, AutoAssignReviewerDTO criteria)
    {
        var reasons = new List<string>();

        if (result.PerformanceScore < criteria.MinimumSkillMatchScore)
            reasons.Add($"Performance score ({result.PerformanceScore:F2}) dưới mức yêu cầu ({criteria.MinimumSkillMatchScore:F2})");

        if (result.CurrentActiveAssignments >= criteria.MaxWorkload)
            reasons.Add($"Quá tải assignment ({result.CurrentActiveAssignments}/{criteria.MaxWorkload})");

        if (criteria.PrioritizeHighPerformance)
        {
            if (result.QualityRating.HasValue && result.QualityRating < 3.0m)
                reasons.Add($"Quality rating thấp ({result.QualityRating:F2})");

            if (result.OnTimeRate.HasValue && result.OnTimeRate < 0.7m)
                reasons.Add($"On-time rate thấp ({result.OnTimeRate:F2})");
        }

        return reasons;
    }

    private decimal CalculatePerformanceBasedOverallScore(ReviewerMatchingResult result, AutoAssignReviewerDTO criteria)
    {
        if (!result.IsEligible)
            return 0;

        // Weight factors for performance-based scoring
        decimal performanceWeight = criteria.PrioritizeHighPerformance ? 0.6m : 0.4m;
        decimal workloadWeight = 0.3m;
        decimal experienceWeight = 0.1m;

        // Experience bonus based on completed assignments
        decimal experienceScore = Math.Min(5.0m, result.CompletedAssignments * 0.1m);

        decimal overallScore = 
            (result.PerformanceScore * performanceWeight) +
            (result.WorkloadScore * workloadWeight) +
            (experienceScore * experienceWeight);

        return Math.Min(5.0m, overallScore);
    }

    #endregion
}