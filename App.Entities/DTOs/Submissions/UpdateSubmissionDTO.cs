using System;
using System.ComponentModel.DataAnnotations;
using App.Commons.Interfaces;
using App.Commons.ResponseModel;
using App.Entities.Entities.App;
using FS.Commons.Interfaces;

namespace App.Entities.DTOs.Submissions;

public class UpdateSubmissionDTO : IValidationPipeline, IEntity<Submission>
{
    [Required] public int Id { get; set; }
    [Required] public int PhaseId { get; set; }
    public string? DocumentUrl { get; set; }
    public string? AdditionalNotes { get; set; }
    public Submission GetEntity() => new Submission
    {
        Id = Id,
        PhaseId = PhaseId,
        DocumentUrl = DocumentUrl?.Trim(),
        AdditionalNotes = AdditionalNotes?.Trim(),
    };
    public BaseResponseModel Validate()
    {
        return new BaseResponseModel
        {
            IsSuccess = true,
        };
    }
}
