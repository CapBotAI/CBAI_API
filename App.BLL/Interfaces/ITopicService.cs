using App.Commons.Paging;
using App.Commons.ResponseModel;
using App.Entities.DTOs.Topics;

namespace App.BLL.Interfaces;

public interface ITopicService
{
    Task<BaseResponseModel<CreateTopicResDTO>> CreateTopic(CreateTopicDTO createTopicDTO, int userId);
    Task<BaseResponseModel<PagingDataModel<TopicOverviewResDTO, GetTopicsQueryDTO>>> GetTopicsWithPaging(GetTopicsQueryDTO query);
    Task<BaseResponseModel<TopicDetailDTO>> GetTopicDetail(int topicId);
    Task<BaseResponseModel<UpdateTopicResDTO>> UpdateTopic(UpdateTopicDTO updateTopicDTO, int userId, bool isAdmin);
    Task<BaseResponseModel> DeleteTopic(int topicId);
    Task<BaseResponseModel> ApproveTopic(int topicId, int userId);
    Task<BaseResponseModel<List<TopicOverviewResDTO>>> GetMyTopics(int userId);
}
