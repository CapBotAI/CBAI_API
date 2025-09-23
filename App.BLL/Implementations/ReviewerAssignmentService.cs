using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using App.BLL.Interfaces;
using System.ComponentModel.DataAnnotations;
using App.Commons;
using App.Commons.Interfaces;
using App.Entities.DTOs.Notifications;
using App.Commons.ResponseModel;
using App.DAL.UnitOfWork;
using App.DAL.Queries;
using App.Entities.DTOs.ReviewerAssignment;
using App.Entities.Entities.App;
using App.Entities.Entities.Core;
using App.Entities.Enums;
using App.Entities.Constants;
using System.Linq.Expressions;
using Microsoft.Extensions.Logging;

namespace App.BLL.Implementations;

public class ReviewerAssignmentService : IReviewerAssignmentService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly IPerformanceMatchingService _performanceMatchingService;
    private readonly ISkillMatchingService _skillMatchingService;
    private readonly INotificationService _notificationService;
    private readonly IEmailService _emailService;
    private readonly ILogger<ReviewerAssignmentService> _logger;

    public ReviewerAssignmentService(IUnitOfWork unitOfWork, IMapper mapper,
        IPerformanceMatchingService performanceMatchingService, ISkillMatchingService skillMatchingService,
        INotificationService notificationService, IEmailService emailService,
        ILogger<ReviewerAssignmentService> logger)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _performanceMatchingService = performanceMatchingService;
        _skillMatchingService = skillMatchingService;
        _notificationService = notificationService;
        _emailService = emailService;
        _logger = logger;
    }

    public async Task<BaseResponseModel<ReviewerAssignmentResponseDTO>> AssignReviewerAsync(AssignReviewerDTO dto,
        int assignedById)
    {
        try
        {
            // Validate DTO (important for programmatic calls that bypass controller model validation)
            var validationResults = new List<ValidationResult>();
            var validationContext = new ValidationContext(dto);
            if (!Validator.TryValidateObject(dto, validationContext, validationResults, true))
            {
                var messages = string.Join("; ", validationResults.Select(v => v.ErrorMessage));
                return new BaseResponseModel<ReviewerAssignmentResponseDTO>
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status400BadRequest,
                    Message = messages
                };
            }

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
            string extraNotes = string.Empty;

            // Ensure a system notification row exists for the assigned reviewer. Use NotificationService first; if it fails, fall back to direct DB insert.
            try
            {
                var topicTitle = createdAssignment.Submission?.TopicVersion?.Topic?.EN_Title ?? createdAssignment.Submission?.Topic?.EN_Title ?? "(Không xác định)";
                var title = $"Bạn được phân công review: {topicTitle}";
                var message = $"Bạn vừa được phân công review đề tài '{topicTitle}'. Vui lòng kiểm tra assignment và hoàn thành trước deadline.";

                var notifDto = new App.Entities.DTOs.Notifications.CreateNotificationDTO
                {
                    UserId = createdAssignment.ReviewerId,
                    Title = title,
                    Message = message,
                    Type = App.Entities.Enums.NotificationTypes.Info,
                    RelatedEntityType = "ReviewerAssignment",
                    RelatedEntityId = createdAssignment.Id
                };

                var notifResult = await _notificationService.CreateAsync(notifDto);

                // If notification service failed (or returned null), create the notification directly to ensure DB persistence
                if (notifResult == null || !notifResult.IsSuccess)
                {
                    var repo = _unitOfWork.GetRepo<SystemNotification>();
                    var fallback = new SystemNotification
                    {
                        UserId = notifDto.UserId,
                        Title = notifDto.Title,
                        Message = notifDto.Message,
                        Type = notifDto.Type,
                        RelatedEntityType = notifDto.RelatedEntityType,
                        RelatedEntityId = notifDto.RelatedEntityId,
                        IsRead = false,
                        CreatedAt = DateTime.Now
                    };

                    await repo.CreateAsync(fallback);
                    await _unitOfWork.SaveChangesAsync();
                }

                // Attempt to send email if reviewer has email. If send fails or throws, append a message but don't throw.
                var reviewerEmail = createdAssignment.Reviewer?.Email;
                if (!string.IsNullOrWhiteSpace(reviewerEmail))
                {
                    try
                    {
                        var emailBody = $@"Xin chào {createdAssignment.Reviewer?.UserName ?? "Reviewer"},
\n\nBạn vừa được phân công review đề tài: {topicTitle}.
\nDeadline: {(createdAssignment.Deadline.HasValue ? createdAssignment.Deadline.Value.ToString("dd/MM/yyyy HH:mm") : "Không có")}
\nVui lòng đăng nhập vào hệ thống để xem chi tiết và bắt đầu review.
\n\nTrân trọng,\nCapBot";

                        var emailModel = new EmailModel(
                            new[] { reviewerEmail },
                            $"[CapBot] Phân công review: {topicTitle}",
                            emailBody
                        );

                        var emailSent = await _emailService.SendEmailAsync(emailModel);
                        if (!emailSent)
                        {
                            // append info to response message so client can see email wasn't sent
                            extraNotes += " Notification created but email not sent (EmailService returned false).";
                        }
                    }
                    catch (Exception)
                    {
                        // Email failure should not break assignment; inform caller via message
                        extraNotes += " Notification created but email send threw an exception.";
                    }
                }
            }
            catch
            {
                // Fail silently for notification/email but do not block the main assignment success
            }

            // Update ReviewerPerformance.TotalAssignments for the reviewer and semester (best-effort)
            try
            {
                // Determine semester from the created assignment's submission -> topicVersion -> topic or submission.topic
                var semesterIdNullable = createdAssignment.Submission?.TopicVersion?.Topic?.SemesterId
                                         ?? createdAssignment.Submission?.Topic?.SemesterId;
                if (semesterIdNullable.HasValue)
                {
                    var semesterId = semesterIdNullable.Value;
                    var perfRepo = _unitOfWork.GetRepo<ReviewerPerformance>();
                    var perfOptions = new QueryOptions<ReviewerPerformance>
                    {
                        Predicate = rp => rp.ReviewerId == createdAssignment.ReviewerId && rp.SemesterId == semesterId
                    };

                    var perf = await perfRepo.GetSingleAsync(perfOptions);
                    if (perf == null)
                    {
                        perf = new ReviewerPerformance
                        {
                            ReviewerId = createdAssignment.ReviewerId,
                            SemesterId = semesterId,
                            TotalAssignments = 1,
                            CompletedAssignments = 0,
                            AverageTimeMinutes = 0,
                            AverageScoreGiven = 0,
                            OnTimeRate = 0,
                            QualityRating = 0,
                            LastUpdated = DateTime.UtcNow
                        };
                        await perfRepo.CreateAsync(perf);
                    }
                    else
                    {
                        perf.TotalAssignments += 1;
                        perf.LastUpdated = DateTime.UtcNow;
                        await perfRepo.UpdateAsync(perf);
                    }

                    await _unitOfWork.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update ReviewerPerformance after assignment for reviewer {ReviewerId} assignment {AssignmentId}", createdAssignment.ReviewerId, createdAssignment.Id);
            }

            var baseMessage = "Phân công reviewer thành công" + extraNotes;

            return new BaseResponseModel<ReviewerAssignmentResponseDTO>
            {
                IsSuccess = true,
                StatusCode = StatusCodes.Status201Created,
                Message = baseMessage,
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
            // Get all users with Reviewer role (include useful navigation on the user)
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
            var reviewers = userRoles.Select(ur => ur.User).Where(u => u != null).Distinct().ToList();
            var reviewerIds = reviewers.Where(r => r != null).Select(r => r!.Id).ToList();

            // If no reviewers found, return early
            if (!reviewerIds.Any())
            {
                return new BaseResponseModel<List<AvailableReviewerDTO>>
                {
                    IsSuccess = true,
                    StatusCode = StatusCodes.Status200OK,
                    Message = "Lấy thông tin workload thành công",
                    Data = new List<AvailableReviewerDTO>()
                };
            }

            // Fetch all assignments for these reviewers in a single DB call. If semesterId is provided,
            // push the semester filter into the predicate so the DB does the heavy-lifting.
            QueryOptions<ReviewerAssignment> assignmentOptions;
            if (semesterId.HasValue)
            {
                var sid = semesterId.Value;
                assignmentOptions = new QueryOptions<ReviewerAssignment>
                {
                    Predicate = ra => reviewerIds.Contains(ra.ReviewerId) &&
                                        ((ra.Submission != null && ra.Submission.Topic != null && ra.Submission.Topic.SemesterId == sid) ||
                                         (ra.Submission != null && ra.Submission.TopicVersion != null && ra.Submission.TopicVersion.Topic != null && ra.Submission.TopicVersion.Topic.SemesterId == sid)),
                    IncludeProperties = new List<Expression<Func<ReviewerAssignment, object>>>
                    {
                        ra => ra.Submission!,
                        ra => ra.Submission!.Topic!,
                        ra => ra.Submission!.TopicVersion!,
                        ra => ra.Submission!.TopicVersion!.Topic!
                    },
                };
            }
            else
            {
                assignmentOptions = new QueryOptions<ReviewerAssignment>
                {
                    Predicate = ra => reviewerIds.Contains(ra.ReviewerId),
                    IncludeProperties = new List<Expression<Func<ReviewerAssignment, object>>>
                    {
                        ra => ra.Submission!,
                        ra => ra.Submission!.Topic!,
                        ra => ra.Submission!.TopicVersion!,
                        ra => ra.Submission!.TopicVersion!.Topic!
                    },
                    };
            }

            var assignments = await _unitOfWork.GetRepo<ReviewerAssignment>().GetAllAsync(assignmentOptions);

            // Group assignments by reviewer to compute counts efficiently in-memory
            var assignmentsByReviewer = assignments
                .Where(ra => ra != null)
                .GroupBy(ra => ra.ReviewerId)
                .ToDictionary(g => g.Key, g => g.ToList());

            // Load reviewer performances for these reviewers (filtered by semester when provided)
            var perfOptions = new QueryOptions<ReviewerPerformance>
            {
                Predicate = rp => reviewerIds.Contains(rp.ReviewerId) && (!semesterId.HasValue || rp.SemesterId == semesterId.Value)
            };
            var performances = await _unitOfWork.GetRepo<ReviewerPerformance>().GetAllAsync(perfOptions);

            // Build a lookup: if semester provided, prefer that semester's perf; otherwise pick the latest by LastUpdated or SemesterId
            Dictionary<int, ReviewerPerformance?> performanceByReviewer;
            if (semesterId.HasValue)
            {
                performanceByReviewer = performances
                    .GroupBy(p => p.ReviewerId)
                    .ToDictionary(g => g.Key, g => g.OrderByDescending(p => p.LastUpdated).FirstOrDefault());
            }
            else
            {
                performanceByReviewer = performances
                    .GroupBy(p => p.ReviewerId)
                    .ToDictionary(g => g.Key, g => g.OrderByDescending(p => p.SemesterId).FirstOrDefault());
            }

            var workloadInfo = new List<AvailableReviewerDTO>();
            foreach (var reviewer in reviewers)
            {
                if (reviewer == null) continue;
                assignmentsByReviewer.TryGetValue(reviewer.Id, out var revAssignments);
                revAssignments ??= new List<ReviewerAssignment>();

                var activeAssignments = revAssignments.Count(ra => ra.Status == AssignmentStatus.Assigned || ra.Status == AssignmentStatus.InProgress);
                //var completedAssignments = revAssignments.Count(ra => ra.Status == AssignmentStatus.Completed);

                performanceByReviewer.TryGetValue(reviewer.Id, out var perf);

                // Fallback to any loaded in-memory performance if DB lookup returned nothing
                perf ??= reviewer.ReviewerPerformances?.OrderByDescending(rp => rp.LastUpdated).FirstOrDefault();

                var workload = new AvailableReviewerDTO
                {
                    Id = reviewer.Id,
                    UserName = reviewer.UserName,
                    Email = reviewer.Email,
                    PhoneNumber = reviewer.PhoneNumber,
                    CurrentAssignments = activeAssignments,
                    CompletedAssignments = perf?.CompletedAssignments ?? 0,
                    AverageScoreGiven = perf?.AverageScoreGiven,
                    OnTimeRate = perf?.OnTimeRate,
                    QualityRating = perf?.QualityRating,
                    Skills = reviewer.LecturerSkills?.Select(ls => ls.SkillTag).ToList() ?? new List<string>(),
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
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get reviewers workload");
            return new BaseResponseModel<List<AvailableReviewerDTO>>
            {
                IsSuccess = false,
                StatusCode = StatusCodes.Status500InternalServerError,
                Message = $"Lỗi hệ thống: {ex.Message}"
            };
        }
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
                TopicTitle = submission.TopicVersion.Topic.EN_Title,
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
            // Get topic skill tags (ensure non-null)
            var topicSkillTags = await _skillMatchingService.ExtractTopicSkillTagsAsync(submissionId) ?? new List<string>();

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
                ReviewerName = reviewer.UserName ?? "Unknown",
                ReviewerEmail = reviewer.Email ?? string.Empty
            };

            // Calculate scores
            // Calculate scores (wrap external service calls defensively)
            try
            {
                matchingResult.SkillMatchScore = await _skillMatchingService.CalculateSkillMatchScoreAsync(reviewerId, topicSkillTags);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Skill match calculation failed for reviewer {ReviewerId} submission {SubmissionId}", reviewerId, submissionId);
                matchingResult.SkillMatchScore = 0;
            }

            try
            {
                matchingResult.PerformanceScore = await _skillMatchingService.CalculatePerformanceScoreAsync(reviewerId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Performance score calculation failed for reviewer {ReviewerId}", reviewerId);
                matchingResult.PerformanceScore = 0;
            }

            try
            {
                matchingResult.WorkloadScore = await _skillMatchingService.CalculateWorkloadScoreAsync(reviewerId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Workload score calculation failed for reviewer {ReviewerId}", reviewerId);
                matchingResult.WorkloadScore = 0;
            }

            // Get additional info (guard null collections)
            matchingResult.ReviewerSkills = reviewer.LecturerSkills?
                .Where(ls => !string.IsNullOrEmpty(ls.SkillTag))
                .ToDictionary(ls => ls.SkillTag!, ls => ls.ProficiencyLevel) ?? new Dictionary<string, App.Entities.Enums.ProficiencyLevels>();

            var performance = reviewer.ReviewerPerformances?.FirstOrDefault();
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
        catch (Exception ex)
        {
            _logger.LogError(ex, "AnalyzeReviewerMatchAsync failed for reviewer {ReviewerId} submission {SubmissionId}", reviewerId, submissionId);
            return new BaseResponseModel<ReviewerMatchingResult>
            {
                IsSuccess = false,
                StatusCode = StatusCodes.Status500InternalServerError,
                Message = $"Lỗi hệ thống: {ex.Message}"
            };
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
                    TopicTitle = a.Submission.TopicVersion.Topic.EN_Title,
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
            // Load assignment as a tracked entity but DO NOT include navigation props here.
            // We'll update the tracked entity's simple properties and SaveChanges so EF only writes changed fields.
            var assignmentOptions = new QueryOptions<ReviewerAssignment>
            {
                Predicate = ra => ra.Id == assignmentId && ra.ReviewerId == reviewerId,
                Tracked = true
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

            // Update the tracked entity's scalar properties. This avoids attaching navigation properties
            // and prevents unintended overwrites of other columns.
            assignment.Status = AssignmentStatus.InProgress;
            if (assignment.StartedAt == null)
            {
                assignment.StartedAt = DateTime.Now;
            }

            // Persist changes on the tracked entity
            await _unitOfWork.GetRepo<ReviewerAssignment>().UpdateAsync(assignment);
            await _unitOfWork.SaveChangesAsync();

            // Reload full assignment for response mapping (no-tracking to keep change tracker clean)
            var updatedAssignmentOptions = new QueryOptions<ReviewerAssignment>
            {
                Predicate = ra => ra.Id == assignmentId,
                IncludeProperties = new List<Expression<Func<ReviewerAssignment, object>>>
                {
                    ra => ra.Submission,
                    ra => ra.Submission.TopicVersion,
                    ra => ra.Submission.TopicVersion.Topic,
                    ra => ra.Submission.SubmittedByUser,
                    ra => ra.Reviewer,
                    ra => ra.AssignedByUser
                },
                Tracked = false
            };

            var updatedAssignment = await _unitOfWork.GetRepo<ReviewerAssignment>().GetSingleAsync(updatedAssignmentOptions);
            var response = _mapper.Map<ReviewerAssignmentResponseDTO>(updatedAssignment);

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
                    // ensure Topic navigation is loaded directly when available
                    ra => ra.Submission.Topic,
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

            // Defensive: guard against null navigation properties and provide safe defaults
            var submission = assignment.Submission;
            var topicVersion = submission?.TopicVersion;
            // Prefer submission.Topic if it's loaded; otherwise fall back to TopicVersion.Topic
            var topic = submission?.Topic ?? topicVersion?.Topic;
            var submittedByUser = submission?.SubmittedByUser;
            var phase = submission?.Phase;
            var reviews = assignment.Reviews ?? new List<Review>();

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
                ReviewerName = assignment.Reviewer?.UserName ?? "Unknown",
                ReviewerEmail = assignment.Reviewer?.Email ?? string.Empty,

                // Submission Info
                SubmissionId = assignment.SubmissionId,
                SubmissionStatus = submission?.Status ?? default,
                SubmittedAt = submission?.SubmittedAt,
                DocumentUrl = submission?.DocumentUrl,
                AdditionalNotes = submission?.AdditionalNotes,

                // Topic Info (safe fallbacks)
                TopicId = topic?.Id ?? topicVersion?.TopicId ?? 0,
                TopicTitle = topic?.EN_Title ?? submission?.Topic?.EN_Title ?? "(Không xác định)",
                TopicDescription = topic?.Description ?? string.Empty,
                TopicObjectives = topic?.Objectives ?? string.Empty,

                // Student Info
                StudentId = submission?.SubmittedBy ?? 0,
                StudentName = submittedByUser?.UserName ?? "Unknown",
                StudentEmail = submittedByUser?.Email ?? string.Empty,

                // Phase Info
                PhaseId = submission?.PhaseId ?? 0,
                PhaseName = phase?.Name ?? string.Empty,

                // Review Info (guard nulls)
                Reviews = reviews.Where(r => r != null && r.IsActive).Select(r => new ReviewSummaryDTO
                {
                    ReviewId = r.Id,
                    Status = r.Status,
                    Recommendation = r.Recommendation,
                    OverallScore = r.OverallScore,
                    SubmittedAt = r.SubmittedAt,
                    TimeSpentMinutes = r.TimeSpentMinutes
                }).ToList(),

                IsOverdue = assignment.Deadline.HasValue && assignment.Deadline < DateTime.Now &&
                            assignment.Status != AssignmentStatus.Completed,
                CanStartReview = assignment.Status == AssignmentStatus.Assigned,
                HasActiveReview = reviews.Any(r => r != null && r.IsActive && r.Status == ReviewStatus.Draft)
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