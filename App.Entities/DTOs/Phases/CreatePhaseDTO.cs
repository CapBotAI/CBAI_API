using System;
using System.ComponentModel.DataAnnotations;
using App.Commons.Interfaces;
using App.Commons.ResponseModel;
using App.Entities.Entities.App;
using FS.Commons.Interfaces;
using Microsoft.AspNetCore.Http;

namespace App.Entities.DTOs.Phases;

public class CreatePhaseDTO : IValidationPipeline, IEntity<Phase>
{
    public int SemesterId { get; set; }
    public int PhaseTypeId { get; set; }

    [Required]
    public string Name { get; set; } = null!;

    [DataType(DataType.Date)]
    public DateTime StartDate { get; set; }

    [DataType(DataType.Date)]
    public DateTime EndDate { get; set; }

    [DataType(DataType.Date)]
    public DateTime? SubmissionDeadline { get; set; }
    public Phase GetEntity() => new Phase
    {
        SemesterId = SemesterId,
        PhaseTypeId = PhaseTypeId,
        Name = Name.Trim(),
        StartDate = StartDate,
        EndDate = EndDate,
        SubmissionDeadline = SubmissionDeadline
    };

    BaseResponseModel IValidationPipeline.Validate()
    {
        /* check StartDate < EndDate, deadline <= EndDate, Name not empty, ids > 0 */
        if (StartDate >= EndDate)
        {
            return new BaseResponseModel
            {
                IsSuccess = false,
                StatusCode = StatusCodes.Status422UnprocessableEntity,
                Message = "StartDate must be less than EndDate"
            };
        }

        if (SubmissionDeadline.HasValue && SubmissionDeadline.Value > EndDate)
        {
            return new BaseResponseModel
            {
                IsSuccess = false,
                StatusCode = StatusCodes.Status422UnprocessableEntity,
                Message = "SubmissionDeadline must be less than or equal to EndDate"
            };
        }

        if (string.IsNullOrWhiteSpace(Name))
        {
            return new BaseResponseModel
            {
                IsSuccess = false,
                StatusCode = StatusCodes.Status422UnprocessableEntity,
                Message = "Name is required"
            };
        }

        if (SemesterId <= 0 || PhaseTypeId <= 0)
        {
            return new BaseResponseModel
            {
                IsSuccess = false,
                StatusCode = StatusCodes.Status422UnprocessableEntity,
                Message = "Invalid SemesterId or PhaseTypeId"
            };
        }

        return new BaseResponseModel { IsSuccess = true };
    }
}
