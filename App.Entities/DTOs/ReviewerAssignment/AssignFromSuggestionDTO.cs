using System;
using System.Collections.Generic;
using App.Entities.Enums;

namespace App.Entities.DTOs.ReviewerAssignment
{
    public class SuggestedReviewerItem
    {
        public int ReviewerId { get; set; }
        public AssignmentTypes? AssignmentType { get; set; }
        public DateTime? Deadline { get; set; }
        public decimal? SkillMatchScore { get; set; }
    }

    public class AssignFromSuggestionDTO
    {
        public int SubmissionId { get; set; }
        public List<SuggestedReviewerItem> Reviewers { get; set; } = new List<SuggestedReviewerItem>();
    }
}
