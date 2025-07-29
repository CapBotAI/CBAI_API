using System.ComponentModel.DataAnnotations;
using App.Commons.Interfaces;
using App.Commons.ResponseModel;

namespace App.Entities.DTOs.TopicVersions;

public class SubmitTopicVersionDTO : IValidationPipeline
{
    [Required(ErrorMessage = "Id phiên bản không được để trống")]
    public int VersionId { get; set; }

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

        return new BaseResponseModel { IsSuccess = true };
    }
}
