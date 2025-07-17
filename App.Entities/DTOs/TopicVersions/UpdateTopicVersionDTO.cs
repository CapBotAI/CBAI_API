using System.ComponentModel.DataAnnotations;
using App.Commons.ResponseModel;

namespace App.Entities.DTOs.TopicVersions;

public class UpdateTopicVersionDTO
{
    [Required(ErrorMessage = "Id phiên bản không được để trống")]
    public int Id { get; set; }

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
        if (Id <= 0)
        {
            return new BaseResponseModel
            {
                IsSuccess = false,
                Message = "Id phiên bản không hợp lệ"
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
