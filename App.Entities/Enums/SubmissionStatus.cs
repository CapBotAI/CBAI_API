namespace App.Entities.Enums;

public enum SubmissionStatus
{
    Pending = 1,
    UnderReview = 2,
    Duplicate = 3,
    [Obsolete("Không sử dụng chung chung thế này nữa.")]
    Completed = 4,
    RevisionRequired = 5,
    EscalatedToModerator = 6,
    Approved = 7,
    Rejected = 8
}
