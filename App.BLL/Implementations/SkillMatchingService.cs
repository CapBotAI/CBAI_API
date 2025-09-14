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
using System.Text.RegularExpressions;

namespace App.BLL.Implementations;

public class SkillMatchingService : ISkillMatchingService
{
    private readonly IUnitOfWork _unitOfWork;

    public SkillMatchingService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<decimal> CalculateSkillMatchScoreAsync(int reviewerId, List<string> topicSkillTags)
    {
        if (!topicSkillTags.Any())
            return 0;

        // Get reviewer skills
        var reviewerSkillsOptions = new QueryOptions<LecturerSkill>
        {
            Predicate = ls => ls.LecturerId == reviewerId
        };
        var reviewerSkills = await _unitOfWork.GetRepo<LecturerSkill>().GetAllAsync(reviewerSkillsOptions);

        if (!reviewerSkills.Any())
            return 0;

        decimal totalScore = 0;
        int matchedSkills = 0;

        foreach (var topicSkill in topicSkillTags)
        {
            var matchingSkill = reviewerSkills.FirstOrDefault(rs =>
                IsSkillMatch(rs.SkillTag, topicSkill));

            if (matchingSkill != null)
            {
                // Score based on proficiency level
                decimal skillScore = (int)matchingSkill.ProficiencyLevel; // 1-4
                totalScore += skillScore;
                matchedSkills++;
            }
        }

        if (matchedSkills == 0)
            return 0;

        // Calculate average score and apply match coverage bonus
        decimal averageScore = totalScore / matchedSkills;
        decimal coverageBonus = (decimal)matchedSkills / topicSkillTags.Count * 0.5m;

        return Math.Min(5.0m, averageScore + coverageBonus);
    }

    public async Task<List<string>> ExtractTopicSkillTagsAsync(int submissionId)
    {
        // Get submission with topic info
        var submissionOptions = new QueryOptions<Submission>
        {
            Predicate = s => s.Id == submissionId,
            IncludeProperties = new List<Expression<Func<Submission, object>>>
            {
                s => s.TopicVersion,
                s => s.TopicVersion.Topic,
                s => s.TopicVersion.Topic.Category
            }
        };
        var submission = await _unitOfWork.GetRepo<Submission>().GetSingleAsync(submissionOptions);

        if (submission == null)
            return new List<string>();

        var skillTags = new List<string>();

        // Extract from topic title and description
        var topic = submission.TopicVersion.Topic;
        if (!string.IsNullOrEmpty(topic.EN_Title))
        {
            skillTags.AddRange(ExtractSkillsFromText(topic.EN_Title));
        }

        if (!string.IsNullOrEmpty(topic.Description))
        {
            skillTags.AddRange(ExtractSkillsFromText(topic.Description));
        }

        if (!string.IsNullOrEmpty(topic.Objectives))
        {
            skillTags.AddRange(ExtractSkillsFromText(topic.Objectives));
        }

        // Add category as skill tag
        if (topic.Category != null && !string.IsNullOrEmpty(topic.Category.Name))
        {
            skillTags.Add(topic.Category.Name);
        }

        return skillTags.Distinct().ToList();
    }


    public async Task<List<ReviewerMatchingResult>> FindBestMatchingReviewersAsync(
        int submissionId, AutoAssignReviewerDTO criteria)
    {
        // Get topic skill tags - luôn extract từ submission vì không có TopicSkillTags trong DTO nữa
        var topicSkillTags = await ExtractTopicSkillTagsAsync(submissionId);

        // Get all reviewers with their skills and performance data
        var userRoleOptions = new QueryOptions<UserRole>
        {
            IncludeProperties = new List<Expression<Func<UserRole, object>>>
            {
                ur => ur.User,
                ur => ur.User.LecturerSkills,
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

            // Calculate skill match score
            matchingResult.SkillMatchScore = await CalculateSkillMatchScoreAsync(reviewer.Id, topicSkillTags);
            matchingResult.ReviewerSkills = reviewer.LecturerSkills.ToDictionary(
                ls => ls.SkillTag,
                ls => ls.ProficiencyLevel);
            matchingResult.MatchedSkills = GetMatchedSkills(reviewer.LecturerSkills.ToList(), topicSkillTags);

            // Calculate workload score
            matchingResult.WorkloadScore = await CalculateWorkloadScoreAsync(reviewer.Id);

            // Get active assignments count
            var activeAssignmentOptions = new QueryOptions<ReviewerAssignment>
            {
                Predicate = ra => ra.ReviewerId == reviewer.Id &&
                                  (ra.Status == AssignmentStatus.Assigned || ra.Status == AssignmentStatus.InProgress)
            };
            var activeAssignments =
                await _unitOfWork.GetRepo<ReviewerAssignment>().GetAllAsync(activeAssignmentOptions);
            matchingResult.CurrentActiveAssignments = activeAssignments.Count();

            // Calculate performance score
            matchingResult.PerformanceScore = await CalculatePerformanceScoreAsync(reviewer.Id);

            // Get performance details
            var performance = reviewer.ReviewerPerformances.FirstOrDefault();
            if (performance != null)
            {
                matchingResult.CompletedAssignments = performance.CompletedAssignments;
                matchingResult.AverageScoreGiven = performance.AverageScoreGiven;
                matchingResult.OnTimeRate = performance.OnTimeRate;
                matchingResult.QualityRating = performance.QualityRating;
            }

            // Check eligibility
            matchingResult.IsEligible = IsReviewerEligible(matchingResult, criteria);
            if (!matchingResult.IsEligible)
            {
                matchingResult.IneligibilityReasons = GetIneligibilityReasons(matchingResult, criteria);
            }

            // Calculate overall score
            matchingResult.OverallScore = CalculateOverallScore(matchingResult, criteria);

            matchingResults.Add(matchingResult);
        }

        return matchingResults.OrderByDescending(r => r.OverallScore).ToList();
    }

    public async Task<decimal> CalculatePerformanceScoreAsync(int reviewerId, int? semesterId = null)
    {
        var performanceOptions = new QueryOptions<ReviewerPerformance>
        {
            Predicate = rp => rp.ReviewerId == reviewerId &&
                              (!semesterId.HasValue || rp.SemesterId == semesterId.Value)
        };
        var performances = await _unitOfWork.GetRepo<ReviewerPerformance>().GetAllAsync(performanceOptions);

        if (!performances.Any())
            return 2.5m; // Default neutral score

        var avgPerformance = performances.First(); // Most recent or specific semester

        decimal performanceScore = 0;
        int factors = 0;

        // On-time rate (0-1) -> (0-2)
        if (avgPerformance.OnTimeRate.HasValue)
        {
            performanceScore += (decimal)avgPerformance.OnTimeRate.Value * 2;
            factors++;
        }

        // Quality rating (0-5)
        if (avgPerformance.QualityRating.HasValue)
        {
            performanceScore += (decimal)avgPerformance.QualityRating.Value;
            factors++;
        }

        // Completion rate
        if (avgPerformance.TotalAssignments > 0)
        {
            decimal completionRate = (decimal)avgPerformance.CompletedAssignments / avgPerformance.TotalAssignments;
            performanceScore += completionRate * 2;
            factors++;
        }

        return factors > 0 ? Math.Min(5.0m, performanceScore / factors) : 2.5m;
    }

    public async Task<decimal> CalculateWorkloadScoreAsync(int reviewerId)
    {
        var activeAssignmentOptions = new QueryOptions<ReviewerAssignment>
        {
            Predicate = ra => ra.ReviewerId == reviewerId &&
                              (ra.Status == AssignmentStatus.Assigned || ra.Status == AssignmentStatus.InProgress)
        };
        var activeAssignments = await _unitOfWork.GetRepo<ReviewerAssignment>().GetAllAsync(activeAssignmentOptions);

        int activeCount = activeAssignments.Count();

        // Workload score: higher load = lower score
        // 0 assignments = 5.0, 10+ assignments = 0.0
        return Math.Max(0, 5.0m - (activeCount * 0.5m));
    }

    #region Private Helper Methods

    private bool IsSkillMatch(string reviewerSkill, string topicSkill)
    {
        var normalizedReviewerSkill = reviewerSkill.ToLower().Trim();
        var normalizedTopicSkill = topicSkill.ToLower().Trim();

        // Exact match
        if (normalizedReviewerSkill == normalizedTopicSkill)
            return true;

        // Partial match
        if (normalizedReviewerSkill.Contains(normalizedTopicSkill) ||
            normalizedTopicSkill.Contains(normalizedReviewerSkill))
            return true;

        // Technology synonyms/related terms
        var synonyms = GetTechnologySynonyms();
        if (synonyms.ContainsKey(normalizedTopicSkill))
        {
            return synonyms[normalizedTopicSkill].Any(synonym =>
                normalizedReviewerSkill.Contains(synonym));
        }

        return false;
    }

    private List<string> ExtractSkillsFromText(string text)
    {
        var skills = new List<string>();

        // Common technology keywords
        var techKeywords = new[]
        {
            "C#", "Java", "Python", "JavaScript", "React", "Angular", "Vue",
            "Node.js", "ASP.NET", ".NET", "Spring", "Django", "Flask",
            "SQL", "MySQL", "PostgreSQL", "MongoDB", "Redis",
            "AI", "Machine Learning", "Deep Learning", "Data Science",
            "Mobile", "Android", "iOS", "Flutter", "React Native",
            "Web", "API", "REST", "GraphQL", "Microservices",
            "Cloud", "AWS", "Azure", "GCP", "Docker", "Kubernetes"
        };

        foreach (var keyword in techKeywords)
        {
            if (text.Contains(keyword, StringComparison.OrdinalIgnoreCase))
            {
                skills.Add(keyword);
            }
        }

        return skills;
    }

    private Dictionary<string, List<string>> GetTechnologySynonyms()
    {
        return new Dictionary<string, List<string>>
        {
            ["ai"] = new List<string> { "artificial intelligence", "machine learning", "ml", "deep learning" },
            ["web"] = new List<string> { "website", "web application", "webapp" },
            ["mobile"] = new List<string> { "app", "application", "android", "ios" },
            ["database"] = new List<string> { "db", "sql", "mysql", "postgresql", "mongodb" },
            ["api"] = new List<string> { "rest", "restful", "web service", "microservice" }
        };
    }

    private List<string> GetMatchedSkills(List<LecturerSkill> reviewerSkills, List<string> topicSkills)
    {
        var matched = new List<string>();

        foreach (var topicSkill in topicSkills)
        {
            if (reviewerSkills.Any(rs => IsSkillMatch(rs.SkillTag, topicSkill)))
            {
                matched.Add(topicSkill);
            }
        }

        return matched;
    }

    private bool IsReviewerEligible(ReviewerMatchingResult result, AutoAssignReviewerDTO criteria)
    {
        // Check minimum skill match score
        if (result.SkillMatchScore < criteria.MinimumSkillMatchScore)
            return false;

        // Check maximum workload
        if (result.CurrentActiveAssignments >= criteria.MaxWorkload)
            return false;

        return true;
    }

    private List<string> GetIneligibilityReasons(ReviewerMatchingResult result, AutoAssignReviewerDTO criteria)
    {
        var reasons = new List<string>();

        if (result.SkillMatchScore < criteria.MinimumSkillMatchScore)
            reasons.Add(
                $"Skill match score ({result.SkillMatchScore:F2}) dưới mức yêu cầu ({criteria.MinimumSkillMatchScore:F2})");

        if (result.CurrentActiveAssignments >= criteria.MaxWorkload)
            reasons.Add($"Quá tải assignment ({result.CurrentActiveAssignments}/{criteria.MaxWorkload})");

        return reasons;
    }

    private decimal CalculateOverallScore(ReviewerMatchingResult result, AutoAssignReviewerDTO criteria)
    {
        if (!result.IsEligible)
            return 0;

        // Weight factors
        decimal skillWeight = 0.4m;
        decimal workloadWeight = 0.3m;
        decimal performanceWeight = criteria.PrioritizeHighPerformance ? 0.3m : 0.1m;

        decimal overallScore =
            (result.SkillMatchScore * skillWeight) +
            (result.WorkloadScore * workloadWeight) +
            (result.PerformanceScore * performanceWeight);

        return Math.Min(5.0m, overallScore);
    }

    #endregion
}