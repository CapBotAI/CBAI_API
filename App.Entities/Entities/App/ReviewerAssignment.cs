using System;
using System.Collections.Generic;
using App.Entities.Entities.Core;
using App.Entities.Enums;

namespace App.Entities.Entities.App;

public partial class ReviewerAssignment
{
    public int Id { get; set; }

    public int SubmissionId { get; set; }

    public int ReviewerId { get; set; }

    public int AssignedBy { get; set; }

    public AssignmentTypes AssignmentType { get; set; } = AssignmentTypes.Primary;

    public decimal? SkillMatchScore { get; set; }

    public DateTime? Deadline { get; set; }

    public AssignmentStatus Status { get; set; } = AssignmentStatus.Assigned;

    public DateTime AssignedAt { get; set; }

    public DateTime? StartedAt { get; set; }

    public DateTime? CompletedAt { get; set; }

    public virtual Submission Submission { get; set; } = null!;
    public virtual User Reviewer { get; set; } = null!;
    public virtual User AssignedByUser { get; set; } = null!;
    public virtual ICollection<Review> Reviews { get; set; } = new List<Review>();
}
