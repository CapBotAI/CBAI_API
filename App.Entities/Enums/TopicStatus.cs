using System;
namespace App.Entities.Enums;

public enum TopicStatus
{
    Draft = 1,
    SubmissionPending = 2,
    Submitted = 3,
    [Obsolete("Không dùng cho TopicVersion nữa; review dồn về Submission")]
    UnderReview = 4,
    [Obsolete] Approved = 5,
    [Obsolete] Rejected = 6,
    [Obsolete] RevisionRequired = 7,
    Archived = 8
}
