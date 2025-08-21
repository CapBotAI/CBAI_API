using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using App.BLL.Interfaces;
using App.Commons.ResponseModel;
using App.DAL.UnitOfWork;
using App.DAL.Queries;
using App.Entities.DTOs.ReviewerAssignment;
using App.Entities.Entities.App;
using App.Entities.Entities.Core;
using App.Entities.Enums;
using App.Entities.Constants;
using System.Linq.Expressions;

namespace App.BLL.Implementations;

public class ReviewerAssignmentService : IReviewerAssignmentService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly IPerformanceMatchingService _performanceMatchingService;
    private readonly ISkillMatchingService skillMatchingService;

    public ReviewerAssignmentService(IUnitOfWork unitOfWork, IMapper mapper,
        IPerformanceMatchingService performanceMatchingService, ISkillMatchingService skillMatchingService)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _performanceMatchingService = performanceMatchingService;
        this.skillMatchingService = skillMatchingService;
        _skillMatchingService = skillMatchingService;
    }

    public async Task<BaseResponseModel<ReviewerAssignmentResponseDTO>> AssignReviewerAsync(AssignReviewerDTO dto,
        int assignedById)
    {
        try
        {
            // Validate submission exists
            var submissionOptions = new QueryOptions<Submission>
            {
                Predicate = s => s.Id == dto.SubmissionId
            };
            var submission = await _unitOfWork.GetRepo<Submission>().GetSingleAsync(submissionOptions);
            if (submission == null)
            {
                return new BaseResponseModel<ReviewerAssignmentResponseDTO>
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status404NotFound,
                    Message = "Submission không tồn tại"
                };
            }

            // Validate reviewer exists and has correct role
            var reviewerOptions = new QueryOptions<User>
            {
                Predicate = u => u.Id == dto.ReviewerId,
                IncludeProperties = new List<Expression<Func<User, object>>>
                {
                    u => u.UserRoles
                }
            };
            var reviewer = await _unitOfWork.GetRepo<User>().GetSingleAsync(reviewerOptions);

            if (reviewer == null)
            {
                return new BaseResponseModel<ReviewerAssignmentResponseDTO>
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status404NotFound,
                    Message = "Reviewer không tồn tại"
                };
            }

            // Get roles for reviewer to check if has Reviewer role
            var roleOptions = new QueryOptions<UserRole>
            {
                Predicate = ur => ur.UserId == dto.ReviewerId,
                IncludeProperties = new List<Expression<Func<UserRole, object>>>
                {
                    ur => ur.Role
                }
            };
            var userRoles = await _unitOfWork.GetRepo<UserRole>().GetAllAsync(roleOptions);
            var hasReviewerRole = userRoles.Any(ur => ur.Role.Name == SystemRoleConstants.Reviewer);

            if (!hasReviewerRole)
            {
                return new BaseResponseModel<ReviewerAssignmentResponseDTO>
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status400BadRequest,
                    Message = "User không có quyền reviewer"
                };
            }

            // Check if assignment already exists
            var existingAssignmentOptions = new QueryOptions<ReviewerAssignment>
            {
                Predicate = ra => ra.SubmissionId == dto.SubmissionId && ra.ReviewerId == dto.ReviewerId
            };
            var existingAssignment =
                await _unitOfWork.GetRepo<ReviewerAssignment>().GetSingleAsync(existingAssignmentOptions);

            if (existingAssignment != null)
            {
                return new BaseResponseModel<ReviewerAssignmentResponseDTO>
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status409Conflict,
                    Message = "Reviewer đã được phân công cho submission này"
                };
            }

            // Check workload limits (optional business rule - max 10 active assignments per reviewer)
            var activeAssignmentOptions = new QueryOptions<ReviewerAssignment>
            {
                Predicate = ra => ra.ReviewerId == dto.ReviewerId &&
                                  (ra.Status == AssignmentStatus.Assigned || ra.Status == AssignmentStatus.InProgress)
            };
            var activeAssignments =
                await _unitOfWork.GetRepo<ReviewerAssignment>().GetAllAsync(activeAssignmentOptions);

            if (activeAssignments.Count() >= 10)
            {
                return new BaseResponseModel<ReviewerAssignmentResponseDTO>
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status400BadRequest,
                    Message = "Reviewer đang có quá nhiều assignment đang hoạt động"
                };
            }

            // Create new assignment
            var assignment = new ReviewerAssignment
            {
                SubmissionId = dto.SubmissionId,
                ReviewerId = dto.ReviewerId,
                AssignedBy = assignedById,
                AssignmentType = dto.AssignmentType,
                SkillMatchScore = dto.SkillMatchScore,
                Deadline = dto.Deadline,
                Status = AssignmentStatus.Assigned,
                AssignedAt = DateTime.Now
            };

            await _unitOfWork.GetRepo<ReviewerAssignment>().CreateAsync(assignment);
            await _unitOfWork.SaveChangesAsync();

            // Fetch the created assignment with related data
            var createdAssignmentOptions = new QueryOptions<ReviewerAssignment>
            {
                Predicate = ra => ra.Id == assignment.Id,
                IncludeProperties = new List<Expression<Func<ReviewerAssignment, object>>>
                {
                    ra => ra.Reviewer,
                    ra => ra.AssignedByUser,
                    ra => ra.Submission,
                    ra => ra.Submission.TopicVersion,
                    ra => ra.Submission.TopicVersion.Topic
                }
            };
            var createdAssignment =
                await _unitOfWork.GetRepo<ReviewerAssignment>().GetSingleAsync(createdAssignmentOptions);

            var response = _mapper.Map<ReviewerAssignmentResponseDTO>(createdAssignment);

            return new BaseResponseModel<ReviewerAssignmentResponseDTO>
            {
                IsSuccess = true,
                StatusCode = StatusCodes.Status201Created,
                Message = "Phân công reviewer thành công",
                Data = response
            };
        }
        catch (Exception)
        {
            throw;
        }
    }

    public async Task<BaseResponseModel<List<ReviewerAssignmentResponseDTO>>> BulkAssignReviewersAsync(
        BulkAssignReviewerDTO dto, int assignedById)
    {
        try
        {
            var results = new List<ReviewerAssignmentResponseDTO>();
            var errors = new List<string>();

            foreach (var assignmentDto in dto.Assignments)
            {
                var result = await AssignReviewerAsync(assignmentDto, assignedById);
                if (result.IsSuccess)
                {
                    results.Add(result.Data!);
                }
                else
                {
                    errors.Add(
                        $"Submission {assignmentDto.SubmissionId} - Reviewer {assignmentDto.ReviewerId}: {result.Message}");
                }
            }

            if (errors.Any())
            {
                return new BaseResponseModel<List<ReviewerAssignmentResponseDTO>>
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status400BadRequest,
                    Message = $"Một số assignment thất bại: {string.Join("; ", errors)}",
                    Data = results
                };
            }

            return new BaseResponseModel<List<ReviewerAssignmentResponseDTO>>
            {
                IsSuccess = true,
                StatusCode = StatusCodes.Status201Created,
                Message = $"Phân công thành công {results.Count} reviewer",
                Data = results
            };
        }
        catch (Exception)
        {
            throw;
        }
    }

    public async Task<BaseResponseModel<List<AvailableReviewerDTO>>> GetAvailableReviewersAsync(int submissionId)
    {
        try
        {
            // Get all users with Reviewer role
            var userRoleOptions = new QueryOptions<UserRole>
            {
                IncludeProperties = new List<Expression<Func<UserRole, object>>>
                {
                    ur => ur.User,
                    ur => ur.User.LecturerSkills,
                    ur => ur.User.ReviewerPerformances,
                    ur => ur.Role
                },
                Predicate = ur => ur.Role.Name == SystemRoleConstants.Reviewer
            };
            var userRoles = await _unitOfWork.GetRepo<UserRole>().GetAllAsync(userRoleOptions);
            var reviewers = userRoles.Select(ur => ur.User).Distinct().ToList();

            // Get already assigned reviewers for this submission
            var assignedReviewerOptions = new QueryOptions<ReviewerAssignment>
            {
                Predicate = ra => ra.SubmissionId == submissionId
            };
            var assignedReviewers =
                await _unitOfWork.GetRepo<ReviewerAssignment>().GetAllAsync(assignedReviewerOptions);
            var assignedReviewerIds = assignedReviewers.Select(ra => ra.ReviewerId).ToList();

            var availableReviewers = new List<AvailableReviewerDTO>();

            foreach (var reviewer in reviewers)
            {
                var isAlreadyAssigned = assignedReviewerIds.Contains(reviewer.Id);

                // Get active assignments for this reviewer
                var activeAssignmentOptions = new QueryOptions<ReviewerAssignment>
                {
                    Predicate = ra => ra.ReviewerId == reviewer.Id &&
                                      (ra.Status == AssignmentStatus.Assigned ||
                                       ra.Status == AssignmentStatus.InProgress)
                };
                var activeAssignments =
                    await _unitOfWork.GetRepo<ReviewerAssignment>().GetAllAsync(activeAssignmentOptions);
                var activeCount = activeAssignments.Count();

                var performance = reviewer.ReviewerPerformances.FirstOrDefault();

                var availableReviewer = new AvailableReviewerDTO
                {
                    Id = reviewer.Id,
                    UserName = reviewer.UserName,
                    Email = reviewer.Email,
                    PhoneNumber = reviewer.PhoneNumber,
                    CurrentAssignments = activeCount,
                    CompletedAssignments = performance?.CompletedAssignments ?? 0,
                    AverageScoreGiven = performance?.AverageScoreGiven,
                    OnTimeRate = performance?.OnTimeRate,
                    QualityRating = performance?.QualityRating,
                    Skills = reviewer.LecturerSkills.Select(ls => ls.SkillTag).ToList(),
                    IsAvailable = !isAlreadyAssigned && activeCount < 10,
                    UnavailableReason = isAlreadyAssigned ? "Đã được phân công" :
                        activeCount >= 10 ? "Quá tải assignment" : null
                };

                availableReviewers.Add(availableReviewer);
            }

            return new BaseResponseModel<List<AvailableReviewerDTO>>
            {
                IsSuccess = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Lấy danh sách reviewer thành công",
                Data = availableReviewers.OrderByDescending(r => r.IsAvailable)
                    .ThenBy(r => r.CurrentAssignments)
                    .ToList()
            };
        }
        catch (Exception)
        {
            throw;
        }
    }

    public async Task<BaseResponseModel<List<ReviewerAssignmentResponseDTO>>> GetAssignmentsBySubmissionAsync(
        int submissionId)
    {
        try
        {
            var assignmentOptions = new QueryOptions<ReviewerAssignment>
            {
                Predicate = ra => ra.SubmissionId == submissionId,
                IncludeProperties = new List<Expression<Func<ReviewerAssignment, object>>>
                {
                    ra => ra.Reviewer,
                    ra => ra.AssignedByUser,
                    ra => ra.Submission,
                    ra => ra.Submission.TopicVersion,
                    ra => ra.Submission.TopicVersion.Topic
                }
            };
            var assignments = await _unitOfWork.GetRepo<ReviewerAssignment>().GetAllAsync(assignmentOptions);

            var response = _mapper.Map<List<ReviewerAssignmentResponseDTO>>(assignments);

            return new BaseResponseModel<List<ReviewerAssignmentResponseDTO>>
            {
                IsSuccess = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Lấy danh sách assignment thành công",
                Data = response
            };
        }
        catch (Exception)
        {
            throw;
        }
    }

    public async Task<BaseResponseModel<List<ReviewerAssignmentResponseDTO>>> GetAssignmentsByReviewerAsync(
        int reviewerId)
    {
        try
        {
            var assignmentOptions = new QueryOptions<ReviewerAssignment>
            {
                Predicate = ra => ra.ReviewerId == reviewerId,
                IncludeProperties = new List<Expression<Func<ReviewerAssignment, object>>>
                {
                    ra => ra.Reviewer,
                    ra => ra.AssignedByUser,
                    ra => ra.Submission,
                    ra => ra.Submission.TopicVersion,
                    ra => ra.Submission.TopicVersion.Topic
                },
                OrderBy = query => query.OrderByDescending(ra => ra.AssignedAt)
            };
            var assignments = await _unitOfWork.GetRepo<ReviewerAssignment>().GetAllAsync(assignmentOptions);

            var response = _mapper.Map<List<ReviewerAssignmentResponseDTO>>(assignments);

            return new BaseResponseModel<List<ReviewerAssignmentResponseDTO>>
            {
                IsSuccess = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Lấy danh sách assignment thành công",
                Data = response
            };
        }
        catch (Exception)
        {
            throw;
        }
    }

    public async Task<BaseResponseModel<ReviewerAssignmentResponseDTO>> UpdateAssignmentStatusAsync(
        int assignmentId, AssignmentStatus newStatus, int updatedById)
    {
        try
        {
            var assignmentOptions = new QueryOptions<ReviewerAssignment>
            {
                Predicate = ra => ra.Id == assignmentId
            };
            var assignment = await _unitOfWork.GetRepo<ReviewerAssignment>().GetSingleAsync(assignmentOptions);

            if (assignment == null)
            {
                return new BaseResponseModel<ReviewerAssignmentResponseDTO>
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status404NotFound,
                    Message = "Assignment không tồn tại"
                };
            }

            // Update status and timestamps
            assignment.Status = newStatus;
            if (newStatus == AssignmentStatus.InProgress && assignment.StartedAt == null)
            {
                assignment.StartedAt = DateTime.Now;
            }
            else if (newStatus == AssignmentStatus.Completed && assignment.CompletedAt == null)
            {
                assignment.CompletedAt = DateTime.Now;
            }

            await _unitOfWork.GetRepo<ReviewerAssignment>().UpdateAsync(assignment);
            await _unitOfWork.SaveChangesAsync();

            // Fetch updated assignment with related data
            var updatedAssignmentOptions = new QueryOptions<ReviewerAssignment>
            {
                Predicate = ra => ra.Id == assignmentId,
                IncludeProperties = new List<Expression<Func<ReviewerAssignment, object>>>
                {
                    ra => ra.Reviewer,
                    ra => ra.AssignedByUser,
                    ra => ra.Submission,
                    ra => ra.Submission.TopicVersion,
                    ra => ra.Submission.TopicVersion.Topic
                }
            };
            var updatedAssignment =
                await _unitOfWork.GetRepo<ReviewerAssignment>().GetSingleAsync(updatedAssignmentOptions);

            var response = _mapper.Map<ReviewerAssignmentResponseDTO>(updatedAssignment);

            return new BaseResponseModel<ReviewerAssignmentResponseDTO>
            {
                IsSuccess = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Cập nhật status assignment thành công",
                Data = response
            };
        }
        catch (Exception)
        {
            throw;
        }
    }

    public async Task<BaseResponseModel> RemoveAssignmentAsync(int assignmentId, int removedById)
    {
        try
        {
            var assignmentOptions = new QueryOptions<ReviewerAssignment>
            {
                Predicate = ra => ra.Id == assignmentId
            };
            var assignment = await _unitOfWork.GetRepo<ReviewerAssignment>().GetSingleAsync(assignmentOptions);

            if (assignment == null)
            {
                return new BaseResponseModel
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status404NotFound,
                    Message = "Assignment không tồn tại"
                };
            }

            // Check if assignment has started (has reviews)
            var reviewOptions = new QueryOptions<Review>
            {
                Predicate = r => r.AssignmentId == assignmentId
            };
            var hasReviews = await _unitOfWork.GetRepo<Review>().AnyAsync(reviewOptions);

            if (hasReviews)
            {
                return new BaseResponseModel
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status400BadRequest,
                    Message = "Không thể xóa assignment đã có review"
                };
            }

            await _unitOfWork.GetRepo<ReviewerAssignment>().DeleteAsync(assignment);
            await _unitOfWork.SaveChangesAsync();

            return new BaseResponseModel
            {
                IsSuccess = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Xóa assignment thành công"
            };
        }
        catch (Exception)
        {
            throw;
        }
    }

    public async Task<BaseResponseModel<List<AvailableReviewerDTO>>> GetReviewersWorkloadAsync(int? semesterId = null)
    {
        try
        {
            // Get all users with Reviewer role
            var userRoleOptions = new QueryOptions<UserRole>
            {
                IncludeProperties = new List<Expression<Func<UserRole, object>>>
                {
                    ur => ur.User,
                    ur => ur.User.LecturerSkills,
                    ur => ur.User.ReviewerPerformances,
                    ur => ur.Role
                },
                Predicate = ur => ur.Role.Name == SystemRoleConstants.Reviewer
            };
            var userRoles = await _unitOfWork.GetRepo<UserRole>().GetAllAsync(userRoleOptions);
            var reviewers = userRoles.Select(ur => ur.User).Distinct().ToList();

            var workloadInfo = new List<AvailableReviewerDTO>();

            foreach (var reviewer in reviewers)
            {
                // Get assignments for this reviewer
                var assignmentOptions = new QueryOptions<ReviewerAssignment>
                {
                    Predicate = ra => ra.ReviewerId == reviewer.Id,
                    IncludeProperties = new List<Expression<Func<ReviewerAssignment, object>>>
                    {
                        ra => ra.Submission,
                        ra => ra.Submission.TopicVersion,
                        ra => ra.Submission.TopicVersion.Topic
                    }
                };
                var assignments = await _unitOfWork.GetRepo<ReviewerAssignment>().GetAllAsync(assignmentOptions);

                if (semesterId.HasValue)
                {
                    // Filter by semester if provided
                    assignments = assignments.Where(ra =>
                        ra.Submission.TopicVersion.Topic.SemesterId == semesterId.Value);
                }

                var activeAssignments = assignments.Count(ra =>
                    ra.Status == AssignmentStatus.Assigned || ra.Status == AssignmentStatus.InProgress);
                var completedAssignments = assignments.Count(ra => ra.Status == AssignmentStatus.Completed);

                var performance = reviewer.ReviewerPerformances
                    .FirstOrDefault(rp => !semesterId.HasValue || rp.SemesterId == semesterId.Value);

                var workload = new AvailableReviewerDTO
                {
                    Id = reviewer.Id,
                    UserName = reviewer.UserName,
                    Email = reviewer.Email,
                    PhoneNumber = reviewer.PhoneNumber,
                    CurrentAssignments = activeAssignments,
                    CompletedAssignments = completedAssignments,
                    AverageScoreGiven = performance?.AverageScoreGiven,
                    OnTimeRate = performance?.OnTimeRate,
                    QualityRating = performance?.QualityRating,
                    Skills = reviewer.LecturerSkills.Select(ls => ls.SkillTag).ToList(),
                    IsAvailable = activeAssignments < 10
                };

                workloadInfo.Add(workload);
            }

            return new BaseResponseModel<List<AvailableReviewerDTO>>
            {
                IsSuccess = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Lấy thông tin workload thành công",
                Data = workloadInfo.OrderBy(w => w.CurrentAssignments).ToList()
            };
        }
        catch (Exception)
        {
            throw;
        }
    }
    // Thêm methods mới vào class ReviewerAssignmentService

    private readonly ISkillMatchingService _skillMatchingService;

// Update constructor
    public ReviewerAssignmentService(IUnitOfWork unitOfWork, IMapper mapper, ISkillMatchingService skillMatchingService)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        this.skillMatchingService = skillMatchingService;
        _skillMatchingService = skillMatchingService;
    }

    public async Task<BaseResponseModel<AutoAssignmentResult>> AutoAssignReviewersAsync(AutoAssignReviewerDTO dto,
        int assignedById)
    {
        try
        {
            // Validate submission exists
            var submissionOptions = new QueryOptions<Submission>
            {
                Predicate = s => s.Id == dto.SubmissionId,
                IncludeProperties = new List<Expression<Func<Submission, object>>>
                {
                    s => s.TopicVersion,
                    s => s.TopicVersion.Topic
                }
            };
            var submission = await _unitOfWork.GetRepo<Submission>().GetSingleAsync(submissionOptions);

            if (submission == null)
            {
                return new BaseResponseModel<AutoAssignmentResult>
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status404NotFound,
                    Message = "Submission không tồn tại"
                };
            }

            // Find best performing reviewers (thay vì skill-based)
            var matchingReviewers =
                await _performanceMatchingService.FindBestPerformingReviewersAsync(dto.SubmissionId, dto);
            var eligibleReviewers = matchingReviewers.Where(r => r.IsEligible).ToList();

            var result = new AutoAssignmentResult
            {
                SubmissionId = dto.SubmissionId,
                TopicTitle = submission.TopicVersion.Topic.Title,
                TopicSkillTags = new List<string> { "Performance-Based Assignment" }, // Placeholder
                ConsideredReviewers = matchingReviewers,
                RequestedReviewers = dto.NumberOfReviewers,
                AssignedCount = 0,
                IsFullyAssigned = false
            };

            if (!eligibleReviewers.Any())
            {
                result.Warnings.Add("Không tìm thấy reviewer phù hợp với tiêu chí performance");
                return new BaseResponseModel<AutoAssignmentResult>
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status400BadRequest,
                    Message = "Không có reviewer phù hợp",
                    Data = result
                };
            }

            // Select top performers to assign
            var reviewersToAssign = eligibleReviewers
                .Take(dto.NumberOfReviewers)
                .ToList();

            // Assign selected reviewers
            var assignedReviewers = new List<ReviewerAssignmentResponseDTO>();

            foreach (var reviewer in reviewersToAssign)
            {
                var assignDto = new AssignReviewerDTO
                {
                    SubmissionId = dto.SubmissionId,
                    ReviewerId = reviewer.ReviewerId,
                    AssignmentType = dto.AssignmentType,
                    Deadline = dto.Deadline,
                    SkillMatchScore = reviewer.PerformanceScore, // Use performance score instead
                    Notes = $"Auto-assigned based on performance score: {reviewer.PerformanceScore:F2}"
                };

                var assignResult = await AssignReviewerAsync(assignDto, assignedById);
                if (assignResult.IsSuccess)
                {
                    assignedReviewers.Add(assignResult.Data!);
                    result.AssignedCount++;
                }
                else
                {
                    result.Warnings.Add($"Không thể assign reviewer {reviewer.ReviewerName}: {assignResult.Message}");
                }
            }

            result.AssignedReviewers = assignedReviewers;
            result.IsFullyAssigned = result.AssignedCount == dto.NumberOfReviewers;

            if (result.AssignedCount == 0)
            {
                return new BaseResponseModel<AutoAssignmentResult>
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status400BadRequest,
                    Message = "Không thể assign bất kỳ reviewer nào",
                    Data = result
                };
            }

            var message = result.IsFullyAssigned
                ? $"Auto assign thành công {result.AssignedCount} reviewer dựa trên performance"
                : $"Auto assign thành công {result.AssignedCount}/{dto.NumberOfReviewers} reviewer dựa trên performance";

            return new BaseResponseModel<AutoAssignmentResult>
            {
                IsSuccess = true,
                StatusCode = StatusCodes.Status201Created,
                Message = message,
                Data = result
            };
        }
        catch (Exception)
        {
            throw;
        }
    }

    public async Task<BaseResponseModel<List<ReviewerMatchingResult>>> GetRecommendedReviewersAsync(
        int submissionId, AutoAssignReviewerDTO? criteria = null)
    {
        try
        {
            criteria ??= new AutoAssignReviewerDTO { SubmissionId = submissionId };

            var matchingReviewers =
                await _performanceMatchingService.FindBestPerformingReviewersAsync(submissionId, criteria);

            return new BaseResponseModel<List<ReviewerMatchingResult>>
            {
                IsSuccess = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Lấy danh sách reviewer được recommend dựa trên performance thành công",
                Data = matchingReviewers
            };
        }
        catch (Exception)
        {
            throw;
        }
    }

    public async Task<BaseResponseModel<ReviewerMatchingResult>> AnalyzeReviewerMatchAsync(int reviewerId,
        int submissionId)
    {
        try
        {
            // Get topic skill tags
            var topicSkillTags = await _skillMatchingService.ExtractTopicSkillTagsAsync(submissionId);

            // Get reviewer info
            var reviewerOptions = new QueryOptions<User>
            {
                Predicate = u => u.Id == reviewerId,
                IncludeProperties = new List<Expression<Func<User, object>>>
                {
                    u => u.LecturerSkills,
                    u => u.ReviewerPerformances
                }
            };
            var reviewer = await _unitOfWork.GetRepo<User>().GetSingleAsync(reviewerOptions);

            if (reviewer == null)
            {
                return new BaseResponseModel<ReviewerMatchingResult>
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status404NotFound,
                    Message = "Reviewer không tồn tại"
                };
            }

            var matchingResult = new ReviewerMatchingResult
            {
                ReviewerId = reviewer.Id,
                ReviewerName = reviewer.UserName,
                ReviewerEmail = reviewer.Email
            };

            // Calculate scores
            matchingResult.SkillMatchScore =
                await _skillMatchingService.CalculateSkillMatchScoreAsync(reviewerId, topicSkillTags);
            matchingResult.PerformanceScore = await _skillMatchingService.CalculatePerformanceScoreAsync(reviewerId);
            matchingResult.WorkloadScore = await _skillMatchingService.CalculateWorkloadScoreAsync(reviewerId);

            // Get additional info
            matchingResult.ReviewerSkills = reviewer.LecturerSkills.ToDictionary(
                ls => ls.SkillTag,
                ls => ls.ProficiencyLevel);

            var performance = reviewer.ReviewerPerformances.FirstOrDefault();
            if (performance != null)
            {
                matchingResult.CompletedAssignments = performance.CompletedAssignments;
                matchingResult.AverageScoreGiven = performance.AverageScoreGiven;
                matchingResult.OnTimeRate = performance.OnTimeRate;
                matchingResult.QualityRating = performance.QualityRating;
            }

            // Get current workload
            var activeAssignmentOptions = new QueryOptions<ReviewerAssignment>
            {
                Predicate = ra => ra.ReviewerId == reviewerId &&
                                  (ra.Status == AssignmentStatus.Assigned || ra.Status == AssignmentStatus.InProgress)
            };
            var activeAssignments =
                await _unitOfWork.GetRepo<ReviewerAssignment>().GetAllAsync(activeAssignmentOptions);
            matchingResult.CurrentActiveAssignments = activeAssignments.Count();

            return new BaseResponseModel<ReviewerMatchingResult>
            {
                IsSuccess = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Phân tích matching thành công",
                Data = matchingResult
            };
        }
        catch (Exception)
        {
            throw;
        }
    }

    public async Task<BaseResponseModel<List<ReviewerAssignmentResponseDTO>>> GetAssignmentsByReviewerAndStatusAsync(
        int reviewerId, AssignmentStatus status)
    {
        try
        {
            var assignmentOptions = new QueryOptions<ReviewerAssignment>
            {
                Predicate = ra => ra.ReviewerId == reviewerId && ra.Status == status,
                IncludeProperties = new List<Expression<Func<ReviewerAssignment, object>>>
                {
                    ra => ra.Submission,
                    ra => ra.Submission.TopicVersion,
                    ra => ra.Submission.TopicVersion.Topic,
                    ra => ra.Submission.SubmittedByUser,
                    ra => ra.Reviewer,
                    ra => ra.AssignedByUser
                },
                OrderBy = query => query.OrderByDescending(ra => ra.AssignedAt),
                Tracked = false
            };

            var assignments = await _unitOfWork.GetRepo<ReviewerAssignment>().GetAllAsync(assignmentOptions);
            var response = _mapper.Map<List<ReviewerAssignmentResponseDTO>>(assignments);

            return new BaseResponseModel<List<ReviewerAssignmentResponseDTO>>
            {
                IsSuccess = true,
                StatusCode = StatusCodes.Status200OK,
                Data = response
            };
        }
        catch (Exception ex)
        {
            return new BaseResponseModel<List<ReviewerAssignmentResponseDTO>>
            {
                IsSuccess = false,
                StatusCode = StatusCodes.Status500InternalServerError,
                Message = $"Lỗi hệ thống: {ex.Message}"
            };
        }
    }

    public async Task<BaseResponseModel<ReviewerStatisticsDTO>> GetReviewerStatisticsAsync(int reviewerId)
    {
        try
        {
            var assignmentOptions = new QueryOptions<ReviewerAssignment>
            {
                Predicate = ra => ra.ReviewerId == reviewerId,
                IncludeProperties = new List<Expression<Func<ReviewerAssignment, object>>>
                {
                    ra => ra.Submission.TopicVersion.Topic,
                    ra => ra.Submission.SubmittedByUser,
                    ra => ra.Reviews
                },
                Tracked = false
            };

            var assignments = await _unitOfWork.GetRepo<ReviewerAssignment>().GetAllAsync(assignmentOptions);
            var assignmentList = assignments.ToList();

            var userRepo = _unitOfWork.GetRepo<User>();
            var reviewer = await userRepo.GetSingleAsync(new QueryOptions<User>
            {
                Predicate = u => u.Id == reviewerId,
                Tracked = false
            });

            var statistics = new ReviewerStatisticsDTO
            {
                ReviewerId = reviewerId,
                ReviewerName = reviewer?.UserName ?? "Unknown",
                TotalAssignments = assignmentList.Count,
                CompletedAssignments = assignmentList.Count(a => a.Status == AssignmentStatus.Completed),
                InProgressAssignments = assignmentList.Count(a => a.Status == AssignmentStatus.InProgress),
                PendingAssignments = assignmentList.Count(a => a.Status == AssignmentStatus.Assigned),
                OverdueAssignments = assignmentList.Count(a =>
                    a.Deadline.HasValue && a.Deadline < DateTime.UtcNow && a.Status != AssignmentStatus.Completed)
            };

            statistics.CompletionRate = statistics.TotalAssignments > 0
                ? (decimal)statistics.CompletedAssignments / statistics.TotalAssignments * 100
                : 0;

            // Tính thời gian review trung bình
            var completedAssignments =
                assignmentList.Where(a => a.CompletedAt.HasValue && a.StartedAt.HasValue).ToList();
            if (completedAssignments.Any())
            {
                var totalHours = completedAssignments.Sum(a => (a.CompletedAt.Value - a.StartedAt.Value).TotalHours);
                statistics.AverageReviewTime = (decimal)(totalHours / completedAssignments.Count);
            }

            statistics.LastReviewDate = assignmentList
                .Where(a => a.CompletedAt.HasValue)
                .Max(a => a.CompletedAt);

            // Thống kê theo status
            statistics.AssignmentsByStatus = assignmentList
                .GroupBy(a => a.Status.ToString())
                .ToDictionary(g => g.Key, g => g.Count());

            // Recent assignments
            statistics.RecentAssignments = assignmentList
                .OrderByDescending(a => a.AssignedAt)
                .Take(5)
                .Select(a => new RecentAssignmentDTO
                {
                    AssignmentId = a.Id,
                    TopicTitle = a.Submission.TopicVersion.Topic.Title,
                    StudentName = a.Submission.SubmittedByUser.UserName ?? "Unknown",
                    Status = a.Status,
                    AssignedAt = a.AssignedAt,
                    Deadline = a.Deadline,
                    IsOverdue = a.Deadline.HasValue && a.Deadline < DateTime.UtcNow &&
                                a.Status != AssignmentStatus.Completed
                })
                .ToList();

            return new BaseResponseModel<ReviewerStatisticsDTO>
            {
                IsSuccess = true,
                StatusCode = StatusCodes.Status200OK,
                Data = statistics
            };
        }
        catch (Exception ex)
        {
            return new BaseResponseModel<ReviewerStatisticsDTO>
            {
                IsSuccess = false,
                StatusCode = StatusCodes.Status500InternalServerError,
                Message = $"Lỗi hệ thống: {ex.Message}"
            };
        }
    }

    public async Task<BaseResponseModel<ReviewerAssignmentResponseDTO>> StartReviewAsync(int assignmentId,
        int reviewerId)
    {
        try
        {
            var assignmentOptions = new QueryOptions<ReviewerAssignment>
            {
                Predicate = ra => ra.Id == assignmentId && ra.ReviewerId == reviewerId,
                IncludeProperties = new List<Expression<Func<ReviewerAssignment, object>>>
                {
                    ra => ra.Submission,
                    ra => ra.Submission.TopicVersion,
                    ra => ra.Submission.TopicVersion.Topic,
                    ra => ra.Submission.SubmittedByUser,
                    ra => ra.Reviewer,
                    ra => ra.AssignedByUser
                }
            };
            var assignment = await _unitOfWork.GetRepo<ReviewerAssignment>().GetSingleAsync(assignmentOptions);

            if (assignment == null)
            {
                return new BaseResponseModel<ReviewerAssignmentResponseDTO>
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status404NotFound,
                    Message = "Assignment không tồn tại hoặc bạn không có quyền"
                };
            }

            if (assignment.Status != AssignmentStatus.Assigned)
            {
                return new BaseResponseModel<ReviewerAssignmentResponseDTO>
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status400BadRequest,
                    Message = "Assignment không ở trạng thái có thể bắt đầu review"
                };
            }

            assignment.Status = AssignmentStatus.InProgress;
            assignment.StartedAt = DateTime.UtcNow;

            await _unitOfWork.GetRepo<ReviewerAssignment>().UpdateAsync(assignment);
            await _unitOfWork.SaveChangesAsync();

            // Map updated assignment to response DTO
            var response = _mapper.Map<ReviewerAssignmentResponseDTO>(assignment);

            return new BaseResponseModel<ReviewerAssignmentResponseDTO>
            {
                IsSuccess = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Bắt đầu review thành công",
                Data = response
            };
        }
        catch (Exception ex)
        {
            return new BaseResponseModel<ReviewerAssignmentResponseDTO>
            {
                IsSuccess = false,
                StatusCode = StatusCodes.Status500InternalServerError,
                Message = $"Lỗi hệ thống: {ex.Message}"
            };
        }
    }

    public async Task<BaseResponseModel<AssignmentDetailsDTO>> GetAssignmentDetailsAsync(int assignmentId)
    {
        try
        {
            var assignmentOptions = new QueryOptions<ReviewerAssignment>
            {
                Predicate = ra => ra.Id == assignmentId,
                IncludeProperties = new List<Expression<Func<ReviewerAssignment, object>>>
                {
                    ra => ra.Submission,
                    ra => ra.Submission.TopicVersion,
                    ra => ra.Submission.TopicVersion.Topic,
                    ra => ra.Submission.SubmittedByUser,
                    ra => ra.Submission.Phase,
                    ra => ra.Reviewer,
                    ra => ra.Reviews
                },
                Tracked = false
            };

            var assignment = await _unitOfWork.GetRepo<ReviewerAssignment>().GetSingleAsync(assignmentOptions);

            if (assignment == null)
            {
                return new BaseResponseModel<AssignmentDetailsDTO>
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status404NotFound,
                    Message = "Assignment không tồn tại"
                };
            }

            var details = new AssignmentDetailsDTO
            {
                AssignmentId = assignment.Id,
                Status = assignment.Status,
                AssignmentType = assignment.AssignmentType,
                AssignedAt = assignment.AssignedAt,
                Deadline = assignment.Deadline,
                StartedAt = assignment.StartedAt,
                CompletedAt = assignment.CompletedAt,
                SkillMatchScore = assignment.SkillMatchScore,

                // Reviewer Info
                ReviewerId = assignment.ReviewerId,
                ReviewerName = assignment.Reviewer.UserName ?? "Unknown",
                ReviewerEmail = assignment.Reviewer.Email ?? "",

                // Submission Info
                SubmissionId = assignment.SubmissionId,
                SubmissionStatus = assignment.Submission.Status,
                SubmittedAt = (DateTime)assignment.Submission.SubmittedAt,
                DocumentUrl = assignment.Submission.DocumentUrl,
                AdditionalNotes = assignment.Submission.AdditionalNotes,

                // Topic Info
                TopicId = assignment.Submission.TopicVersion.TopicId,
                TopicTitle = assignment.Submission.TopicVersion.Topic.Title,
                TopicDescription = assignment.Submission.TopicVersion.Topic.Description,
                TopicObjectives = assignment.Submission.TopicVersion.Topic.Objectives,

                // Student Info
                StudentId = assignment.Submission.SubmittedBy,
                StudentName = assignment.Submission.SubmittedByUser.UserName ?? "Unknown",
                StudentEmail = assignment.Submission.SubmittedByUser.Email ?? "",

                // Phase Info
                PhaseId = assignment.Submission.PhaseId,
                PhaseName = assignment.Submission.Phase.Name,

                // Review Info
                Reviews = assignment.Reviews.Where(r => r.IsActive).Select(r => new ReviewSummaryDTO
                {
                    ReviewId = r.Id,
                    Status = r.Status,
                    Recommendation = r.Recommendation,
                    OverallScore = r.OverallScore,
                    SubmittedAt = r.SubmittedAt,
                    TimeSpentMinutes = r.TimeSpentMinutes
                }).ToList(),

                IsOverdue = assignment.Deadline.HasValue && assignment.Deadline < DateTime.UtcNow &&
                            assignment.Status != AssignmentStatus.Completed,
                CanStartReview = assignment.Status == AssignmentStatus.Assigned,
                HasActiveReview = assignment.Reviews.Any(r => r.IsActive && r.Status == ReviewStatus.Draft)
            };

            return new BaseResponseModel<AssignmentDetailsDTO>
            {
                IsSuccess = true,
                StatusCode = StatusCodes.Status200OK,
                Data = details
            };
        }
        catch (Exception ex)
        {
            return new BaseResponseModel<AssignmentDetailsDTO>
            {
                IsSuccess = false,
                StatusCode = StatusCodes.Status500InternalServerError,
                Message = $"Lỗi hệ thống: {ex.Message}"
            };
        }
    }
}