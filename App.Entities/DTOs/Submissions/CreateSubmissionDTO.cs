using System;
using System.ComponentModel.DataAnnotations;
using App.Commons.Interfaces;
using App.Commons.ResponseModel;
using App.Entities.Entities.App;
using App.Entities.Enums;
using FS.Commons.Interfaces;
using Microsoft.AspNetCore.Http;

namespace App.Entities.DTOs.Submissions;

public class CreateSubmissionDTO : IValidationPipeline, IEntity<Submission>
{
    [Required] public int TopicId { get; set; }
    [Required] public int PhaseId { get; set; }
    public string? DocumentUrl { get; set; }
    public string? AdditionalNotes { get; set; }

    public long? FileId { get; set; }

    public Submission GetEntity() => new Submission
    {
        TopicId = TopicId,
        PhaseId = PhaseId,
        DocumentUrl = DocumentUrl?.Trim(),
        AdditionalNotes = AdditionalNotes?.Trim(),
        SubmissionRound = 1,
        Status = SubmissionStatus.Pending
    };
    public BaseResponseModel Validate()
    { /* ids > 0, url length, ... */
        return new BaseResponseModel
        {
            IsSuccess = true,
        };
    }
}
