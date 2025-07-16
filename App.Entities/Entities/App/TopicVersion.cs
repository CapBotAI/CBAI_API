using System;
using System.Collections.Generic;
using App.Entities.Entities.Core;
using App.Entities.Enums;

namespace App.Entities.Entities.App;

public partial class TopicVersion
{
    public int Id { get; set; }

    public int TopicId { get; set; }

    public int VersionNumber { get; set; }

    public string Title { get; set; } = null!;

    public string? Description { get; set; }

    public string? Objectives { get; set; }

    public string? Methodology { get; set; }

    public string? ExpectedOutcomes { get; set; }

    public string? Requirements { get; set; }

    public string? DocumentUrl { get; set; }

    public TopicStatus Status { get; set; } = TopicStatus.Draft;

    public DateTime? SubmittedAt { get; set; }

    public int? SubmittedBy { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual Topic Topic { get; set; } = null!;
    public virtual User? SubmittedByUser { get; set; }
    public virtual ICollection<Submission> Submissions { get; set; } = new List<Submission>();
}
