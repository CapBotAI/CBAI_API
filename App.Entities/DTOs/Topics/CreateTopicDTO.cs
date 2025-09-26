using System.ComponentModel.DataAnnotations;
using App.Commons.Interfaces;
using App.Commons.ResponseModel;
using App.Entities.Entities.App;
using FS.Commons.Interfaces;

namespace App.Entities.DTOs.Topics;

public class CreateTopicDTO : IValidationPipeline, IEntity<Topic>
{
    [Required(ErrorMessage = "Tiêu đề chủ đề không được để trống")]
    [StringLength(500, ErrorMessage = "Tiêu đề chủ đề không được vượt quá 500 ký tự")]
    public string EN_Title { get; set; } = null!;

    public string? Abbreviation { get; set; }
    public string? VN_title { get; set; }
    public string? Problem { get; set; }

    public string? Context { get; set; }

    public string? Content { get; set; }

    public string? Description { get; set; }

    public string? Objectives { get; set; }

    [Required(ErrorMessage = "Danh mục chủ đề không được để trống")]
    public int CategoryId { get; set; }

    [Required(ErrorMessage = "Học kỳ không được để trống")]
    public int SemesterId { get; set; }

    [Range(4, 5, ErrorMessage = "Số lượng sinh viên tối đa phải từ 1 đến 5")]
    public int MaxStudents { get; set; } = 4;
    public string? PotentialDuplicate { get; set; } = null;
    public long? FileId { get; set; }

    public Topic GetEntity()
    {
        return new Topic
        {
            EN_Title = EN_Title.Trim(),
            Description = Description?.Trim(),
            Objectives = Objectives?.Trim(),
            CategoryId = CategoryId,
            SemesterId = SemesterId,
            MaxStudents = MaxStudents,
            Abbreviation = Abbreviation?.Trim(),
            VN_title = VN_title?.Trim(),
            Problem = Problem?.Trim(),
            Context = Context?.Trim(),
            Content = Content?.Trim(),
           PotentialDuplicate = PotentialDuplicate?.Trim()
        };
    }


    public BaseResponseModel Validate()
    {
        if (string.IsNullOrWhiteSpace(EN_Title))
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