using System;
using System.ComponentModel.DataAnnotations;
using App.Commons.Interfaces;
using App.Commons.ResponseModel;
using FS.Commons.Interfaces;
using Microsoft.AspNetCore.Http;

namespace App.Entities.DTOs.Semesters;

public class UpdateSemesterDTO : IValidationPipeline
{
    [Required(ErrorMessage = "Id là bắt buộc")]
    public int Id { get; set; }

    [Required(ErrorMessage = "Tên học kỳ là bắt buộc")]
    public string Name { get; set; } = null!;

    [Required(ErrorMessage = "Ngày bắt đầu là bắt buộc")]
    [DataType(DataType.Date)]
    public DateTime StartDate { get; set; }

    [Required(ErrorMessage = "Ngày kết thúc là bắt buộc")]
    [DataType(DataType.Date)]
    public DateTime EndDate { get; set; }

    public BaseResponseModel Validate()
    {
        var validationResult = new BaseResponseModel();
        if (StartDate >= EndDate)
        {
            validationResult.Message = "Ngày bắt đầu phải nhỏ hơn ngày kết thúc";
            validationResult.IsSuccess = false;
            validationResult.StatusCode = StatusCodes.Status422UnprocessableEntity;
            return validationResult;
        }

        if (StartDate < DateTime.Now)
        {
            validationResult.Message = "Ngày bắt đầu phải lớn hơn ngày hiện tại";
            validationResult.IsSuccess = false;
            validationResult.StatusCode = StatusCodes.Status422UnprocessableEntity;
            return validationResult;
        }

        if (EndDate < DateTime.Now)
        {
            validationResult.Message = "Ngày kết thúc phải lớn hơn ngày hiện tại";
            validationResult.IsSuccess = false;
            validationResult.StatusCode = StatusCodes.Status422UnprocessableEntity;
            return validationResult;
        }

        validationResult.IsSuccess = true;
        return validationResult;
    }

}
