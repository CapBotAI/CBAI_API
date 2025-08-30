using System.ComponentModel.DataAnnotations;
using App.Commons.Interfaces;
using App.Commons.ResponseModel;
using App.Entities.Entities.App;
using FS.Commons.Interfaces;

namespace App.Entities.DTOs.TopicVersions;

public class CreateTopicVersionDTO : IEntity<TopicVersion>, IValidationPipeline
{
    [Required(ErrorMessage = "Id chủ đề không được để trống")]
    public int TopicId { get; set; }

    [Required(ErrorMessage = "Tiêu đề không được để trống")]
    [StringLength(500, ErrorMessage = "Tiêu đề không được vượt quá 500 ký tự")]
    public string Title { get; set; } = null!;
    public string? Description { get; set; }

    public string? Objectives { get; set; }

    public string? Methodology { get; set; }
    public string? ExpectedOutcomes { get; set; }
    public string? Requirements { get; set; }

    public long? FileId { get; set; }

    public string? DocumentUrl { get; set; }

    public TopicVersion GetEntity()
    {
        return new TopicVersion
        {
            TopicId = TopicId,
            Title = Title.Trim(),
            Description = Description?.Trim(),
            Objectives = Objectives?.Trim(),
            Methodology = Methodology?.Trim(),
            ExpectedOutcomes = ExpectedOutcomes?.Trim(),
            Requirements = Requirements?.Trim(),
            DocumentUrl = DocumentUrl?.Trim(),
        };
    }


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
