using System;
using App.Commons.Paging;
using App.Entities.Enums;

namespace App.Entities.DTOs.Submissions;

public class GetSubmissionsQueryDTO : PagingModel
{
    public int? TopicVersionId { get; set; }
    public int? PhaseId { get; set; }
    public int? SemesterId { get; set; }
    public SubmissionStatus? Status { get; set; }
}
