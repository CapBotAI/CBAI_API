using App.Commons.ResponseModel;
using App.Commons.Paging;
using App.Entities.DTOs.ReviewComment;

namespace App.BLL.Interfaces;

public interface IReviewCommentService
{
    /// <summary>
    /// Tạo comment mới cho review
    /// </summary>
    Task<BaseResponseModel<ReviewCommentResponseDTO>> CreateAsync(CreateReviewCommentDTO createDTO);

    /// <summary>
    /// Cập nhật comment
    /// </summary>
    Task<BaseResponseModel<ReviewCommentResponseDTO>> UpdateAsync(int id, UpdateReviewCommentDTO updateDTO);

    /// <summary>
    /// Xóa comment (soft delete)
    /// </summary>
    Task<BaseResponseModel<string>> DeleteAsync(int id);

    /// <summary>
    /// Lấy comment theo ID
    /// </summary>
    Task<BaseResponseModel<ReviewCommentResponseDTO>> GetByIdAsync(int id);

    /// <summary>
    /// Lấy danh sách comment theo Review ID
    /// </summary>
    Task<BaseResponseModel<PagingDataModel<ReviewCommentResponseDTO>>> GetByReviewIdAsync(int reviewId, PagingModel pagingModel);

    /// <summary>
    /// Lấy tất cả comment với phân trang
    /// </summary>
    Task<BaseResponseModel<PagingDataModel<ReviewCommentResponseDTO>>> GetAllAsync(PagingModel pagingModel);

    /// <summary>
    /// Đánh dấu comment đã được giải quyết
    /// </summary>
    Task<BaseResponseModel<string>> MarkAsResolvedAsync(int id);

    /// <summary>
    /// Lấy danh sách comment chưa được giải quyết
    /// </summary>
    Task<BaseResponseModel<PagingDataModel<ReviewCommentResponseDTO>>> GetUnresolvedCommentsAsync(PagingModel pagingModel);
}