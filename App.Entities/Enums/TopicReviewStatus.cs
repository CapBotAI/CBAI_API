namespace App.Entities.Enums;

public enum TopicReviewStatus
{
    PendingReview = 0,      
    InProgress = 1,         
    Approved = 2,           
    RequiresRevision = 3,  
    Failed = 4,             
    Conflicted = 5,         
    PendingModerator = 6,   
    RevisionOverdue = 7,    
    Resubmitted = 8        
}