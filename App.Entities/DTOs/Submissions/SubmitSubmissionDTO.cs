using System;
using System.ComponentModel.DataAnnotations;
using App.Commons.Interfaces;
using App.Commons.ResponseModel;

namespace App.Entities.DTOs.Submissions;

public class SubmitSubmissionDTO : IValidationPipeline
{
    [Required] public int Id { get; set; }
    public BaseResponseModel Validate() => new BaseResponseModel { IsSuccess = true };
}
