using App.Commons.Paging;
using App.Commons.ResponseModel;
using App.Entities.DTOs.Topics;
using App.Entities.DTOs.TopicVersions;

namespace App.BLL.Interfaces;

public interface ITopicVersionService
{
    Task<BaseResponseModel<CreaterTopicVersionResDTO>> CreateTopicVersion(CreateTopicVersionDTO createTopicVersionDTO, int userId);
    Task<BaseResponseModel<TopicVersionDetailDTO>> UpdateTopicVersion(UpdateTopicVersionDTO updateTopicVersionDTO, int userId);
    Task<BaseResponseModel<PagingDataModel<TopicVersionOverviewDTO, GetTopicVersionQueryDTO>>> GetTopicVersionHistory(GetTopicVersionQueryDTO query, int topicId);
    Task<BaseResponseModel<TopicVersionDetailDTO>> GetTopicVersionDetail(int versionId);
    Task<BaseResponseModel> SubmitTopicVersion(SubmitTopicVersionDTO submitTopicVersionDTO, int userId);
    Task<BaseResponseModel> ReviewTopicVersion(ReviewTopicVersionDTO reviewTopicVersionDTO, int userId);
    Task<BaseResponseModel> DeleteTopicVersion(int versionId, int userId);
}
