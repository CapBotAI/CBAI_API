using System;
using App.Commons;

namespace App.Entities.Entities.App;

public partial class ReviewCriteriaScore : CommonDataModel
{
    public int Id { get; set; }

    public int ReviewId { get; set; }

    public int CriteriaId { get; set; }

    public decimal Score { get; set; }

    public string? Comment { get; set; }

    public virtual Review Review { get; set; } = null!;
    public virtual EvaluationCriteria Criteria { get; set; } = null!;
}
