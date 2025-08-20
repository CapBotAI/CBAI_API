using System.ComponentModel.DataAnnotations;
using App.Commons;
using App.Entities.Enums;

namespace App.Entities.DTOs.Review;

public class ModeratorFinalReviewDTO
{
    [Required(ErrorMessage = ConstantModel.Required)]
    public int SubmissionId { get; set; }

    [Required(ErrorMessage = ConstantModel.Required)]
    public ReviewRecommendations FinalRecommendation { get; set; }

    [Range(0, 10, ErrorMessage = "Điểm số phải từ 0 đến 10")]
    public decimal? FinalScore { get; set; }

    [StringLength(2000, ErrorMessage = "Ghi chú không được vượt quá 2000 ký tự")]
    public string? ModeratorNotes { get; set; }

    public DateTime? RevisionDeadline { get; set; }
}