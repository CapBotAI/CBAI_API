using System.ComponentModel.DataAnnotations;
using App.Commons.Interfaces;
using App.Commons.ResponseModel;
using FS.Commons.Interfaces;
using Microsoft.AspNetCore.Http;

namespace App.Entities.DTOs.PhaseTypes;

public class CreatePhaseTypeDTO : IEntity<App.Entities.Entities.App.PhaseType>, IValidationPipeline
{
    [Required(ErrorMessage = "Tên loại giai đoạn là bắt buộc")]
    [StringLength(100, ErrorMessage = "Tên loại giai đoạn không được vượt quá 100 ký tự")]
    public string Name { get; set; } = null!;

    [StringLength(500, ErrorMessage = "Mô tả không được vượt quá 500 ký tự")]
    public string? Description { get; set; }

    public App.Entities.Entities.App.PhaseType GetEntity()
    {
        return new App.Entities.Entities.App.PhaseType
        {
            Name = Name,
            Description = Description,
            IsActive = true,
        };
    }

    public BaseResponseModel Validate()
    {
        var validationResult = new BaseResponseModel();

        if (string.IsNullOrWhiteSpace(Name))
        {
            validationResult.Message = "Tên loại giai đoạn không được để trống";
            validationResult.IsSuccess = false;
            validationResult.StatusCode = StatusCodes.Status422UnprocessableEntity;
            return validationResult;
        }

        validationResult.IsSuccess = true;
        return validationResult;
    }
}
