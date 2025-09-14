using System;
using System.Collections.Generic;
using App.Commons;
using App.Entities.Entities.Core;
using App.Entities.Enums;

namespace App.Entities.Entities.App;

public partial class TopicVersion : CommonDataModel
{
    public int Id { get; set; }

    public int TopicId { get; set; }

    public int VersionNumber { get; set; }

    public string EN_Title { get; set; } = null!;

    public string? Description { get; set; }

    public string? Objectives { get; set; }

    public string? Methodology { get; set; }

    public string? ExpectedOutcomes { get; set; }

    public string? Requirements { get; set; }

    public string? DocumentUrl { get; set; }

    public TopicStatus Status { get; set; } = TopicStatus.Draft;

    public DateTime? SubmittedAt { get; set; }

    public int? SubmittedBy { get; set; }

    public string? VN_title { get; set; }
    public string? Problem { get; set; }

    public string? Context { get; set; }
    public string? Content { get; set; }

    public virtual Topic Topic { get; set; } = null!;
    public virtual User? SubmittedByUser { get; set; }
    public virtual ICollection<Submission> Submissions { get; set; } = new List<Submission>();
}