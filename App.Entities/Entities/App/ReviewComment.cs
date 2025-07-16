using System;
using App.Commons;
using App.Entities.Enums;

namespace App.Entities.Entities.App;

public partial class ReviewComment : CommonDataModel
{
    public int Id { get; set; }

    public int ReviewId { get; set; }

    public string? SectionName { get; set; }

    public int? LineNumber { get; set; }

    public string CommentText { get; set; } = null!;

    public CommentTypes CommentType { get; set; } = CommentTypes.Suggestion;

    public PriorityLevels Priority { get; set; } = PriorityLevels.Medium;

    public bool IsResolved { get; set; } = false;

    public virtual Review Review { get; set; } = null!;
}
