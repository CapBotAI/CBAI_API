namespace App.Entities.ElasticModels;

public class TopicDocument
{
    public int Id { get; set; }
    public string Title { get; set; } = null!;
    public string? Description { get; set; }
    public string? Objectives { get; set; }
    public int SupervisorId { get; set; }
    public int? CategoryId { get; set; }
    public int SemesterId { get; set; }
    public int MaxStudents { get; set; }
    public bool IsLegacy { get; set; }
    public bool IsApproved { get; set; }
    public string? CategoryName { get; set; }
    public string? SemesterName { get; set; }
    public string? SupervisorName { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastModifiedAt { get; set; }
    public bool IsActive { get; set; }
    
    
    public string FullContent { get; set; } = null!;
    

    public List<string>? Keywords { get; set; }
}