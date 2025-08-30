using System.ComponentModel.DataAnnotations;
using App.Commons.ResponseModel;

namespace App.Entities.DTOs.Topics;

public class UpdateTopicDTO
{
    [Required(ErrorMessage = "Id chủ đề không được để trống")]
    public int Id { get; set; }

    [Required(ErrorMessage = "Tiêu đề chủ đề không được để trống")]
    [StringLength(500, ErrorMessage = "Tiêu đề chủ đề không được vượt quá 500 ký tự")]
    public string Title { get; set; } = null!;

    public string? Description { get; set; }
    public string? Objectives { get; set; }

    [Required(ErrorMessage = "Danh mục chủ đề không được để trống")]
    public int CategoryId { get; set; }

    [Range(1, 5, ErrorMessage = "Số lượng sinh viên tối đa phải từ 1 đến 5")]
    public int MaxStudents { get; set; }

    public long? FileId { get; set; }

    public BaseResponseModel Validate()
    {
        if (Id <= 0)
        {
            return new BaseResponseModel
            {
                IsSuccess = false,
                Message = "Id chủ đề không hợp lệ"
            };
        }

        if (string.IsNullOrWhiteSpace(Title))
        {
            return new BaseResponseModel
            {
                IsSuccess = false,
                Message = "Tiêu đề chủ đề không được để trống"
            };
        }

        return new BaseResponseModel { IsSuccess = true };
    }
}
