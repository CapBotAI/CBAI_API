using System;
using App.Commons.Paging;
using App.Commons.ResponseModel;
using App.Entities.DTOs.Submissions;

namespace App.BLL.Interfaces;

public interface ISubmissionService
{
    Task<BaseResponseModel<SubmissionDetailDTO>> CreateSubmission(CreateSubmissionDTO dto, int userId);
    Task<BaseResponseModel<SubmissionDetailDTO>> UpdateSubmission(UpdateSubmissionDTO dto, int userId);
    Task<BaseResponseModel> SubmitSubmission(SubmitSubmissionDTO dto, int userId);
    Task<BaseResponseModel> ResubmitSubmission(ResubmitSubmissionDTO dto, int userId);
    Task<BaseResponseModel<SubmissionDetailDTO>> GetSubmissionDetail(int id);
    Task<BaseResponseModel<PagingDataModel<SubmissionOverviewResDTO, GetSubmissionsQueryDTO>>> GetSubmissions(GetSubmissionsQueryDTO query);
    Task<BaseResponseModel> DeleteSubmission(int id, int userId, bool isAdmin);
}
