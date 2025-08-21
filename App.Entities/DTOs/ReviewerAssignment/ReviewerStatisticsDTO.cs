using App.Entities.Enums;

namespace App.Entities.DTOs.ReviewerAssignment;

public class ReviewerStatisticsDTO
{
    public int ReviewerId { get; set; }
    public string ReviewerName { get; set; } = null!;
    public int TotalAssignments { get; set; }
    public int CompletedAssignments { get; set; }
    public int InProgressAssignments { get; set; }
    public int PendingAssignments { get; set; }
    public int OverdueAssignments { get; set; }
    public decimal CompletionRate { get; set; }
    public decimal AverageReviewTime { get; set; } // Tính bằng giờ
    public DateTime? LastReviewDate { get; set; }
    
    public Dictionary<string, int> AssignmentsByStatus { get; set; } = new Dictionary<string, int>();
    public List<RecentAssignmentDTO> RecentAssignments { get; set; } = new List<RecentAssignmentDTO>();
}

public class RecentAssignmentDTO
{
    public int AssignmentId { get; set; }
    public string TopicTitle { get; set; } = null!;
    public string StudentName { get; set; } = null!;
    public AssignmentStatus Status { get; set; }
    public DateTime AssignedAt { get; set; }
    public DateTime? Deadline { get; set; }
    public bool IsOverdue { get; set; }
}