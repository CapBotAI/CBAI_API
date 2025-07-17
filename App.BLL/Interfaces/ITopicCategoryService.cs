using System;
using App.Commons.ResponseModel;
using App.Entities.DTOs.TopicCategories;

namespace App.BLL.Interfaces;

public interface ITopicCategoryService
{
    Task<BaseResponseModel<CreateTopicCategoryResDTO>> CreateTopicCategory(CreateTopicCategoryDTO createTopicCategoryDTO, int userId);
    Task<BaseResponseModel<List<TopicCategoryOverviewResDTO>>> GetAllTopicCategory();
    Task<BaseResponseModel<UpdateTopicCategoryResDTO>> UpdateTopicCategory(UpdateTopicCategoryDTO updateTopicCategoryDTO, int userId);
    Task<BaseResponseModel<TopicCategoryDetailDTO>> GetTopicCategoryDetail(int topicCategoryId);
    Task<BaseResponseModel> DeleteTopicCategory(int topicCategoryId);
}
