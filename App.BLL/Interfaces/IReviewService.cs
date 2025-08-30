using App.Commons.ResponseModel;
using App.Entities.DTOs.Review;
using App.Commons.Paging;

namespace App.BLL.Interfaces;

public interface IReviewService
{
    Task<BaseResponseModel<ReviewResponseDTO>> CreateAsync(CreateReviewDTO createDTO);
    Task<BaseResponseModel<ReviewResponseDTO>> UpdateAsync(UpdateReviewDTO updateDTO);
    Task<BaseResponseModel> DeleteAsync(int id);
    Task<BaseResponseModel<ReviewResponseDTO>> GetByIdAsync(int id);
    Task<BaseResponseModel<PagingDataModel<ReviewResponseDTO>>> GetAllAsync(PagingModel pagingModel);
    Task<BaseResponseModel<ReviewResponseDTO>> SubmitReviewAsync(int reviewId);
    Task<BaseResponseModel<List<ReviewResponseDTO>>> GetReviewsByAssignmentAsync(int assignmentId);
    Task<BaseResponseModel<ReviewResponseDTO>> WithdrawReviewAsync(int reviewId);
}