using System.ComponentModel.DataAnnotations;
using App.Commons;

namespace App.Entities.DTOs.ReviewerAssignment;

public class BulkAssignReviewerDTO
{
  
    [Required(ErrorMessage = ConstantModel.Required)]
    [MinLength(1, ErrorMessage = "Phải có ít nhất 1 assignment")]
    public List<AssignReviewerDTO> Assignments { get; set; } = new();
}