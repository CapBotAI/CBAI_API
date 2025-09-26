namespace App.Entities.DTOs.EvaluationCriteria;

public class EvaluationCriteriaResponseDTO
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public int MaxScore { get; set; }
    public decimal Weight { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime LastModifiedAt { get; set; }
    public string? CreatedBy { get; set; }
    public string? LastModifiedBy { get; set; }
    public bool IsActive { get; set; }
    
    /// <summary>
    /// Tên semester áp dụng tiêu chí này (null nếu là tiêu chí chung)
    /// </summary>
    public string? SemesterName { get; set; }
}