using System.ComponentModel.DataAnnotations;
using App.Commons.ResponseModel;

namespace App.Entities.DTOs.TopicVersions;

public class CreateTopicVersionDTO
{
    [Required(ErrorMessage = "Id chủ đề không được để trống")]
    public int TopicId { get; set; }

    [Required(ErrorMessage = "Tiêu đề không được để trống")]
    [StringLength(500, ErrorMessage = "Tiêu đề không được vượt quá 500 ký tự")]
    public string Title { get; set; } = null!;

    [StringLength(2000, ErrorMessage = "Mô tả không được vượt quá 2000 ký tự")]
    public string? Description { get; set; }

    [StringLength(2000, ErrorMessage = "Mục tiêu không được vượt quá 2000 ký tự")]
    public string? Objectives { get; set; }

    [StringLength(3000, ErrorMessage = "Phương pháp nghiên cứu không được vượt quá 3000 ký tự")]
    public string? Methodology { get; set; }

    [StringLength(3000, ErrorMessage = "Kết quả mong đợi không được vượt quá 3000 ký tự")]
    public string? ExpectedOutcomes { get; set; }

    [StringLength(2000, ErrorMessage = "Yêu cầu không được vượt quá 2000 ký tự")]
    public string? Requirements { get; set; }

    public string? DocumentUrl { get; set; }

    public BaseResponseModel Validate()
    {
        if (TopicId <= 0)
        {
            return new BaseResponseModel
            {
                IsSuccess = false,
                Message = "Id chủ đề không hợp lệ"
            };
        }

        if (string.IsNullOrWhiteSpace(Title))
        {
            return new BaseResponseModel
            {
                IsSuccess = false,
                Message = "Tiêu đề không được để trống"
            };
        }

        return new BaseResponseModel { IsSuccess = true };
    }
}
