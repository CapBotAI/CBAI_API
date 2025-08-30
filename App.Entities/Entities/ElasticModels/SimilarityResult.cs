namespace App.Entities.ElasticModels;

public class SimilarityResult
{
    public int TopicId { get; set; }
    public string Title { get; set; } = null!;
    public string? Description { get; set; }
    public double SimilarityScore { get; set; }
    public string SimilarityPercentage => $"{SimilarityScore * 100:F2}%";
    public List<string> MatchedFields { get; set; } = new();
    public string? SupervisorName { get; set; }
    public string? SemesterName { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class DuplicateDetectionResult
{
    public int QueryTopicId { get; set; }
    public string QueryTopicTitle { get; set; } = null!;
    public List<SimilarityResult> SimilarTopics { get; set; } = new();
    public double HighestSimilarity { get; set; }
    public bool HasPotentialDuplicates => HighestSimilarity > 0.7;
    public string DetectionSummary { get; set; } = null!;
}