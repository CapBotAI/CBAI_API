using System.ComponentModel.DataAnnotations;
using App.Commons.Interfaces;
using App.Commons.ResponseModel;
using App.Entities.Entities.App;
using FS.Commons.Interfaces;

namespace App.Entities.DTOs.Topics;

public class CreateTopicDTO : IValiationPipeline, IEntity<Topic>
{
    [Required(ErrorMessage = "Tiêu đề chủ đề không được để trống")]
    [StringLength(500, ErrorMessage = "Tiêu đề chủ đề không được vượt quá 500 ký tự")]
    public string Title { get; set; } = null!;

    [StringLength(2000, ErrorMessage = "Mô tả không được vượt quá 2000 ký tự")]
    public string? Description { get; set; }

    [StringLength(2000, ErrorMessage = "Mục tiêu không được vượt quá 2000 ký tự")]
    public string? Objectives { get; set; }

    [Required(ErrorMessage = "Danh mục chủ đề không được để trống")]
    public int CategoryId { get; set; }

    [Required(ErrorMessage = "Học kỳ không được để trống")]
    public int SemesterId { get; set; }

    [Range(1, 5, ErrorMessage = "Số lượng sinh viên tối đa phải từ 1 đến 5")]
    public int MaxStudents { get; set; } = 1;

    // TopicVersion fields for initial version
    [StringLength(3000, ErrorMessage = "Phương pháp nghiên cứu không được vượt quá 3000 ký tự")]
    public string? Methodology { get; set; }

    [StringLength(3000, ErrorMessage = "Kết quả mong đợi không được vượt quá 3000 ký tự")]
    public string? ExpectedOutcomes { get; set; }

    [StringLength(2000, ErrorMessage = "Yêu cầu không được vượt quá 2000 ký tự")]
    public string? Requirements { get; set; }

    public string? DocumentUrl { get; set; }

    public Topic GetEntity()
    {
        return new Topic
        {
            Title = Title.Trim(),
            Description = Description?.Trim(),
            Objectives = Objectives?.Trim(),
            CategoryId = CategoryId,
            SemesterId = SemesterId,
            MaxStudents = MaxStudents,
        };
    }


    public BaseResponseModel Validate()
    {
        if (string.IsNullOrWhiteSpace(Title))
        {
            return new BaseResponseModel
            {
                IsSuccess = false,
                Message = "Tiêu đề chủ đề không được để trống"
            };
        }

        if (CategoryId <= 0)
        {
            return new BaseResponseModel
            {
                IsSuccess = false,
                Message = "Danh mục chủ đề không hợp lệ"
            };
        }

        if (SemesterId <= 0)
        {
            return new BaseResponseModel
            {
                IsSuccess = false,
                Message = "Học kỳ không hợp lệ"
            };
        }

        return new BaseResponseModel { IsSuccess = true };
    }
}
