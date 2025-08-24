using AutoMapper;
using App.BLL.Interfaces;
using App.Commons.ResponseModel;
using App.Commons.Paging;
using App.DAL.UnitOfWork;
using App.DAL.Queries;
using App.Entities.DTOs.ReviewComment;
using App.Entities.Entities.App;
using App.Entities.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace App.BLL.Implementations;

public class ReviewCommentService : IReviewCommentService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public ReviewCommentService(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<BaseResponseModel<ReviewCommentResponseDTO>> CreateAsync(CreateReviewCommentDTO createDTO)
    {
        try
        {
            // Kiểm tra Review có tồn tại không
            var reviewRepo = _unitOfWork.GetRepo<Review>();
            var reviewExists = await reviewRepo.AnyAsync(new QueryOptions<Review>
            {
                Predicate = r => r.Id == createDTO.ReviewId && r.IsActive
            });

            if (!reviewExists)
            {
                return new BaseResponseModel<ReviewCommentResponseDTO>
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status404NotFound,
                    Message = "Review không tồn tại hoặc đã bị xóa"
                };
            }

            // Tạo comment mới
            var comment = new ReviewComment
            {
                ReviewId = createDTO.ReviewId,
                SectionName = createDTO.SectionName,
                LineNumber = createDTO.LineNumber,
                CommentText = createDTO.CommentText,
                CommentType = createDTO.CommentType,
                Priority = createDTO.Priority,
                IsResolved = false,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                LastModifiedAt = DateTime.UtcNow
            };

            var commentRepo = _unitOfWork.GetRepo<ReviewComment>();
            await commentRepo.CreateAsync(comment);
            await _unitOfWork.SaveChangesAsync();

            var response = _mapper.Map<ReviewCommentResponseDTO>(comment);
            response.CommentTypeName = comment.CommentType.ToString();
            response.PriorityName = comment.Priority.ToString();

            return new BaseResponseModel<ReviewCommentResponseDTO>
            {
                IsSuccess = true,
                StatusCode = StatusCodes.Status201Created,
                Message = "Tạo comment thành công",
                Data = response
            };
        }
        catch (Exception ex)
        {
            return new BaseResponseModel<ReviewCommentResponseDTO>
            {
                IsSuccess = false,
                StatusCode = StatusCodes.Status500InternalServerError,
                Message = $"Lỗi hệ thống: {ex.Message}"
            };
        }
    }

    public async Task<BaseResponseModel<ReviewCommentResponseDTO>> UpdateAsync(int id, UpdateReviewCommentDTO updateDTO)
    {
        try
        {
            var commentRepo = _unitOfWork.GetRepo<ReviewComment>();
            var comment = await commentRepo.GetSingleAsync(new QueryOptions<ReviewComment>
            {
                Predicate = c => c.Id == id && c.IsActive
            });

            if (comment == null)
            {
                return new BaseResponseModel<ReviewCommentResponseDTO>
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status404NotFound,
                    Message = "Comment không tồn tại hoặc đã bị xóa"
                };
            }

            // Cập nhật thông tin
            comment.SectionName = updateDTO.SectionName;
            comment.LineNumber = updateDTO.LineNumber;
            comment.CommentText = updateDTO.CommentText;
            comment.CommentType = updateDTO.CommentType;
            comment.Priority = updateDTO.Priority;
            comment.IsResolved = updateDTO.IsResolved;
            comment.LastModifiedAt = DateTime.UtcNow;

            await commentRepo.UpdateAsync(comment);
            await _unitOfWork.SaveChangesAsync();

            var response = _mapper.Map<ReviewCommentResponseDTO>(comment);
            response.CommentTypeName = comment.CommentType.ToString();
            response.PriorityName = comment.Priority.ToString();

            return new BaseResponseModel<ReviewCommentResponseDTO>
            {
                IsSuccess = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Cập nhật comment thành công",
                Data = response
            };
        }
        catch (Exception ex)
        {
            return new BaseResponseModel<ReviewCommentResponseDTO>
            {
                IsSuccess = false,
                StatusCode = StatusCodes.Status500InternalServerError,
                Message = $"Lỗi hệ thống: {ex.Message}"
            };
        }
    }

    public async Task<BaseResponseModel<string>> DeleteAsync(int id)
    {
        try
        {
            var commentRepo = _unitOfWork.GetRepo<ReviewComment>();
            var comment = await commentRepo.GetSingleAsync(new QueryOptions<ReviewComment>
            {
                Predicate = c => c.Id == id && c.IsActive
            });

            if (comment == null)
            {
                return new BaseResponseModel<string>
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status404NotFound,
                    Message = "Comment không tồn tại hoặc đã bị xóa"
                };
            }

            // Soft delete
            comment.IsActive = false;
            comment.DeletedAt = DateTime.UtcNow;
            comment.LastModifiedAt = DateTime.UtcNow;

            await commentRepo.UpdateAsync(comment);
            await _unitOfWork.SaveChangesAsync();

            return new BaseResponseModel<string>
            {
                IsSuccess = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Xóa comment thành công",
                Data = "Deleted successfully"
            };
        }
        catch (Exception ex)
        {
            return new BaseResponseModel<string>
            {
                IsSuccess = false,
                StatusCode = StatusCodes.Status500InternalServerError,
                Message = $"Lỗi hệ thống: {ex.Message}"
            };
        }
    }

    public async Task<BaseResponseModel<ReviewCommentResponseDTO>> GetByIdAsync(int id)
    {
        try
        {
            var commentRepo = _unitOfWork.GetRepo<ReviewComment>();
            var comment = await commentRepo.GetSingleAsync(new QueryOptions<ReviewComment>
            {
                Predicate = c => c.Id == id && c.IsActive,
                IncludeProperties = new List<Expression<Func<ReviewComment, object>>>
                {
                    c => c.Review,
                    c => c.Review.Assignment,
                    c => c.Review.Assignment.Reviewer
                }
            });

            if (comment == null)
            {
                return new BaseResponseModel<ReviewCommentResponseDTO>
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status404NotFound,
                    Message = "Comment không tồn tại"
                };
            }

            var response = _mapper.Map<ReviewCommentResponseDTO>(comment);
            response.CommentTypeName = comment.CommentType.ToString();
            response.PriorityName = comment.Priority.ToString();
            response.ReviewerName = comment.Review?.Assignment?.Reviewer?.UserName; // Sử dụng UserName thay vì FullName
            response.ReviewTitle = $"Review #{comment.Review?.Id}";

            return new BaseResponseModel<ReviewCommentResponseDTO>
            {
                IsSuccess = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Lấy thông tin comment thành công",
                Data = response
            };
        }
        catch (Exception ex)
        {
            return new BaseResponseModel<ReviewCommentResponseDTO>
            {
                IsSuccess = false,
                StatusCode = StatusCodes.Status500InternalServerError,
                Message = $"Lỗi hệ thống: {ex.Message}"
            };
        }
    }

    public async Task<BaseResponseModel<PagingDataModel<ReviewCommentResponseDTO>>> GetByReviewIdAsync(int reviewId, PagingModel pagingModel)
    {
        try
        {
            var commentRepo = _unitOfWork.GetRepo<ReviewComment>();
            
            var query = commentRepo.Get(new QueryOptions<ReviewComment>
            {
                Predicate = c => c.ReviewId == reviewId && c.IsActive,
                IncludeProperties = new List<Expression<Func<ReviewComment, object>>>
                {
                    c => c.Review,
                    c => c.Review.Assignment,
                    c => c.Review.Assignment.Reviewer
                },
                Tracked = false,
                OrderBy = q => q.OrderBy(x => x.CreatedAt)
            });

            var totalItems = await query.CountAsync();
            var items = await query
                .Skip((pagingModel.PageNumber - 1) * pagingModel.PageSize)
                .Take(pagingModel.PageSize)
                .ToListAsync();

            var responseList = items.Select(comment =>
            {
                var response = _mapper.Map<ReviewCommentResponseDTO>(comment);
                response.CommentTypeName = comment.CommentType.ToString();
                response.PriorityName = comment.Priority.ToString();
                response.ReviewerName = comment.Review?.Assignment?.Reviewer?.UserName;
                response.ReviewTitle = $"Review #{comment.Review?.Id}";
                return response;
            }).ToList();

            pagingModel.TotalRecord = totalItems;
            var pagingData = new PagingDataModel<ReviewCommentResponseDTO>(responseList, pagingModel);

            return new BaseResponseModel<PagingDataModel<ReviewCommentResponseDTO>>
            {
                IsSuccess = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Lấy danh sách comment thành công",
                Data = pagingData
            };
        }
        catch (Exception ex)
        {
            return new BaseResponseModel<PagingDataModel<ReviewCommentResponseDTO>>
            {
                IsSuccess = false,
                StatusCode = StatusCodes.Status500InternalServerError,
                Message = $"Lỗi hệ thống: {ex.Message}"
            };
        }
    }

    public async Task<BaseResponseModel<PagingDataModel<ReviewCommentResponseDTO>>> GetAllAsync(PagingModel pagingModel)
    {
        try
        {
            var commentRepo = _unitOfWork.GetRepo<ReviewComment>();
            
            var query = commentRepo.Get(new QueryOptions<ReviewComment>
            {
                Predicate = c => c.IsActive,
                IncludeProperties = new List<Expression<Func<ReviewComment, object>>>
                {
                    c => c.Review,
                    c => c.Review.Assignment,
                    c => c.Review.Assignment.Reviewer
                },
                Tracked = false,
                OrderBy = q => q.OrderByDescending(x => x.CreatedAt)
            });

            var totalItems = await query.CountAsync();
            var items = await query
                .Skip((pagingModel.PageNumber - 1) * pagingModel.PageSize)
                .Take(pagingModel.PageSize)
                .ToListAsync();

            var responseList = items.Select(comment =>
            {
                var response = _mapper.Map<ReviewCommentResponseDTO>(comment);
                response.CommentTypeName = comment.CommentType.ToString();
                response.PriorityName = comment.Priority.ToString();
                response.ReviewerName = comment.Review?.Assignment?.Reviewer?.UserName;
                response.ReviewTitle = $"Review #{comment.Review?.Id}";
                return response;
            }).ToList();

            pagingModel.TotalRecord = totalItems;
            var pagingData = new PagingDataModel<ReviewCommentResponseDTO>(responseList, pagingModel);

            return new BaseResponseModel<PagingDataModel<ReviewCommentResponseDTO>>
            {
                IsSuccess = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Lấy danh sách comment thành công",
                Data = pagingData
            };
        }
        catch (Exception ex)
        {
            return new BaseResponseModel<PagingDataModel<ReviewCommentResponseDTO>>
            {
                IsSuccess = false,
                StatusCode = StatusCodes.Status500InternalServerError,
                Message = $"Lỗi hệ thống: {ex.Message}"
            };
        }
    }

    public async Task<BaseResponseModel<string>> MarkAsResolvedAsync(int id)
    {
        try
        {
            var commentRepo = _unitOfWork.GetRepo<ReviewComment>();
            var comment = await commentRepo.GetSingleAsync(new QueryOptions<ReviewComment>
            {
                Predicate = c => c.Id == id && c.IsActive
            });

            if (comment == null)
            {
                return new BaseResponseModel<string>
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status404NotFound,
                    Message = "Comment không tồn tại hoặc đã bị xóa"
                };
            }

            comment.IsResolved = true;
            comment.LastModifiedAt = DateTime.UtcNow;

            await commentRepo.UpdateAsync(comment);
            await _unitOfWork.SaveChangesAsync();

            return new BaseResponseModel<string>
            {
                IsSuccess = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Đánh dấu comment đã giải quyết thành công",
                Data = "Resolved successfully"
            };
        }
        catch (Exception ex)
        {
            return new BaseResponseModel<string>
            {
                IsSuccess = false,
                StatusCode = StatusCodes.Status500InternalServerError,
                Message = $"Lỗi hệ thống: {ex.Message}"
            };
        }
    }

    public async Task<BaseResponseModel<PagingDataModel<ReviewCommentResponseDTO>>> GetUnresolvedCommentsAsync(PagingModel pagingModel)
    {
        try
        {
            var commentRepo = _unitOfWork.GetRepo<ReviewComment>();
            
            var query = commentRepo.Get(new QueryOptions<ReviewComment>
            {
                Predicate = c => c.IsActive && !c.IsResolved,
                IncludeProperties = new List<Expression<Func<ReviewComment, object>>>
                {
                    c => c.Review,
                    c => c.Review.Assignment,
                    c => c.Review.Assignment.Reviewer
                },
                Tracked = false,
                OrderBy = q => q.OrderByDescending(x => x.Priority)
                    .ThenByDescending(x => x.CreatedAt)
            });

            var totalItems = await query.CountAsync();
            var items = await query
                .Skip((pagingModel.PageNumber - 1) * pagingModel.PageSize)
                .Take(pagingModel.PageSize)
                .ToListAsync();

            var responseList = items.Select(comment =>
            {
                var response = _mapper.Map<ReviewCommentResponseDTO>(comment);
                response.CommentTypeName = comment.CommentType.ToString();
                response.PriorityName = comment.Priority.ToString();
                response.ReviewerName = comment.Review?.Assignment?.Reviewer?.UserName;
                response.ReviewTitle = $"Review #{comment.Review?.Id}";
                return response;
            }).ToList();

            pagingModel.TotalRecord = totalItems;
            var pagingData = new PagingDataModel<ReviewCommentResponseDTO>(responseList, pagingModel);

            return new BaseResponseModel<PagingDataModel<ReviewCommentResponseDTO>>
            {
                IsSuccess = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Lấy danh sách comment chưa giải quyết thành công",
                Data = pagingData
            };
        }
        catch (Exception ex)
        {
            return new BaseResponseModel<PagingDataModel<ReviewCommentResponseDTO>>
            {
                IsSuccess = false,
                StatusCode = StatusCodes.Status500InternalServerError,
                Message = $"Lỗi hệ thống: {ex.Message}"
            };
        }
    }
}