using System;
using System.ComponentModel.DataAnnotations;
using App.Commons.Interfaces;
using App.Commons.ResponseModel;

namespace App.Entities.DTOs.Submissions;

public class ResubmitSubmissionDTO : IValidationPipeline
{
    [Required] public int Id { get; set; }
    [Required] public int TopicVersionId { get; set; }
    public BaseResponseModel Validate() => new BaseResponseModel { IsSuccess = true };
}
