using App.Commons.ResponseModel;
using App.Entities.DTOs.ReviewerAssignment;
using App.Entities.Enums;

namespace App.BLL.Interfaces;

public interface IReviewerAssignmentService
{
    /// <summary>
    /// Phân công reviewer cho một submission
    /// </summary>
    Task<BaseResponseModel<ReviewerAssignmentResponseDTO>> AssignReviewerAsync(AssignReviewerDTO dto, int assignedById);
    
    /// <summary>
    /// Phân công nhiều reviewer cùng lúc
    /// </summary>
    Task<BaseResponseModel<List<ReviewerAssignmentResponseDTO>>> BulkAssignReviewersAsync(BulkAssignReviewerDTO dto, int assignedById);
    
    /// <summary>
    /// Lấy danh sách reviewer có thể phân công cho submission
    /// </summary>
    Task<BaseResponseModel<List<AvailableReviewerDTO>>> GetAvailableReviewersAsync(int submissionId);
    
    /// <summary>
    /// Lấy danh sách assignment theo submission
    /// </summary>
    Task<BaseResponseModel<List<ReviewerAssignmentResponseDTO>>> GetAssignmentsBySubmissionAsync(int submissionId);
    
    /// <summary>
    /// Lấy danh sách assignment của một reviewer
    /// </summary>
    Task<BaseResponseModel<List<ReviewerAssignmentResponseDTO>>> GetAssignmentsByReviewerAsync(int reviewerId);
    
    /// <summary>
    /// Cập nhật status của assignment
    /// </summary>
    Task<BaseResponseModel<ReviewerAssignmentResponseDTO>> UpdateAssignmentStatusAsync(int assignmentId, AssignmentStatus newStatus, int updatedById);
    
    /// <summary>
    /// Hủy assignment
    /// </summary>
    Task<BaseResponseModel> RemoveAssignmentAsync(int assignmentId, int removedById);
    
    /// <summary>
    /// Lấy thống kê workload của reviewers
    /// </summary>
    Task<BaseResponseModel<List<AvailableReviewerDTO>>> GetReviewersWorkloadAsync(int? semesterId = null);
    // Thêm methods mới vào interface

    /// <summary>
    /// Tự động phân công reviewer dựa trên skill matching và workload
    /// </summary>
    Task<BaseResponseModel<AutoAssignmentResult>> AutoAssignReviewersAsync(AutoAssignReviewerDTO dto, int assignedById);

    /// <summary>
    /// Lấy danh sách reviewer được recommend cho submission
    /// </summary>
    Task<BaseResponseModel<List<ReviewerMatchingResult>>> GetRecommendedReviewersAsync(int submissionId, AutoAssignReviewerDTO? criteria = null);

    /// <summary>
    /// Phân tích skill matching giữa reviewer và submission
    /// </summary>
    Task<BaseResponseModel<ReviewerMatchingResult>> AnalyzeReviewerMatchAsync(int reviewerId, int submissionId);
}