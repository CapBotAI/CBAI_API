using System.Collections.Generic;

namespace App.Entities.DTOs.ReviewerSuggestion
{
    /// <summary>
    /// Output DTO: reviewer suggestions and optional AI explanation.
    /// </summary>
    public class ReviewerSuggestionOutputDTO
    {
        public List<ReviewerSuggestionDTO> Suggestions { get; set; } = new();
        public string? AIExplanation { get; set; }
        // Assignment results produced when the caller requested auto-assign
        public List<App.Entities.DTOs.ReviewerAssignment.ReviewerAssignmentResponseDTO>? AssignmentResults { get; set; } = new();
        // Any assignment-level error messages (one string per failed assignment)
        public List<string>? AssignmentErrors { get; set; } = new();
        // Messages produced when skipping reviewers (e.g., too many active assignments)
        public List<string>? SkipMessages { get; set; } = new();
    }
}