using System.ComponentModel.DataAnnotations;
using App.Commons.ResponseModel;

namespace App.Entities.DTOs.TopicCategories;

public class UpdateTopicCategoryDTO
{
    [Required(ErrorMessage = "Id danh mục chủ đề không được để trống")]
    public int Id { get; set; }

    [Required(ErrorMessage = "Tên danh mục chủ đề không được để trống")]
    [StringLength(200, ErrorMessage = "Tên danh mục chủ đề không được vượt quá 200 ký tự")]
    public string Name { get; set; } = null!;

    [StringLength(500, ErrorMessage = "Mô tả không được vượt quá 500 ký tự")]
    public string? Description { get; set; }

    public BaseResponseModel Validate()
    {
        if (Id <= 0)
        {
            return new BaseResponseModel
            {
                IsSuccess = false,
                Message = "Id danh mục chủ đề không hợp lệ"
            };
        }

        if (string.IsNullOrWhiteSpace(Name))
        {
            return new BaseResponseModel
            {
                IsSuccess = false,
                Message = "Tên danh mục chủ đề không được để trống"
            };
        }

        return new BaseResponseModel { IsSuccess = true };
    }
}
