using System.ComponentModel.DataAnnotations;
using App.Commons.Interfaces;
using App.Commons.ResponseModel;
using App.Entities.Enums;

namespace App.Entities.DTOs.TopicVersions;

public class ReviewTopicVersionDTO : IValidationPipeline
{
    [Required(ErrorMessage = "Id phiên bản không được để trống")]
    public int VersionId { get; set; }

    [Required(ErrorMessage = "Trạng thái không được để trống")]
    public TopicStatus Status { get; set; }

    [StringLength(1000, ErrorMessage = "Ghi chú không được vượt quá 1000 ký tự")]
    public string? ReviewNote { get; set; }

    public BaseResponseModel Validate()
    {
        if (VersionId <= 0)
        {
            return new BaseResponseModel
            {
                IsSuccess = false,
                Message = "Id phiên bản không hợp lệ"
            };
        }

        if (Status != TopicStatus.Approved && Status != TopicStatus.Rejected && Status != TopicStatus.RevisionRequired)
        {
            return new BaseResponseModel
            {
                IsSuccess = false,
                Message = "Trạng thái review không hợp lệ"
            };
        }

        return new BaseResponseModel { IsSuccess = true };
    }
}
