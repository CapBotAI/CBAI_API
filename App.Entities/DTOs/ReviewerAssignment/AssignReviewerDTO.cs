using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;
using App.Commons;
using App.Entities.Enums;

namespace App.Entities.DTOs.ReviewerAssignment;

public class AssignReviewerDTO : IValidatableObject
{
   
    [Required(ErrorMessage = ConstantModel.Required)]
    public int SubmissionId { get; set; }

  
    [Required(ErrorMessage = ConstantModel.Required)]
    public int ReviewerId { get; set; }

    [Required(ErrorMessage = ConstantModel.Required)]
    public AssignmentTypes AssignmentType { get; set; } = AssignmentTypes.Primary;

   
    public DateTime? Deadline { get; set; }

    
    [Range(0, 5, ErrorMessage = "Điểm khớp skill phải từ 0 đến 5")]
    public decimal? SkillMatchScore { get; set; }

    
    [StringLength(500, ErrorMessage = "Ghi chú không được vượt quá 500 ký tự")]
    public string? Notes { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Deadline.HasValue && Deadline.Value <= DateTime.UtcNow)
        {
            yield return new ValidationResult("Deadline phải ở tương lai", new[] { nameof(Deadline) });
        }
    }
}