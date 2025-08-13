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
    [Obsolete("Outdated - Review dồn về Submission")]
    Task<BaseResponseModel> SubmitTopicVersion(SubmitTopicVersionDTO submitTopicVersionDTO, int userId);
    [Obsolete("Outdated - Review dồn về Submission")]
    Task<BaseResponseModel> ReviewTopicVersion(ReviewTopicVersionDTO reviewTopicVersionDTO, int userId, bool isReviewer);
    Task<BaseResponseModel> DeleteTopicVersion(int versionId, int userId, bool isAdmin);
}
