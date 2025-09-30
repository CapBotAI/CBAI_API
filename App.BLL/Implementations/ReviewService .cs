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
using System.Linq.Expressions;

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

    // Ensure submission status is updated after a review is submitted
    private async Task UpdateSubmissionStatusAfterSubmit(int assignmentId)
    {
        try
        {
            var assignmentRepo = _unitOfWork.GetRepo<ReviewerAssignment>();
            var submissionRepo = _unitOfWork.GetRepo<Submission>();

            //var assignment = await assignmentRepo.GetSingleAsync(new QueryOptions<ReviewerAssignment>
            //{
            //    Predicate = a => a.Id == assignmentId,
            //    IncludeProperties = new List<Expression<Func<ReviewerAssignment, object>>>
            //    {
            //        a => a.Submission,
            //        a => a.Reviews
            //    },
            //    Tracked = false
            //});
            var assignmentReviewer = await assignmentRepo.GetAllAsync(new QueryOptions<ReviewerAssignment>
            {              
                IncludeProperties = new List<Expression<Func<ReviewerAssignment, object>>>
                {
                    a => a.Submission,
                    a => a.Reviews
                },
                Tracked = false
            });
            var assignment = assignmentReviewer.FirstOrDefault(a => a.Id == assignmentId) ;
           
            if (assignment.Submission == null) return; // should not happen
            var submission = assignment.Submission;

            // Collect submitted reviews across all assignments for this submission
            var submittedReviews = assignmentReviewer.Where(a => a.SubmissionId == assignment.SubmissionId)
                .SelectMany(ra => ra.Reviews ?? new List<Review>() )
                .Where(r => r.IsActive && r.Status == ReviewStatus.Submitted )
                .ToList();

            // If submission is escalated to moderator, a single submitted review (the moderator's extra reviewer)
            // should decide the final status. Otherwise the standard two-reviewer rules apply.
            if (submission.Status == SubmissionStatus.EscalatedToModerator && submittedReviews.Count > 2)
            {
                // Use the latest submitted review from the assignment that triggered this call (assignment variable)
                var latestForThisAssignment = (assignment.Reviews ?? new List<Review>())
                    .Where(r => r.IsActive && r.Status == ReviewStatus.Submitted)
                    .OrderByDescending(r => r.SubmittedAt)
                    .FirstOrDefault();

                if (latestForThisAssignment != null)
                {
                    if (latestForThisAssignment.Recommendation == ReviewRecommendations.MinorRevision || latestForThisAssignment.Recommendation == ReviewRecommendations.MajorRevision)
                    {
                        submission.Status = SubmissionStatus.RevisionRequired;
                    }
                    else if (latestForThisAssignment.Recommendation == ReviewRecommendations.Approve)
                    {
                        submission.Status = SubmissionStatus.Approved;
                    }
                    else if (latestForThisAssignment.Recommendation == ReviewRecommendations.Reject)
                    {
                        submission.Status = SubmissionStatus.Rejected;
                    }
                }
            }
            else
            {
                if (submittedReviews.Count < 2) return; // wait for at least two submitted reviews

                var approveCount = submittedReviews.Count(r => r.Recommendation == ReviewRecommendations.Approve);
                var rejectCount = submittedReviews.Count(r => r.Recommendation == ReviewRecommendations.Reject);
                var revisionCount = submittedReviews.Count(r => r.Recommendation == ReviewRecommendations.MinorRevision || r.Recommendation == ReviewRecommendations.MajorRevision);

                if (revisionCount > 0)
                {
                    submission.Status = SubmissionStatus.RevisionRequired;
                }
                else if (approveCount >= 2)
                {
                    submission.Status = SubmissionStatus.Approved;
                }
                else if (rejectCount >= 2)
                {
                    submission.Status = SubmissionStatus.Rejected;
                }
                else if (approveCount > 0 && rejectCount > 0)
                {
                    submission.Status = SubmissionStatus.EscalatedToModerator;
                }
            }

            // Persist submission status change
            await submissionRepo.UpdateAsync(submission);

            // Also update related assignments: mark assignments that have a submitted review as Completed or Overdue
            // Use the latest submitted review for each assignment to determine completed time
            var assignmentIdsWithSubmitted = submittedReviews.Select(r => r.AssignmentId).Distinct().ToList();
            var assignmentsToUpdate = submission.ReviewerAssignments
                .Where(ra => assignmentIdsWithSubmitted.Contains(ra.Id))
                .ToList();

            var updatedAssignmentIds = new List<int>();
            foreach (var ra in assignmentsToUpdate)
            {
                try
                {
                    // find latest submitted review for this assignment
                    var latestSubmitted = (ra.Reviews ?? new List<Review>())
                        .Where(r => r.IsActive && r.Status == ReviewStatus.Submitted)
                        .OrderByDescending(r => r.SubmittedAt)
                        .FirstOrDefault();

                    if (latestSubmitted == null) continue;

                    // Determine on-time vs overdue using UTC comparisons; if no deadline, treat as on-time
                    var completedAt = latestSubmitted.SubmittedAt ?? DateTime.UtcNow;
                    var isOnTime = true;
                    if (ra.Deadline.HasValue)
                    {
                        isOnTime = completedAt.ToUniversalTime() <= ra.Deadline.Value.ToUniversalTime();
                    }

                    ra.CompletedAt = completedAt;
                    ra.Status = isOnTime ? AssignmentStatus.Completed : AssignmentStatus.Overdue;

                    // Update the assignment row (tracked=false so UpdateAsync will attach)
                    await assignmentRepo.UpdateAsync(ra);
                    updatedAssignmentIds.Add(ra.Id);
                }
                catch { /* best-effort, do not block */ }
            }

            // Persist all assignment changes first so performance recalculation reads persisted state
            var saveResult = await _unitOfWork.SaveAsync();
            if (saveResult.IsSuccess)
            {
                // Now recalculate reviewer performance for updated assignments (best-effort)
                foreach (var aid in updatedAssignmentIds.Distinct())
                {
                    try
                    {
                        await UpdateReviewerPerformanceForAssignmentAsync(aid);
                    }
                    catch { }
                }
            }
        }
        catch
        {
            // Best-effort: do not throw
        }
    }
    //private async Task UpdateSubmissionStatusAfterSubmit(int assignmentId)
    //{
    //    try
    //    {
    //        var assignmentRepo = _unitOfWork.GetRepo<ReviewerAssignment>();
    //        var submissionRepo = _unitOfWork.GetRepo<Submission>();

    //        var assignment = await assignmentRepo.GetSingleAsync(new QueryOptions<ReviewerAssignment>
    //        {
    //            Predicate = a => a.Id == assignmentId,
    //            IncludeProperties = new List<System.Linq.Expressions.Expression<Func<ReviewerAssignment, object>>>
    //            {
    //                a => a.Submission,
    //                a => a.Submission.ReviewerAssignments,
    //                a => a.Submission.ReviewerAssignments.Select(ra => ra.Reviews)

    //            },
    //            Tracked = false
    //        });


    //        var submission = assignment.Submission;

    //        // Collect submitted reviews across all assignments for this submission
    //        var submittedReviews = submission.ReviewerAssignments
    //            .SelectMany(ra => ra.Reviews ?? new List<Review>())
    //            .Where(r => r.IsActive && r.Status == ReviewStatus.Submitted)
    //            .ToList();

    //        // If submission is escalated to moderator, a single submitted review (the moderator's extra reviewer)
    //        // should decide the final status. Otherwise the standard two-reviewer rules apply.
    //        if (submission.Status == SubmissionStatus.EscalatedToModerator && submittedReviews.Count >= 1)
    //        {
    //            // Use the latest submitted review from the assignment that triggered this call (assignment variable)
    //            var latestForThisAssignment = (assignment.Reviews ?? new List<Review>())
    //                .Where(r => r.IsActive && r.Status == ReviewStatus.Submitted)
    //                .OrderByDescending(r => r.SubmittedAt)
    //                .FirstOrDefault();

    //            if (latestForThisAssignment != null)
    //            {
    //                if (latestForThisAssignment.Recommendation == ReviewRecommendations.MinorRevision || latestForThisAssignment.Recommendation == ReviewRecommendations.MajorRevision)
    //                {
    //                    submission.Status = SubmissionStatus.RevisionRequired;
    //                }
    //                else if (latestForThisAssignment.Recommendation == ReviewRecommendations.Approve)
    //                {
    //                    submission.Status = SubmissionStatus.Approved;
    //                }
    //                else if (latestForThisAssignment.Recommendation == ReviewRecommendations.Reject)
    //                {
    //                    submission.Status = SubmissionStatus.Rejected;
    //                }
    //            }
    //        }
    //        else
    //        {
    //            if (submittedReviews.Count < 2) return; // wait for at least two submitted reviews

    //            var approveCount = submittedReviews.Count(r => r.Recommendation == ReviewRecommendations.Approve);
    //            var rejectCount = submittedReviews.Count(r => r.Recommendation == ReviewRecommendations.Reject);
    //            var revisionCount = submittedReviews.Count(r => r.Recommendation == ReviewRecommendations.MinorRevision || r.Recommendation == ReviewRecommendations.MajorRevision);

    //            if (revisionCount > 0)
    //            {
    //                submission.Status = SubmissionStatus.RevisionRequired;
    //            }
    //            else if (approveCount >= 2)
    //            {
    //                submission.Status = SubmissionStatus.Approved;
    //            }
    //            else if (rejectCount >= 2)
    //            {
    //                submission.Status = SubmissionStatus.Rejected;
    //            }
    //            else if (approveCount > 0 && rejectCount > 0)
    //            {
    //                submission.Status = SubmissionStatus.EscalatedToModerator;
    //            }
    //        }

    //        // Persist submission status change
    //        await submissionRepo.UpdateAsync(submission);

    //        // Also update related assignments: mark assignments that have a submitted review as Completed or Overdue
    //        // Use the latest submitted review for each assignment to determine completed time
    //        var assignmentIdsWithSubmitted = submittedReviews.Select(r => r.AssignmentId).Distinct().ToList();
    //        var assignmentsToUpdate = submission.ReviewerAssignments
    //            .Where(ra => assignmentIdsWithSubmitted.Contains(ra.Id))
    //            .ToList();

    //        var updatedAssignmentIds = new List<int>();
    //        foreach (var ra in assignmentsToUpdate)
    //        {
    //            try
    //            {
    //                // find latest submitted review for this assignment
    //                var latestSubmitted = (ra.Reviews ?? new List<Review>())
    //                    .Where(r => r.IsActive && r.Status == ReviewStatus.Submitted)
    //                    .OrderByDescending(r => r.SubmittedAt)
    //                    .FirstOrDefault();

    //                if (latestSubmitted == null) continue;

    //                // Determine on-time vs overdue using UTC comparisons; if no deadline, treat as on-time
    //                var completedAt = latestSubmitted.SubmittedAt ?? DateTime.UtcNow;
    //                var isOnTime = true;
    //                if (ra.Deadline.HasValue)
    //                {
    //                    isOnTime = completedAt.ToUniversalTime() <= ra.Deadline.Value.ToUniversalTime();
    //                }

    //                ra.CompletedAt = completedAt;
    //                ra.Status = isOnTime ? AssignmentStatus.Completed : AssignmentStatus.Overdue;

    //                // Update the assignment row (tracked=false so UpdateAsync will attach)
    //                await assignmentRepo.UpdateAsync(ra);
    //                updatedAssignmentIds.Add(ra.Id);
    //            }
    //            catch { /* best-effort, do not block */ }
    //        }

    //        // Persist all assignment changes first so performance recalculation reads persisted state
    //        var saveResult = await _unitOfWork.SaveAsync();
    //        if (saveResult.IsSuccess)
    //        {
    //            // Now recalculate reviewer performance for updated assignments (best-effort)
    //            foreach (var aid in updatedAssignmentIds.Distinct())
    //            {
    //                try
    //                {
    //                    await UpdateReviewerPerformanceForAssignmentAsync(aid);
    //                }
    //                catch { }
    //            }
    //        }
    //    }
    //    catch
    //    {
    //        // Best-effort: do not throw
    //    }
    //}

    public async Task<BaseResponseModel<ReviewResponseDTO>> CreateAsync(CreateReviewDTO createDTO, int currentUserId)
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
                // // Additional validation: ensure score fits DB decimal(4,2) (max 99.99)
                // if (scoreDTO.Score > 99.99m)
                // {
                //     await _unitOfWork.RollBackAsync();
                //     return new BaseResponseModel<ReviewResponseDTO>
                //     {
                //         IsSuccess = false,
                //         StatusCode = StatusCodes.Status400BadRequest,
                //         Message = $"Điểm cho tiêu chí '{criteriaItem.Name}' không thể lớn hơn 99.99 (giá trị nhận được: {scoreDTO.Score})"
                //     };
                // }
            }

            // Create review
            var review = new Review
            {
                AssignmentId = createDTO.AssignmentId,
                OverallComment = createDTO.OverallComment,
                Recommendation = createDTO.Recommendation,
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
                // // Additional validation: ensure score fits DB decimal(4,2) (max 99.99)
                // if (scoreDTO.Score > 99.99m)
                // {
                //     await _unitOfWork.RollBackAsync();
                //     return new BaseResponseModel<ReviewResponseDTO>
                //     {
                //         IsSuccess = false,
                //         StatusCode = StatusCodes.Status400BadRequest,
                //         Message = $"Điểm cho tiêu chí '{criteriaItem.Name}' không thể lớn hơn 99.99 (giá trị nhận được: {scoreDTO.Score})"
                //     };
                // }
            }

            // Update review (do not accept TimeSpentMinutes from client; it's server-computed on submit)
            review.OverallComment = updateDTO.OverallComment;
            review.Recommendation = updateDTO.Recommendation;
            // review.TimeSpentMinutes is server-authoritative; do not overwrite here
            review.LastModifiedAt = DateTime.Now;

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
                // Ensure nested navigation (Criteria on each ReviewCriteriaScore) is included so AutoMapper
                // can populate Criteria in CriteriaScoreResponseDTO and controller projections won't get null refs
                AdvancedIncludes = new List<App.DAL.Queries.Interfaces.IIncludeSpecification<Review>>
                {
                    new App.DAL.Queries.Implementations.ComplexIncludeSpecification<Review>("ReviewCriteriaScores.Criteria")
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
                AdvancedIncludes = new List<App.DAL.Queries.Interfaces.IIncludeSpecification<Review>>
                {
                    new App.DAL.Queries.Implementations.ComplexIncludeSpecification<Review>("ReviewCriteriaScores.Criteria")
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

            // If TimeSpentMinutes is not provided, try to compute it from assignment timestamps
            try
            {
                if (!review.TimeSpentMinutes.HasValue || review.TimeSpentMinutes.Value <= 0)
                {
                    var assignmentRepo = _unitOfWork.GetRepo<ReviewerAssignment>();
                    var assignment = await assignmentRepo.GetSingleAsync(new QueryOptions<ReviewerAssignment>
                    {
                        Predicate = a => a.Id == review.AssignmentId,
                        Tracked = false
                    });

                    DateTime? startTime = null;

                    if (assignment != null)
                    {
                        // Prefer StartedAt (when reviewer explicitly started), then AssignedAt
                        if (assignment.StartedAt.HasValue)
                        {
                            startTime = assignment.StartedAt.Value;
                        }
                        else if (assignment.AssignedAt != default(DateTime))
                        {
                            startTime = assignment.AssignedAt;
                        }
                    }

                    // Fallback to review.CreatedAt if available
                    if (!startTime.HasValue && review.CreatedAt != default(DateTime))
                    {
                        startTime = review.CreatedAt;
                    }

                    // If we have a startTime, compute minutes; otherwise set a safe minimum (best-effort)
                    if (startTime.HasValue)
                    {
                        var endTime = review.SubmittedAt ?? DateTime.UtcNow;
                        var minutes = (int)Math.Round((endTime.ToUniversalTime() - startTime.Value.ToUniversalTime()).TotalMinutes);
                        // clamp to reasonable range (1-600)
                        minutes = Math.Max(1, Math.Min(600, minutes));
                        review.TimeSpentMinutes = minutes;
                    }
                    else
                    {
                        // As a last resort, set a minimal positive time so averages are not skewed by null/zero
                        review.TimeSpentMinutes = 1;
                    }
                }
            }
            catch
            {
                // best-effort: do not block submit if computation fails
                try
                {
                    // ensure at least a minimal positive value
                    if (!review.TimeSpentMinutes.HasValue || review.TimeSpentMinutes.Value <= 0)
                    {
                        review.TimeSpentMinutes = 1;
                    }
                }
                catch { }
            }

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

            // Ensure submission status is updated according to review recommendations (two reviewers logic)
            try
            {
                await UpdateSubmissionStatusAfterSubmit(review.AssignmentId);
            }
            catch { /* do not block submit if status update fails */ }

            // After assignment status changes are persisted, update reviewer performance metrics (best-effort)
            try
            {
                await UpdateReviewerPerformanceForAssignmentAsync(review.AssignmentId);
            }
            catch { /* don't block submit if perf update fails */ }

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
                AdvancedIncludes = new List<App.DAL.Queries.Interfaces.IIncludeSpecification<Review>>
                {
                    new App.DAL.Queries.Implementations.ComplexIncludeSpecification<Review>("ReviewCriteriaScores.Criteria")
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

    /// <summary>
    /// Recalculate and persist ReviewerPerformance for the reviewer of the given assignment.
    /// This aggregates assignments and submitted reviews within the same semester as the submission/topic.
    /// Best-effort: failures are logged by callers; do not throw to block user actions.
    /// </summary>
    public async Task UpdateReviewerPerformanceForAssignmentAsync(int assignmentId)
    {
        // caller does not need the specific review instance; load assignment and recompute
        var assignmentRepo = _unitOfWork.GetRepo<ReviewerAssignment>();
        var assignment = await assignmentRepo.GetSingleAsync(new QueryOptions<ReviewerAssignment>
        {
            Predicate = a => a.Id == assignmentId,
            IncludeProperties = new List<System.Linq.Expressions.Expression<Func<ReviewerAssignment, object>>>
            {
                a => a.Submission,
                a => a.Submission.TopicVersion,
                a => a.Submission.TopicVersion.Topic,
                a => a.Submission.Topic,
                a => a.Reviewer,
                a => a.Reviews
            },
            Tracked = false
        });

        if (assignment == null) return;

        // Reuse the same aggregation logic below by calling the private implementation
        await UpdateReviewerPerformanceAfterReviewAsync_Internal(assignment);
    }

    // Internal helper that accepts a loaded assignment
    private async Task UpdateReviewerPerformanceAfterReviewAsync_Internal(ReviewerAssignment assignment)
    {
        try
        {
            var reviewerId = assignment.ReviewerId;

            // Determine semester id from available navigation properties
            int? semesterId = assignment.Submission?.TopicVersion?.Topic?.SemesterId
                              ?? assignment.Submission?.Topic?.SemesterId;
            if (!semesterId.HasValue) return;
            var semId = semesterId.Value;

            // Fetch all assignments for the reviewer (EF sometimes struggles with deep nested predicates)
            var assignmentRepo = _unitOfWork.GetRepo<ReviewerAssignment>();
            var allAssignments = await assignmentRepo.GetAllAsync(new QueryOptions<ReviewerAssignment>
            {
                Predicate = ra => ra.ReviewerId == reviewerId,
                IncludeProperties = new List<System.Linq.Expressions.Expression<Func<ReviewerAssignment, object>>>
                {
                    ra => ra.Reviews,
                    ra => ra.Submission,
                    ra => ra.Submission.TopicVersion,
                    ra => ra.Submission.TopicVersion.Topic,
                    ra => ra.Submission.Topic
                },
                Tracked = false
            });

            // Filter by semester in-memory to avoid EF translation issues
            var assignmentsInSemester = allAssignments.Where(ra =>
                (ra.Submission?.TopicVersion?.Topic?.SemesterId == semId) ||
                (ra.Submission?.Topic?.SemesterId == semId)
            ).ToList();

            var totalAssignments = assignmentsInSemester.Count;
            var completedAssignments = assignmentsInSemester.Count(a => a.Status == AssignmentStatus.Completed || a.Reviews.Any(r => r.Status == ReviewStatus.Submitted));

            // Collect submitted reviews from these assignments
            var submittedReviews = assignmentsInSemester
                .SelectMany(a => a.Reviews ?? new List<Review>())
                .Where(r => r.IsActive && r.Status == ReviewStatus.Submitted)
                .ToList();

            // Compute average only from reviews that have a positive TimeSpentMinutes (server-computed on submit)
            var timeValues = submittedReviews.Where(r => r.TimeSpentMinutes.HasValue && r.TimeSpentMinutes.Value > 0)
                .Select(r => r.TimeSpentMinutes!.Value).ToList();
            var avgTime = timeValues.Any() ? (int)Math.Round(timeValues.Average()) : 0;
            var avgScore = submittedReviews.Any() ? (decimal?)submittedReviews.Where(r => r.OverallScore.HasValue).Average(r => r.OverallScore!.Value) : null;

            // Compute on-time rate by matching review.AssignmentId -> assignment.Deadline
            int onTimeCount = 0;
            foreach (var r in submittedReviews)
            {
                var parentAssignment = assignmentsInSemester.FirstOrDefault(a => a.Id == r.AssignmentId);
                if (parentAssignment != null && r.SubmittedAt.HasValue && parentAssignment.Deadline.HasValue && r.SubmittedAt.Value <= parentAssignment.Deadline.Value)
                {
                    onTimeCount++;
                }
            }

            var onTimeRate = submittedReviews.Any() ? (decimal?) ((decimal)onTimeCount / submittedReviews.Count) : null;

            // Update or create ReviewerPerformance
            var perfRepo = _unitOfWork.GetRepo<ReviewerPerformance>();
            var perf = await perfRepo.GetSingleAsync(new QueryOptions<ReviewerPerformance>
            {
                Predicate = rp => rp.ReviewerId == reviewerId && rp.SemesterId == semId
            });

            if (perf == null)
            {
                perf = new ReviewerPerformance
                {
                    ReviewerId = reviewerId,
                    SemesterId = semId,
                    TotalAssignments = totalAssignments,
                    CompletedAssignments = completedAssignments,
                    AverageTimeMinutes = avgTime,
                    AverageScoreGiven = avgScore,
                    OnTimeRate = onTimeRate * 100m,
                    LastUpdated = DateTime.UtcNow,
                    // Initialize QualityRating as proportionally from on-time performance (scale 0..100)
                    QualityRating = onTimeRate.HasValue ? (onTimeRate.Value * 100m) : 0m
                };
                await perfRepo.CreateAsync(perf);
            }
            else
            {
                perf.TotalAssignments = totalAssignments;
                perf.CompletedAssignments = completedAssignments;
                perf.AverageTimeMinutes = avgTime;
                perf.AverageScoreGiven = avgScore;
                perf.OnTimeRate = onTimeRate * 100;
                perf.LastUpdated = DateTime.UtcNow;
                // Update QualityRating: base it on on-time rate (0..100 scale). If there's an existing QualityRating,
                // blend it conservatively by averaging with current onTimeRate*100 to avoid sudden jumps.
                var currentQuality = perf.QualityRating ?? 0m;
                var latestQuality = onTimeRate.HasValue ? (onTimeRate.Value * 100m) : 0m;
                perf.QualityRating = Math.Round((currentQuality + latestQuality) / 2m, 2);
                await perfRepo.UpdateAsync(perf);
            }

            await _unitOfWork.SaveChangesAsync();
        }
        catch
        {
            // swallow exceptions - best-effort update
        }
    }
}