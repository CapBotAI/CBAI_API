using App.Entities.Entities.App;
using Microsoft.AspNetCore.Identity;

namespace App.Entities.Entities.Core;

public partial class User : IdentityUser<int>
{
    public DateTime CreatedAt { get; set; }
    public string? LastModifiedBy { get; set; }
    public DateTime LastModifiedAt { get; set; }
    public DateTime? DeletedAt { get; set; }

    public virtual ICollection<UserClaim> Claims { get; set; }
    public virtual ICollection<UserLogin> Logins { get; set; }
    public virtual ICollection<UserToken> Tokens { get; set; }
    public virtual ICollection<UserRole> UserRoles { get; set; }
    public virtual ICollection<Topic> Topics { get; set; }
    public virtual ICollection<TopicVersion> TopicVersions { get; set; }
    public virtual ICollection<LecturerSkill> LecturerSkills { get; set; }
    public virtual ICollection<Submission> Submissions { get; set; }
    public virtual ICollection<ReviewerAssignment> ReviewerAssignments { get; set; }
    public virtual ICollection<SubmissionWorkflowLog> SubmissionWorkflowLogs { get; set; }
    public virtual ICollection<ReviewerPerformance> ReviewerPerformances { get; set; }
    public virtual ICollection<SystemNotification> SystemNotifications { get; set; }
    public virtual UserProfile? Profile { get; set; }
}