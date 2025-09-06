using System;
using System.Linq.Expressions;
using App.BLL.Interfaces;
using App.Commons.Extensions;
using App.Commons.Paging;
using App.Commons.ResponseModel;
using App.DAL.Interfaces;
using App.DAL.Queries.Implementations;
using App.DAL.UnitOfWork;
using App.Entities.Constants;
using App.Entities.DTOs.Notifications;
using App.Entities.DTOs.Submissions;
using App.Entities.Entities.App;
using App.Entities.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace App.BLL.Implementations;

public class SubmissionService : ISubmissionService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IIdentityRepository _identityRepository;
    private readonly INotificationService _notificationService;
    private readonly IAIService _aIService;
    private readonly IElasticsearchService _elasticsearchService;


    public SubmissionService(IUnitOfWork unitOfWork,
     IIdentityRepository identityRepository,
     INotificationService notificationService,
     IAIService aIService,
     IElasticsearchService elasticsearchService)
    {
        _unitOfWork = unitOfWork;
        _identityRepository = identityRepository;
        _notificationService = notificationService;
        _aIService = aIService;
        _elasticsearchService = elasticsearchService;

    }

    public async Task<BaseResponseModel<SubmissionDetailDTO>> CreateSubmission(CreateSubmissionDTO dto, int userId)
    {
        try
        {
            var user = await _identityRepository.GetByIdAsync((long)userId);
            if (user == null)
            {
                return new BaseResponseModel<SubmissionDetailDTO>
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status404NotFound,
                    Message = "Người dùng không tồn tại"
                };
            }

            var topicRepo = _unitOfWork.GetRepo<Topic>();
            var topic = await topicRepo.GetSingleAsync(new QueryBuilder<Topic>()
                .WithPredicate(x => x.Id == dto.TopicId && x.IsActive && x.DeletedAt == null)
                .WithTracking(true)
                .Build());

            if (topic == null)
            {
                return new BaseResponseModel<SubmissionDetailDTO>
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status404NotFound,
                    Message = "Chủ đề không tồn tại"
                };
            }

            var phaseRepo = _unitOfWork.GetRepo<Phase>();
            var phase = await phaseRepo.GetSingleAsync(new QueryBuilder<Phase>()
                .WithPredicate(x => x.Id == dto.PhaseId && x.IsActive && x.DeletedAt == null)
                .WithTracking(true)
                .Build());

            if (phase == null)
            {
                return new BaseResponseModel<SubmissionDetailDTO>
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status404NotFound,
                    Message = "Giai đoạn không tồn tại"
                };
            }

            // Check file ownership nếu có FileId
            if (dto.FileId.HasValue)
            {
                var fileRepo = _unitOfWork.GetRepo<AppFile>();
                var file = await fileRepo.GetSingleAsync(new QueryBuilder<AppFile>()
                    .WithPredicate(x => x.Id == dto.FileId.Value && x.CreatedBy == user.UserName && x.IsActive && x.DeletedAt == null)
                    .WithTracking(false)
                    .Build());

                if (file == null)
                {
                    return new BaseResponseModel<SubmissionDetailDTO>
                    {
                        IsSuccess = false,
                        StatusCode = StatusCodes.Status409Conflict,
                        Message = "File không tồn tại hoặc không phải của bạn"
                    };
                }
            }

            await _unitOfWork.BeginTransactionAsync();

            var submissionRepo = _unitOfWork.GetRepo<Submission>();
            var entityFileRepo = _unitOfWork.GetRepo<EntityFile>();

            var submission = dto.GetEntity();
            submission.SubmittedBy = userId;
            submission.IsActive = true;
            submission.DeletedAt = null;
            submission.Status = SubmissionStatus.Pending;
            submission.CreatedAt = DateTime.Now;
            submission.CreatedBy = user.UserName;

            await submissionRepo.CreateAsync(submission);
            await _unitOfWork.SaveChangesAsync();

            if (dto.FileId.HasValue)
            {
                await entityFileRepo.CreateAsync(new EntityFile
                {
                    EntityId = submission.Id,
                    EntityType = EntityType.Submission,
                    FileId = dto.FileId.Value,
                    IsPrimary = true,
                    Caption = $"Submission #{submission.Id}",
                    CreatedAt = DateTime.Now,
                });
            }

            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitTransactionAsync();

            var entityFileRepo_2 = _unitOfWork.GetRepo<EntityFile>();
            var entityFile = await entityFileRepo_2.GetSingleAsync(new QueryBuilder<EntityFile>()
                .WithPredicate(x => x.EntityId == submission.Id && x.EntityType == EntityType.Submission && x.IsPrimary)
                .WithInclude(x => x.File!)
                .WithTracking(false)
                .Build());

            return new BaseResponseModel<SubmissionDetailDTO>
            {
                Data = new SubmissionDetailDTO(submission, entityFile),
                IsSuccess = true,
                StatusCode = StatusCodes.Status201Created,
                Message = "Tạo submission thành công"
            };
        }
        catch (Exception)
        {
            await _unitOfWork.RollBackAsync();
            throw;
        }
    }
    public async Task<BaseResponseModel<SubmissionDetailDTO>> UpdateSubmission(UpdateSubmissionDTO dto, int userId)
    {
        try
        {
            var user = await _identityRepository.GetByIdAsync((long)userId);
            if (user == null)
            {
                return new BaseResponseModel<SubmissionDetailDTO>
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status404NotFound,
                    Message = "Người dùng không tồn tại"
                };
            }

            var submissionRepo = _unitOfWork.GetRepo<Submission>();
            var submission = await submissionRepo.GetSingleAsync(new QueryBuilder<Submission>()
                .WithPredicate(x => x.Id == dto.Id && x.IsActive && x.DeletedAt == null)
                .WithInclude(x => x.Phase)
                .WithInclude(x => x.Topic)
                .WithInclude(x => x.TopicVersion)
                .WithTracking(true)
                .Build());

            if (submission == null)
            {
                return new BaseResponseModel<SubmissionDetailDTO>
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status404NotFound,
                    Message = "Submission không tồn tại"
                };
            }

            if (submission.SubmittedBy != userId)
            {
                return new BaseResponseModel<SubmissionDetailDTO>
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status403Forbidden,
                    Message = "Bạn không có quyền cập nhật submission này"
                };
            }

            if (submission.Status != SubmissionStatus.Pending && submission.Status != SubmissionStatus.RevisionRequired)
            {
                return new BaseResponseModel<SubmissionDetailDTO>
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status400BadRequest,
                    Message = "Chỉ được cập nhật khi submission đang ở trạng thái Pending hoặc RevisionRequired"
                };
            }

            // Validate file nếu có
            if (dto.FileId.HasValue)
            {
                var fileRepo = _unitOfWork.GetRepo<AppFile>();
                var file = await fileRepo.GetSingleAsync(new QueryBuilder<AppFile>()
                    .WithPredicate(x => x.Id == dto.FileId.Value && x.IsActive && x.DeletedAt == null)
                    .WithTracking(false)
                    .Build());

                if (file == null)
                {
                    return new BaseResponseModel<SubmissionDetailDTO>
                    {
                        IsSuccess = false,
                        StatusCode = StatusCodes.Status409Conflict,
                        Message = "File không tồn tại"
                    };
                }

                if (file.CreatedBy != user.UserName)
                {
                    return new BaseResponseModel<SubmissionDetailDTO>
                    {
                        IsSuccess = false,
                        StatusCode = StatusCodes.Status409Conflict,
                        Message = "File không phải của bạn"
                    };
                }
            }

            await _unitOfWork.BeginTransactionAsync();

            if (submission.PhaseId != dto.PhaseId)
            {
                var phaseRepo = _unitOfWork.GetRepo<Phase>();
                var phase = await phaseRepo.GetSingleAsync(new QueryBuilder<Phase>()
                    .WithPredicate(x => x.Id == dto.PhaseId && x.IsActive && x.DeletedAt == null)
                    .WithInclude(x => x.Semester)
                    .WithTracking(false)
                    .Build());

                if (phase == null)
                {
                    await _unitOfWork.RollBackAsync();
                    return new BaseResponseModel<SubmissionDetailDTO>
                    {
                        IsSuccess = false,
                        StatusCode = StatusCodes.Status404NotFound,
                        Message = "Giai đoạn không tồn tại"
                    };
                }

                var topicSemesterId = submission.Topic?.SemesterId;
                if (!topicSemesterId.HasValue || topicSemesterId.Value != phase.SemesterId)
                {
                    await _unitOfWork.RollBackAsync();
                    return new BaseResponseModel<SubmissionDetailDTO>
                    {
                        IsSuccess = false,
                        StatusCode = StatusCodes.Status409Conflict,
                        Message = "Giai đoạn không thuộc cùng học kỳ với chủ đề"
                    };
                }

                submission.PhaseId = dto.PhaseId;
            }

            submission.DocumentUrl = dto.DocumentUrl?.Trim();
            submission.AdditionalNotes = dto.AdditionalNotes?.Trim();

            await submissionRepo.UpdateAsync(submission);

            // Gắn/đổi file chính
            if (dto.FileId.HasValue)
            {
                var entityFileRepo = _unitOfWork.GetRepo<EntityFile>();
                var existed = await entityFileRepo.GetSingleAsync(new QueryBuilder<EntityFile>()
                    .WithPredicate(x => x.EntityId == submission.Id && x.EntityType == EntityType.Submission && x.IsPrimary)
                    .WithTracking(false)
                    .Build());

                if (existed != null)
                {
                    existed.FileId = dto.FileId.Value;
                    existed.Caption = $"Submission #{submission.Id}";
                    existed.CreatedAt = DateTime.Now;
                    await entityFileRepo.UpdateAsync(existed);
                }
                else
                {
                    await entityFileRepo.CreateAsync(new EntityFile
                    {
                        EntityId = submission.Id,
                        EntityType = EntityType.Submission,
                        FileId = dto.FileId.Value,
                        IsPrimary = true,
                        Caption = $"Submission #{submission.Id}",
                        CreatedAt = DateTime.Now,
                    });
                }
            }

            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitTransactionAsync();

            var entityFileRepo_2 = _unitOfWork.GetRepo<EntityFile>();
            var entityFile = await entityFileRepo_2.GetSingleAsync(new QueryBuilder<EntityFile>()
                .WithPredicate(x => x.EntityId == submission.Id && x.EntityType == EntityType.Submission && x.IsPrimary)
                .WithInclude(x => x.File!)
                .WithTracking(false)
                .Build());

            return new BaseResponseModel<SubmissionDetailDTO>
            {
                Data = new SubmissionDetailDTO(submission, entityFile),
                IsSuccess = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Cập nhật submission thành công"
            };
        }
        catch (Exception)
        {
            await _unitOfWork.RollBackAsync();
            throw;
        }
    }

    /// <summary>
    ///Submit submission
    /// </summary>
    /// <param name="dto"></param>
    /// <param name="userId"></param>
    /// <returns></returns>
    public async Task<BaseResponseModel> SubmitSubmission(SubmitSubmissionDTO dto, int userId)
    {
        try
        {
            var user = await _identityRepository.GetByIdAsync((long)userId);
            if (user == null)
            {
                return new BaseResponseModel
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status404NotFound,
                    Message = "Người dùng không tồn tại"
                };
            }

            var submissionRepo = _unitOfWork.GetRepo<Submission>();
            // Đọc ngoài transaction bằng NoTracking
            var submission = await submissionRepo.GetSingleAsync(new QueryBuilder<Submission>()
                .WithPredicate(x => x.Id == dto.Id && x.IsActive && x.DeletedAt == null)
                .WithInclude(x => x.Phase)
                .WithInclude(x => x.TopicVersion)
                .WithInclude(x => x.Topic)
                .WithTracking(false)
                .Build());

            if (submission == null)
            {
                return new BaseResponseModel
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status404NotFound,
                    Message = "Submission không tồn tại"
                };
            }

            if (submission.SubmittedBy != userId)
            {
                return new BaseResponseModel
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status403Forbidden,
                    Message = "Bạn không có quyền submit submission này"
                };
            }

            if (submission.Status != SubmissionStatus.Pending)
            {
                return new BaseResponseModel
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status400BadRequest,
                    Message = "Chỉ được submit khi submission đang ở trạng thái Pending"
                };
            }

            // Check deadline
            if (submission.Phase.SubmissionDeadline.HasValue &&
                DateTime.Now > submission.Phase.SubmissionDeadline.Value)
            {
                return new BaseResponseModel
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status400BadRequest,
                    Message = "Đã quá hạn nộp cho giai đoạn này"
                };
            }

            // ===== AI duplicate check (Gemini + Elasticsearch) ngoài transaction =====
            var title = submission.TopicVersion?.Title ?? submission.Topic.Title;
            var description = submission.TopicVersion?.Description ?? submission.Topic.Description;
            var keywords = await _aIService.GenerateKeywordsAsync(title, description);

            bool hasDuplicate = false;
            string? aiDetails = null;

            if (keywords.Count > 0)
            {
                var query = string.Join(" ", keywords.Distinct());
                var searchRes = await _elasticsearchService.SearchTopicsAsync(query, size: 10);
                if (searchRes.IsSuccess && searchRes.Data != null)
                {
                    var duplicates = searchRes.Data.Where(d => d.Id != submission.TopicId).Take(5).ToList();
                    if (duplicates.Count > 0)
                    {
                        hasDuplicate = true;
                        aiDetails = $"Found {duplicates.Count} similar topics by AI keywords: {string.Join("; ", duplicates.Select(d => $"{d.Id}:{d.Title}"))}";
                    }
                }
            }
            // ===== End AI duplicate check =====

            await _unitOfWork.BeginTransactionAsync();

            // Reload với tracking trong transaction
            var submissionRepoT = _unitOfWork.GetRepo<Submission>();
            var versionRepoT = _unitOfWork.GetRepo<TopicVersion>();

            var submissionT = await submissionRepoT.GetSingleAsync(new QueryBuilder<Submission>()
                .WithPredicate(x => x.Id == dto.Id && x.IsActive && x.DeletedAt == null)
                .WithInclude(x => x.TopicVersion)
                .WithTracking(true)
                .Build());

            if (submissionT == null)
            {
                await _unitOfWork.RollBackAsync();
                return new BaseResponseModel { IsSuccess = false, StatusCode = StatusCodes.Status404NotFound, Message = "Submission không tồn tại" };
            }

            if (hasDuplicate)
            {
                submissionT.AiCheckStatus = AiCheckStatus.Failed;
                submissionT.AiCheckScore = null;
                submissionT.AiCheckDetails = aiDetails;
                await submissionRepoT.UpdateAsync(submissionT);
                await _unitOfWork.SaveChangesAsync();
                await _unitOfWork.CommitTransactionAsync();

                return new BaseResponseModel
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status409Conflict,
                    Message = "Phát hiện đề tài tương tự. Vui lòng xem lại hoặc điều chỉnh đề tài."
                };
            }

            // Pass: cập nhật AI và chuyển trạng thái trong cùng transaction
            submissionT.AiCheckStatus = AiCheckStatus.Passed;
            submissionT.AiCheckDetails = keywords.Count > 0 ? $"AI keywords: {string.Join(", ", keywords)}" : "AI skipped/no keywords";
            submissionT.Status = SubmissionStatus.UnderReview;
            submissionT.SubmittedAt = DateTime.Now;
            await submissionRepoT.UpdateAsync(submissionT);

            if (submissionT.TopicVersionId.HasValue && submissionT.TopicVersion != null)
            {
                submissionT.TopicVersion.Status = TopicStatus.Submitted;
                await versionRepoT.UpdateAsync(submissionT.TopicVersion);
            }

            var moderators = await _identityRepository.GetUsersInRoleAsync(SystemRoleConstants.Moderator);
            var moderatorIds = moderators.Select(x => (int)x.Id).Distinct().ToList();
            if (moderatorIds.Count > 0)
            {
                var createBulkNotification = await _notificationService.CreateBulkAsync(new CreateBulkNotificationsDTO
                {
                    UserIds = moderatorIds,
                    Title = "Thông báo về submission mới",
                    Message = $"Submission #{submissionT.Id} đã được submit với chủ đề {submissionT.Topic.Title}",
                    Type = NotificationTypes.Info,
                    RelatedEntityType = EntityType.Submission.ToString(),
                    RelatedEntityId = submissionT.Id
                });

                if (!createBulkNotification.IsSuccess)
                {
                    throw new Exception(createBulkNotification.Message);
                }
            }

            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitTransactionAsync();

            return new BaseResponseModel
            {
                IsSuccess = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Submit submission thành công"
            };
        }
        catch (Exception)
        {
            await _unitOfWork.RollBackAsync();
            throw;
        }
    }

    public async Task<BaseResponseModel> ResubmitSubmission(ResubmitSubmissionDTO dto, int userId)
    {
        try
        {
            var user = await _identityRepository.GetByIdAsync((long)userId);
            if (user == null)
            {
                return new BaseResponseModel
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status404NotFound,
                    Message = "Người dùng không tồn tại"
                };
            }

            var submissionRepo = _unitOfWork.GetRepo<Submission>();
            var submission = await submissionRepo.GetSingleAsync(new QueryBuilder<Submission>()
                .WithPredicate(x => x.Id == dto.Id && x.IsActive && x.DeletedAt == null)
                .WithInclude(x => x.Phase)
                .WithTracking(false)
                .Build());

            if (submission == null)
            {
                return new BaseResponseModel
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status404NotFound,
                    Message = "Submission không tồn tại"
                };
            }

            if (submission.SubmittedBy != userId)
            {
                return new BaseResponseModel
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status403Forbidden,
                    Message = "Bạn không có quyền resubmit submission này"
                };
            }

            if (submission.Status != SubmissionStatus.RevisionRequired)
            {
                return new BaseResponseModel
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status400BadRequest,
                    Message = "Chỉ được resubmit khi submission đang ở trạng thái RevisionRequired"
                };
            }

            // Check deadline
            if (submission.Phase.SubmissionDeadline.HasValue &&
                DateTime.Now > submission.Phase.SubmissionDeadline.Value)
            {
                return new BaseResponseModel
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status400BadRequest,
                    Message = "Đã quá hạn nộp lại cho giai đoạn này"
                };
            }

            var versionRepo = _unitOfWork.GetRepo<TopicVersion>();
            var topicVersion = await versionRepo.GetSingleAsync(new QueryBuilder<TopicVersion>()
                .WithPredicate(x => x.Id == dto.TopicVersionId && x.IsActive && x.DeletedAt == null)
                .WithInclude(x => x.Topic)
                .WithTracking(false)
                .Build());

            if (topicVersion == null)
            {
                return new BaseResponseModel
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status404NotFound,
                    Message = "Phiên bản chủ đề không tồn tại"
                };
            }

            if (topicVersion.Status != TopicStatus.Draft)
            {
                return new BaseResponseModel
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status400BadRequest,
                    Message = "TopicVersion phải ở trạng thái Draft trước khi resubmit"
                };
            }

            if (topicVersion.TopicId != submission.TopicId)
            {
                return new BaseResponseModel
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status409Conflict,
                    Message = "TopicVersion không thuộc cùng Topic với Submission."
                };
            }

            //? ===== AI duplicate check (Gemini + Elasticsearch) =====
            //* (1) Đọc dữ liệu cần thiết ngoài transaction (nên dùng NoTracking) và gọi AI
            var title = topicVersion.Title ?? submission.Topic.Title;
            var description = topicVersion.Description ?? submission.Topic.Description;
            var keywords = await _aIService.GenerateKeywordsAsync(title, description);

            bool hasDuplicate = false;
            string? aiDetails = null;

            if (keywords.Count > 0)
            {
                var query = string.Join(" ", keywords.Distinct());
                var searchRes = await _elasticsearchService.SearchTopicsAsync(query, size: 10);
                if (searchRes.IsSuccess && searchRes.Data != null)
                {
                    var duplicates = searchRes.Data.Where(d => d.Id != submission.TopicId).Take(5).ToList();
                    if (duplicates.Count > 0)
                    {
                        hasDuplicate = true;
                        aiDetails = $"Found {duplicates.Count} similar topics by AI keywords: {string.Join("; ", duplicates.Select(d => $"{d.Id}:{d.Title}"))}";
                    }
                }
            }
            //? ===== End AI duplicate check =====

            await _unitOfWork.BeginTransactionAsync();

            //* (2) Reload entity với tracking trong transaction
            var submissionRepoT = _unitOfWork.GetRepo<Submission>();
            var versionRepoT = _unitOfWork.GetRepo<TopicVersion>();

            var submissionT = await submissionRepoT.GetSingleAsync(new QueryBuilder<Submission>()
                .WithPredicate(x => x.Id == dto.Id && x.IsActive && x.DeletedAt == null)
                .WithInclude(x => x.TopicVersion)
                .WithTracking(true)
                .Build());

            if (submissionT == null)
            {
                await _unitOfWork.RollBackAsync();
                return new BaseResponseModel { IsSuccess = false, StatusCode = StatusCodes.Status404NotFound, Message = "Submission không tồn tại" };
            }

            //* (3) Ghi theo nhánh
            if (hasDuplicate)
            {
                submissionT.AiCheckStatus = AiCheckStatus.Failed;
                submissionT.AiCheckScore = null;
                submissionT.AiCheckDetails = aiDetails;
                await submissionRepoT.UpdateAsync(submissionT);
                await _unitOfWork.SaveChangesAsync();
                await _unitOfWork.CommitTransactionAsync();

                return new BaseResponseModel
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status409Conflict,
                    Message = "Phát hiện đề tài tương tự. Vui lòng xem lại hoặc điều chỉnh đề tài."
                };
            }

            //* Pass: cập nhật AI và tiến hành đổi trạng thái
            submissionT.AiCheckStatus = AiCheckStatus.Passed;
            submissionT.AiCheckDetails = keywords.Count > 0 ? $"AI keywords: {string.Join(", ", keywords)}" : "AI skipped/no keywords";
            submissionT.Status = SubmissionStatus.UnderReview;
            submissionT.SubmittedAt = DateTime.Now;
            submissionT.SubmissionRound += 1;
            submissionT.TopicVersionId = topicVersion.Id;
            await submissionRepoT.UpdateAsync(submissionT);

            var topicVersionT = await versionRepoT.GetSingleAsync(new QueryBuilder<TopicVersion>()
                .WithPredicate(x => x.Id == topicVersion.Id && x.IsActive && x.DeletedAt == null)
                .WithTracking(true)
                .Build());
            topicVersionT.Status = TopicStatus.Submitted;
            await versionRepoT.UpdateAsync(topicVersionT);

            var moderators = await _identityRepository.GetUsersInRoleAsync(SystemRoleConstants.Moderator);
            var moderatorIds = moderators.Select(x => (int)x.Id).Distinct().ToList();
            if (moderatorIds.Count > 0)
            {
                var createBulkNotification = await _notificationService.CreateBulkAsync(new CreateBulkNotificationsDTO
                {
                    UserIds = moderatorIds,
                    Title = "Thông báo về submission mới",
                    Message = $"Submission #{submission.Id} đã được resubmit với đề tài {submission.Topic.Title} và phiên bản đề tài {topicVersion.Title}",
                    Type = NotificationTypes.Info,
                    RelatedEntityType = EntityType.Submission.ToString(),
                    RelatedEntityId = submission.Id
                });

                if (!createBulkNotification.IsSuccess)
                {
                    throw new Exception(createBulkNotification.Message);
                }
            }

            var reviewerIds = submission.ReviewerAssignments.Select(x => x.ReviewerId).Distinct().ToList();
            if (reviewerIds.Count > 0)
            {
                var createBulkNotification = await _notificationService.CreateBulkAsync(new CreateBulkNotificationsDTO
                {
                    UserIds = reviewerIds,
                    Title = "Thông báo về submission mới",
                    Message = $"Submission #{submission.Id} đã được resubmit với đề tài {submission.Topic.Title} và phiên bản đề tài {topicVersion.Title}",
                    Type = NotificationTypes.Info,
                    RelatedEntityType = EntityType.Submission.ToString(),
                    RelatedEntityId = submission.Id
                });

                if (!createBulkNotification.IsSuccess)
                {
                    throw new Exception(createBulkNotification.Message);
                }
            }

            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitTransactionAsync();

            return new BaseResponseModel
            {
                IsSuccess = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Resubmit submission thành công"
            };
        }
        catch (Exception)
        {
            await _unitOfWork.RollBackAsync();
            throw;
        }
    }

    public async Task<BaseResponseModel<SubmissionDetailDTO>> GetSubmissionDetail(int id)
    {
        try
        {
            var submissionRepo = _unitOfWork.GetRepo<Submission>();
            var submission = await submissionRepo.GetSingleAsync(new QueryBuilder<Submission>()
                .WithPredicate(x => x.Id == id && x.IsActive && x.DeletedAt == null)
                .WithInclude(x => x.TopicVersion)
                .WithInclude(x => x.Topic)
                .WithInclude(x => x.Phase)
                .WithInclude(x => x.SubmittedByUser)
                .WithTracking(false)
                .Build());

            if (submission == null)
            {
                return new BaseResponseModel<SubmissionDetailDTO>
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status404NotFound,
                    Message = "Submission không tồn tại"
                };
            }

            // Lấy file chính (nếu có)
            var entityFileRepo = _unitOfWork.GetRepo<EntityFile>();
            var entityFile = await entityFileRepo.GetSingleAsync(new QueryBuilder<EntityFile>()
                .WithPredicate(x => x.EntityId == id && x.EntityType == EntityType.Submission && x.IsPrimary)
                .WithInclude(x => x.File!)
                .WithTracking(false)
                .Build());

            return new BaseResponseModel<SubmissionDetailDTO>
            {
                Data = new SubmissionDetailDTO(submission, entityFile),
                IsSuccess = true,
                StatusCode = StatusCodes.Status200OK
            };
        }
        catch (Exception)
        {
            throw;
        }
    }

    public async Task<BaseResponseModel<PagingDataModel<SubmissionOverviewResDTO, GetSubmissionsQueryDTO>>> GetSubmissions(GetSubmissionsQueryDTO query)
    {
        try
        {
            var submissionRepo = _unitOfWork.GetRepo<Submission>();

            Expression<Func<Submission, bool>> predicate = x => true;

            if (query.TopicVersionId.HasValue)
            {
                var vId = query.TopicVersionId.Value;
                predicate = predicate.AndAlso(x => x.TopicVersionId == vId);
            }

            if (query.PhaseId.HasValue)
            {
                var pId = query.PhaseId.Value;
                predicate = predicate.AndAlso(x => x.PhaseId == pId);
            }

            if (query.Status.HasValue)
            {
                var st = query.Status.Value;
                predicate = predicate.AndAlso(x => x.Status == st);
            }

            var qb = new QueryBuilder<Submission>()
                .WithPredicate(predicate)
                .WithInclude(x => x.SubmittedByUser)
                 .WithInclude(x => x.Topic)
                .WithTracking(false);

            // Lọc theo SemesterId qua join Phase.SemesterId nếu có
            if (query.SemesterId.HasValue)
            {
                qb.WithInclude(x => x.Phase);
            }

            var baseQuery = submissionRepo.Get(qb.Build());

            if (query.SemesterId.HasValue)
            {
                var semId = query.SemesterId.Value;
                baseQuery = baseQuery.Where(x => x.Phase.SemesterId == semId);
            }

            query.TotalRecord = await baseQuery.CountAsync();

            var items = await baseQuery
                .OrderByDescending(x => x.SubmittedAt)
                .ToPagedList(query.PageNumber, query.PageSize)
                .ToListAsync();

            return new BaseResponseModel<PagingDataModel<SubmissionOverviewResDTO, GetSubmissionsQueryDTO>>
            {
                Data = new PagingDataModel<SubmissionOverviewResDTO, GetSubmissionsQueryDTO>(
                    items.Select(x => new SubmissionOverviewResDTO(x)).ToList(), query),
                IsSuccess = true,
                StatusCode = StatusCodes.Status200OK
            };
        }
        catch (Exception)
        {
            throw;
        }
    }

    public async Task<BaseResponseModel> DeleteSubmission(int id, int userId, bool isAdmin)
    {
        try
        {
            var user = await _identityRepository.GetByIdAsync((long)userId);
            if (user == null)
            {
                return new BaseResponseModel
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status404NotFound,
                    Message = "Người dùng không tồn tại"
                };
            }

            var submissionRepo = _unitOfWork.GetRepo<Submission>();
            var submission = await submissionRepo.GetSingleAsync(new QueryBuilder<Submission>()
                .WithPredicate(x => x.Id == id && x.IsActive && x.DeletedAt == null)
                .WithInclude(x => x.TopicVersion)
                .WithTracking(true)
                .Build());

            if (submission == null)
            {
                return new BaseResponseModel
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status404NotFound,
                    Message = "Submission không tồn tại"
                };
            }

            if (submission.SubmittedBy != userId && !isAdmin)
            {
                return new BaseResponseModel
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status403Forbidden,
                    Message = "Bạn không có quyền hủy/xóa submission này"
                };
            }

            if (submission.Status == SubmissionStatus.UnderReview || submission.Status == SubmissionStatus.Completed)
            {
                return new BaseResponseModel
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status400BadRequest,
                    Message = "Chỉ được hủy/xóa khi submission ở trạng thái Pending hoặc RevisionRequired"
                };
            }

            await _unitOfWork.BeginTransactionAsync();

            var versionRepo = _unitOfWork.GetRepo<TopicVersion>();
            var topicVersion = submission.TopicVersion;

            if (topicVersion != null)
            {
                if (topicVersion.Status == TopicStatus.SubmissionPending)
                {
                    topicVersion.Status = TopicStatus.Draft;
                    await versionRepo.UpdateAsync(topicVersion);
                }
                else if (topicVersion.Status == TopicStatus.Submitted)
                {
                    topicVersion.Status = TopicStatus.Archived;
                    await versionRepo.UpdateAsync(topicVersion);
                }
            }

            submission.IsActive = false;
            submission.DeletedAt = DateTime.Now;
            submission.LastModifiedBy = user.UserName;
            await submissionRepo.UpdateAsync(submission);

            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitTransactionAsync();

            return new BaseResponseModel
            {
                IsSuccess = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Hủy/Xóa submission thành công"
            };
        }
        catch (Exception)
        {
            await _unitOfWork.RollBackAsync();
            throw;
        }
    }
}