using AutoMapper;
using App.BLL.Interfaces;
using App.Commons.ResponseModel;
using App.Commons.Paging;
using App.DAL.UnitOfWork;
using App.DAL.Queries;
using App.Entities.DTOs.Review;
using App.Entities.Entities.App;
using App.Entities.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace App.BLL.Implementations;

public class ReviewService : IReviewService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ISemesterService _semesterService;

    public ReviewService(IUnitOfWork unitOfWork, IMapper mapper, ISemesterService semesterService)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _semesterService = semesterService;
    }

    public async Task<BaseResponseModel<ReviewResponseDTO>> CreateAsync(CreateReviewDTO createDTO , int currentUserId)
    {
        try
        {
            await _unitOfWork.BeginTransactionAsync();

            var reviewRepo = _unitOfWork.GetRepo<Review>();
            var scoreRepo = _unitOfWork.GetRepo<ReviewCriteriaScore>();
            var criteriaRepo = _unitOfWork.GetRepo<EvaluationCriteria>();
            var assignmentRepo = _unitOfWork.GetRepo<ReviewerAssignment>();

            // Kiểm tra assignment tồn tại
            var assignment = await assignmentRepo.GetSingleAsync(new QueryOptions<ReviewerAssignment>
            {
                Predicate = x => x.Id == createDTO.AssignmentId && x.ReviewerId == currentUserId,
                Tracked = false
            });

            if (assignment == null)
            {
                await _unitOfWork.RollBackAsync();
                return new BaseResponseModel<ReviewResponseDTO>
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status404NotFound,
                    Message = "Không tìm thấy assignment hoặc bạn không có quyền review"
                };
            }

            // THÊM: Kiểm tra deadline
            if (assignment.Deadline.HasValue && assignment.Deadline < DateTime.UtcNow)
            {
                await _unitOfWork.RollBackAsync();
                return new BaseResponseModel<ReviewResponseDTO>
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status400BadRequest,
                    Message = "Assignment đã quá hạn deadline, không thể tạo review"
                };
            }

            // Kiểm tra đã có review cho assignment này chưa
            var existingReview = await reviewRepo.GetSingleAsync(new QueryOptions<Review>
            {
                Predicate = x => x.AssignmentId == createDTO.AssignmentId && x.IsActive,
                Tracked = false
            });

            if (existingReview != null)
            {
                await _unitOfWork.RollBackAsync();
                return new BaseResponseModel<ReviewResponseDTO>
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status409Conflict,
                    Message = "Assignment này đã có review"
                };
            }
            var criteriaIds = createDTO.CriteriaScores.Select(x => x.CriteriaId).ToList();


            var currentSemesterResult = await _semesterService.GetCurrentSemesterAsync();
            int? currentSemesterId = currentSemesterResult.IsSuccess ? currentSemesterResult.Data?.Id : null;

            var criteria = await criteriaRepo.GetAllAsync(new QueryOptions<EvaluationCriteria>
            {
                Predicate = x => criteriaIds.Contains(x.Id) && 
                                 x.IsActive &&
                                 (x.SemesterId == currentSemesterId || x.SemesterId == null), // Filter theo semester
                Tracked = false
            });
            

            if (criteria.Count() != criteriaIds.Count)
            {
                await _unitOfWork.RollBackAsync();
                return new BaseResponseModel<ReviewResponseDTO>
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status400BadRequest,
                    Message = "Một số tiêu chí đánh giá không tồn tại"
                };
            }

            // Validate scores against max scores
            foreach (var scoreDTO in createDTO.CriteriaScores)
            {
                var criteriaItem = criteria.First(x => x.Id == scoreDTO.CriteriaId);
                if (scoreDTO.Score > criteriaItem.MaxScore)
                {
                    await _unitOfWork.RollBackAsync();
                    return new BaseResponseModel<ReviewResponseDTO>
                    {
                        IsSuccess = false,
                        StatusCode = StatusCodes.Status400BadRequest,
                        Message = $"Điểm cho tiêu chí '{criteriaItem.Name}' không được vượt quá {criteriaItem.MaxScore}"
                    };
                }
            }

            // Create review
            var review = new Review
            {
                AssignmentId = createDTO.AssignmentId,
                OverallComment = createDTO.OverallComment,
                Recommendation = createDTO.Recommendation,
                TimeSpentMinutes = createDTO.TimeSpentMinutes,
                Status = ReviewStatus.Draft,
                CreatedAt = DateTime.UtcNow,
                LastModifiedAt = DateTime.UtcNow,
                IsActive = true
            };

            // Calculate overall score
            decimal totalWeightedScore = 0;
            decimal totalWeight = 0;

            foreach (var scoreDTO in createDTO.CriteriaScores)
            {
                var criteriaItem = criteria.First(x => x.Id == scoreDTO.CriteriaId);
                var normalizedScore = (scoreDTO.Score / criteriaItem.MaxScore) * 10; // Normalize to 10-point scale
                totalWeightedScore += normalizedScore * criteriaItem.Weight;
                totalWeight += criteriaItem.Weight;
            }

            review.OverallScore = totalWeight > 0 ? totalWeightedScore / totalWeight : 0;

            await reviewRepo.CreateAsync(review);
            var saveResult = await _unitOfWork.SaveAsync();
            
            if (!saveResult.IsSuccess)
            {
                await _unitOfWork.RollBackAsync();
                return new BaseResponseModel<ReviewResponseDTO>
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status500InternalServerError,
                    Message = saveResult.Message
                };
            }

            // Create criteria scores
            var scores = new List<ReviewCriteriaScore>();
            foreach (var scoreDTO in createDTO.CriteriaScores)
            {
                var score = new ReviewCriteriaScore
                {
                    ReviewId = review.Id,
                    CriteriaId = scoreDTO.CriteriaId,
                    Score = scoreDTO.Score,
                    Comment = scoreDTO.Comment,
                    CreatedAt = DateTime.UtcNow,
                    LastModifiedAt = DateTime.UtcNow,
                    IsActive = true
                };
                scores.Add(score);
            }

            await scoreRepo.CreateAllAsync(scores);
            var finalResult = await _unitOfWork.SaveAsync();

            if (!finalResult.IsSuccess)
            {
                await _unitOfWork.RollBackAsync();
                return new BaseResponseModel<ReviewResponseDTO>
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status500InternalServerError,
                    Message = finalResult.Message
                };
            }

            await _unitOfWork.CommitTransactionAsync();

            // Get created review with related data
            var createdReview = await GetByIdAsync(review.Id);
            return new BaseResponseModel<ReviewResponseDTO>
            {
                IsSuccess = true,
                StatusCode = StatusCodes.Status201Created,
                Message = "Tạo đánh giá thành công",
                Data = createdReview.Data
            };
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollBackAsync();
            return new BaseResponseModel<ReviewResponseDTO>
            {
                IsSuccess = false,
                StatusCode = StatusCodes.Status500InternalServerError,
                Message = $"Lỗi hệ thống: {ex.Message}"
            };
        }
    }

    public async Task<BaseResponseModel<ReviewResponseDTO>> UpdateAsync(UpdateReviewDTO updateDTO)
    {
        try
        {
            await _unitOfWork.BeginTransactionAsync();

            var reviewRepo = _unitOfWork.GetRepo<Review>();
            var scoreRepo = _unitOfWork.GetRepo<ReviewCriteriaScore>();
            var criteriaRepo = _unitOfWork.GetRepo<EvaluationCriteria>();

            var review = await reviewRepo.GetSingleAsync(new QueryOptions<Review>
            {
                Predicate = x => x.Id == updateDTO.Id && x.IsActive,
                IncludeProperties = new List<System.Linq.Expressions.Expression<Func<Review, object>>>
                {
                    x => x.ReviewCriteriaScores
                }
            });

            if (review == null)
            {
                await _unitOfWork.RollBackAsync();
                return new BaseResponseModel<ReviewResponseDTO>
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status404NotFound,
                    Message = "Không tìm thấy đánh giá"
                };
            }

            if (review.Status == ReviewStatus.Submitted)
            {
                await _unitOfWork.RollBackAsync();
                return new BaseResponseModel<ReviewResponseDTO>
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status400BadRequest,
                    Message = "Không thể cập nhật đánh giá đã submit"
                };
            }

            // Validate criteria scores
            var criteriaIds = updateDTO.CriteriaScores.Select(x => x.CriteriaId).ToList();
            var criteria = await criteriaRepo.GetAllAsync(new QueryOptions<EvaluationCriteria>
            {
                Predicate = x => criteriaIds.Contains(x.Id) && x.IsActive,
                Tracked = false
            });

            if (criteria.Count() != criteriaIds.Count)
            {
                await _unitOfWork.RollBackAsync();
                return new BaseResponseModel<ReviewResponseDTO>
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status400BadRequest,
                    Message = "Một số tiêu chí đánh giá không tồn tại"
                };
            }

            // Validate scores against max scores
            foreach (var scoreDTO in updateDTO.CriteriaScores)
            {
                var criteriaItem = criteria.First(x => x.Id == scoreDTO.CriteriaId);
                if (scoreDTO.Score > criteriaItem.MaxScore)
                {
                    await _unitOfWork.RollBackAsync();
                    return new BaseResponseModel<ReviewResponseDTO>
                    {
                        IsSuccess = false,
                        StatusCode = StatusCodes.Status400BadRequest,
                        Message = $"Điểm cho tiêu chí '{criteriaItem.Name}' không được vượt quá {criteriaItem.MaxScore}"
                    };
                }
            }

            // Update review
            review.OverallComment = updateDTO.OverallComment;
            review.Recommendation = updateDTO.Recommendation;
            review.TimeSpentMinutes = updateDTO.TimeSpentMinutes;
            review.LastModifiedAt = DateTime.UtcNow;

            // Calculate new overall score
            decimal totalWeightedScore = 0;
            decimal totalWeight = 0;

            foreach (var scoreDTO in updateDTO.CriteriaScores)
            {
                var criteriaItem = criteria.First(x => x.Id == scoreDTO.CriteriaId);
                var normalizedScore = (scoreDTO.Score / criteriaItem.MaxScore) * 10;
                totalWeightedScore += normalizedScore * criteriaItem.Weight;
                totalWeight += criteriaItem.Weight;
            }

            review.OverallScore = totalWeight > 0 ? totalWeightedScore / totalWeight : 0;

            // Thay vì deactivate và tạo mới, hãy cập nhật trực tiếp
            var existingScores = review.ReviewCriteriaScores.Where(x => x.IsActive).ToList();

            // Cập nhật hoặc tạo mới scores
            foreach (var scoreDTO in updateDTO.CriteriaScores)
            {
                var existingScore = existingScores.FirstOrDefault(x => x.CriteriaId == scoreDTO.CriteriaId);
                
                if (existingScore != null)
                {
                    // Cập nhật bản ghi hiện có
                    existingScore.Score = scoreDTO.Score;
                    existingScore.Comment = scoreDTO.Comment;
                    existingScore.LastModifiedAt = DateTime.UtcNow;
                }
                else
                {
                    // Tạo mới nếu chưa có
                    var newScore = new ReviewCriteriaScore
                    {
                        ReviewId = review.Id,
                        CriteriaId = scoreDTO.CriteriaId,
                        Score = scoreDTO.Score,
                        Comment = scoreDTO.Comment,
                        CreatedAt = DateTime.UtcNow,
                        LastModifiedAt = DateTime.UtcNow,
                        IsActive = true
                    };
                    await scoreRepo.CreateAsync(newScore);
                }
            }

            // Xóa các scores không còn được sử dụng
            var scoresToRemove = existingScores
                .Where(x => !updateDTO.CriteriaScores.Any(dto => dto.CriteriaId == x.CriteriaId))
                .ToList();

            foreach (var score in scoresToRemove)
            {
                score.IsActive = false;
                score.DeletedAt = DateTime.UtcNow;
                score.LastModifiedAt = DateTime.UtcNow;
            }

            await reviewRepo.UpdateAsync(review);
            // Không cần CreateAllAsync vì đã cập nhật trực tiếp

            var result = await _unitOfWork.SaveAsync();
            if (!result.IsSuccess)
            {
                await _unitOfWork.RollBackAsync();
                return new BaseResponseModel<ReviewResponseDTO>
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status500InternalServerError,
                    Message = result.Message
                };
            }

            await _unitOfWork.CommitTransactionAsync();

            // Get updated review
            var updatedReview = await GetByIdAsync(review.Id);
            return new BaseResponseModel<ReviewResponseDTO>
            {
                IsSuccess = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Cập nhật đánh giá thành công",
                Data = updatedReview.Data
            };
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollBackAsync();
            return new BaseResponseModel<ReviewResponseDTO>
            {
                IsSuccess = false,
                StatusCode = StatusCodes.Status500InternalServerError,
                Message = $"Lỗi hệ thống: {ex.Message}"
            };
        }
    }

    public async Task<BaseResponseModel> DeleteAsync(int id)
    {
        try
        {
            var reviewRepo = _unitOfWork.GetRepo<Review>();
            
            var review = await reviewRepo.GetSingleAsync(new QueryOptions<Review>
            {
                Predicate = x => x.Id == id && x.IsActive
            });

            if (review == null)
            {
                return new BaseResponseModel
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status404NotFound,
                    Message = "Không tìm thấy đánh giá"
                };
            }

            if (review.Status == ReviewStatus.Submitted)
            {
                return new BaseResponseModel
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status400BadRequest,
                    Message = "Không thể xóa đánh giá đã submit"
                };
            }

            review.IsActive = false;
            review.DeletedAt = DateTime.UtcNow;
            review.LastModifiedAt = DateTime.UtcNow;

            await reviewRepo.UpdateAsync(review);
            var result = await _unitOfWork.SaveAsync();

            if (!result.IsSuccess)
            {
                return new BaseResponseModel
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status500InternalServerError,
                    Message = result.Message
                };
            }

            return new BaseResponseModel
            {
                IsSuccess = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Xóa đánh giá thành công"
            };
        }
        catch (Exception ex)
        {
            return new BaseResponseModel
            {
                IsSuccess = false,
                StatusCode = StatusCodes.Status500InternalServerError,
                Message = $"Lỗi hệ thống: {ex.Message}"
            };
        }
    }
    public async Task<BaseResponseModel<ReviewResponseDTO>> WithdrawReviewAsync(int reviewId)
{
    try
    {
        var reviewRepo = _unitOfWork.GetRepo<Review>();
        
        var review = await reviewRepo.GetSingleAsync(new QueryOptions<Review>
        {
            Predicate = x => x.Id == reviewId && x.IsActive
        });

        if (review == null)
        {
            return new BaseResponseModel<ReviewResponseDTO>
            {
                IsSuccess = false,
                StatusCode = StatusCodes.Status404NotFound,
                Message = "Không tìm thấy đánh giá"
            };
        }

        if (review.Status != ReviewStatus.Submitted)
        {
            return new BaseResponseModel<ReviewResponseDTO>
            {
                IsSuccess = false,
                StatusCode = StatusCodes.Status400BadRequest,
                Message = "Chỉ có thể rút lại đánh giá đã submit"
            };
        }

        // Chuyển về trạng thái Draft
        review.Status = ReviewStatus.Draft;
        review.SubmittedAt = null;
        review.LastModifiedAt = DateTime.UtcNow;

        await reviewRepo.UpdateAsync(review);
        var result = await _unitOfWork.SaveAsync();

        if (!result.IsSuccess)
        {
            return new BaseResponseModel<ReviewResponseDTO>
            {
                IsSuccess = false,
                StatusCode = StatusCodes.Status500InternalServerError,
                Message = result.Message
            };
        }

        // Get updated review
        var updatedReview = await GetByIdAsync(review.Id);
        return new BaseResponseModel<ReviewResponseDTO>
        {
            IsSuccess = true,
            StatusCode = StatusCodes.Status200OK,
            Message = "Rút lại đánh giá thành công",
            Data = updatedReview.Data
        };
    }
    catch (Exception ex)
    {
        return new BaseResponseModel<ReviewResponseDTO>
        {
            IsSuccess = false,
            StatusCode = StatusCodes.Status500InternalServerError,
            Message = $"Lỗi hệ thống: {ex.Message}"
        };
    }
}

    public async Task<BaseResponseModel<ReviewResponseDTO>> GetByIdAsync(int id)
    {
        try
        {
            var reviewRepo = _unitOfWork.GetRepo<Review>();
            
            var review = await reviewRepo.GetSingleAsync(new QueryOptions<Review>
            {
                Predicate = x => x.Id == id && x.IsActive,
                IncludeProperties = new List<System.Linq.Expressions.Expression<Func<Review, object>>>
                {
                    x => x.ReviewCriteriaScores
                },
                Tracked = false
            });

            if (review == null)
            {
                return new BaseResponseModel<ReviewResponseDTO>
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status404NotFound,
                    Message = "Không tìm thấy đánh giá"
                };
            }

            var responseDTO = _mapper.Map<ReviewResponseDTO>(review);
            return new BaseResponseModel<ReviewResponseDTO>
            {
                IsSuccess = true,
                StatusCode = StatusCodes.Status200OK,
                Data = responseDTO
            };
        }
        catch (Exception ex)
        {
            return new BaseResponseModel<ReviewResponseDTO>
            {
                IsSuccess = false,
                StatusCode = StatusCodes.Status500InternalServerError,
                Message = $"Lỗi hệ thống: {ex.Message}"
            };
        }
    }

    public async Task<BaseResponseModel<PagingDataModel<ReviewResponseDTO>>> GetAllAsync(PagingModel pagingModel)
    {
        try
        {
            var reviewRepo = _unitOfWork.GetRepo<Review>();
            
            var query = reviewRepo.Get(new QueryOptions<Review>
            {
                Predicate = x => x.IsActive,
                IncludeProperties = new List<System.Linq.Expressions.Expression<Func<Review, object>>>
                {
                    x => x.ReviewCriteriaScores
                },
                Tracked = false,
                OrderBy = q => q.OrderByDescending(x => x.CreatedAt)
            });

            var totalItems = await query.CountAsync();
            var items = await query
                .Skip((pagingModel.PageNumber - 1) * pagingModel.PageSize)
                .Take(pagingModel.PageSize)
                .ToListAsync();

            var responseItems = _mapper.Map<List<ReviewResponseDTO>>(items);
            
            pagingModel.TotalRecord = totalItems;
            var pagingData = new PagingDataModel<ReviewResponseDTO>(responseItems, pagingModel);

            return new BaseResponseModel<PagingDataModel<ReviewResponseDTO>>
            {
                IsSuccess = true,
                StatusCode = StatusCodes.Status200OK,
                Data = pagingData
            };
        }
        catch (Exception ex)
        {
            return new BaseResponseModel<PagingDataModel<ReviewResponseDTO>>
            {
                IsSuccess = false,
                StatusCode = StatusCodes.Status500InternalServerError,
                Message = $"Lỗi hệ thống: {ex.Message}"
            };
        }
    }

    public async Task<BaseResponseModel<ReviewResponseDTO>> SubmitReviewAsync(int reviewId)
    {
        try
        {
            var reviewRepo = _unitOfWork.GetRepo<Review>();
            
            var review = await reviewRepo.GetSingleAsync(new QueryOptions<Review>
            {
                Predicate = x => x.Id == reviewId && x.IsActive
            });

            if (review == null)
            {
                return new BaseResponseModel<ReviewResponseDTO>
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status404NotFound,
                    Message = "Không tìm thấy đánh giá"
                };
            }

            if (review.Status == ReviewStatus.Submitted)
            {
                return new BaseResponseModel<ReviewResponseDTO>
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status400BadRequest,
                    Message = "Đánh giá đã được submit"
                };
            }

            review.Status = ReviewStatus.Submitted;
            review.SubmittedAt = DateTime.UtcNow;
            review.LastModifiedAt = DateTime.UtcNow;

            await reviewRepo.UpdateAsync(review);
            var result = await _unitOfWork.SaveAsync();

            if (!result.IsSuccess)
            {
                return new BaseResponseModel<ReviewResponseDTO>
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status500InternalServerError,
                    Message = result.Message
                };
            }

            var updatedReview = await GetByIdAsync(reviewId);
            return new BaseResponseModel<ReviewResponseDTO>
            {
                IsSuccess = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Submit đánh giá thành công",
                Data = updatedReview.Data
            };
        }
        catch (Exception ex)
        {
            return new BaseResponseModel<ReviewResponseDTO>
            {
                IsSuccess = false,
                StatusCode = StatusCodes.Status500InternalServerError,
                Message = $"Lỗi hệ thống: {ex.Message}"
            };
        }
    }

    public async Task<BaseResponseModel<List<ReviewResponseDTO>>> GetReviewsByAssignmentAsync(int assignmentId)
    {
        try
        {
            var reviewRepo = _unitOfWork.GetRepo<Review>();
            
            var reviews = await reviewRepo.GetAllAsync(new QueryOptions<Review>
            {
                Predicate = x => x.AssignmentId == assignmentId && x.IsActive,
                IncludeProperties = new List<System.Linq.Expressions.Expression<Func<Review, object>>>
                {
                    x => x.ReviewCriteriaScores
                },
                Tracked = false,
                OrderBy = q => q.OrderByDescending(x => x.CreatedAt)
            });

            var responseItems = _mapper.Map<List<ReviewResponseDTO>>(reviews);

            return new BaseResponseModel<List<ReviewResponseDTO>>
            {
                IsSuccess = true,
                StatusCode = StatusCodes.Status200OK,
                Data = responseItems
            };
        }
        catch (Exception ex)
        {
            return new BaseResponseModel<List<ReviewResponseDTO>>
            {
                IsSuccess = false,
                StatusCode = StatusCodes.Status500InternalServerError,
                Message = $"Lỗi hệ thống: {ex.Message}"
            };
        }
    }
}