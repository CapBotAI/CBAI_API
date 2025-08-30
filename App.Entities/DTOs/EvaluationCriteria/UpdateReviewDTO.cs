using System.ComponentModel.DataAnnotations;
using App.Commons;
using App.Entities.Enums;

namespace App.Entities.DTOs.Review;

public class UpdateReviewDTO
{
    /// <summary>
    /// ID của review cần cập nhật
    /// </summary>
    [Required(ErrorMessage = ConstantModel.Required)]
    public int Id { get; set; }

    /// <summary>
    /// Danh sách điểm cho từng tiêu chí
    /// </summary>
    [Required(ErrorMessage = "Điểm đánh giá theo tiêu chí là bắt buộc")]
    public List<CriteriaScoreDTO> CriteriaScores { get; set; } = new List<CriteriaScoreDTO>();

    /// <summary>
    /// Nhận xét tổng quan
    /// </summary>
    [StringLength(2000, ErrorMessage = "Nhận xét tổng quan không được vượt quá 2000 ký tự")]
    public string? OverallComment { get; set; }

    /// <summary>
    /// Khuyến nghị cho submission
    /// </summary>
    public ReviewRecommendations Recommendation { get; set; } = ReviewRecommendations.MinorRevision;

    /// <summary>
    /// Thời gian dành để review (phút)
    /// </summary>
    [Range(1, 600, ErrorMessage = "Thời gian review phải từ 1 đến 600 phút")]
    public int? TimeSpentMinutes { get; set; }
}