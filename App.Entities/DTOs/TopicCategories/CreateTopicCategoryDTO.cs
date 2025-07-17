using System.ComponentModel.DataAnnotations;
using App.Commons.Interfaces;
using App.Commons.ResponseModel;
using App.Entities.Entities.App;
using FS.Commons.Interfaces;

namespace App.Entities.DTOs.TopicCategories;

public class CreateTopicCategoryDTO : IEntity<TopicCategory>, IValiationPipeline
{
    [Required(ErrorMessage = "Tên danh mục chủ đề không được để trống")]
    [StringLength(200, ErrorMessage = "Tên danh mục chủ đề không được vượt quá 200 ký tự")]
    public string Name { get; set; } = null!;

    [StringLength(500, ErrorMessage = "Mô tả không được vượt quá 500 ký tự")]
    public string? Description { get; set; }

    public BaseResponseModel Validate()
    {
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

    public TopicCategory GetEntity()
    {
        return new TopicCategory
        {
            Name = Name.Trim(),
            Description = Description?.Trim(),
            IsActive = true
        };
    }
}
