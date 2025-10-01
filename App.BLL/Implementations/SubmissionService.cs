using System;
using System.Linq.Expressions;
using App.BLL.Interfaces;
using App.Commons;
using App.Commons.Email;
using App.Commons.Email.Interfaces;
using App.Commons.Extensions;
using App.Commons.Interfaces;
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
using Elastic.Clients.Elasticsearch.Ingest;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MimeKit;

namespace App.BLL.Implementations;

public class SubmissionService : ISubmissionService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IIdentityRepository _identityRepository;
    private readonly INotificationService _notificationService;
    private readonly IAIService _aIService;
    private readonly IElasticsearchService _elasticsearchService;
    private readonly IEmailService _emailService;
    private readonly IPathProvider _pathProvider;
    private readonly IConfiguration _configuration;
    private readonly IAiRubricClient _aiRubricClient;

    private readonly ILogger<SubmissionService> _logger;

    public SubmissionService(IUnitOfWork unitOfWork,
     IIdentityRepository identityRepository,
     INotificationService notificationService,
     IAIService aIService,
     IElasticsearchService elasticsearchService,
     ILogger<SubmissionService> logger,
     IEmailService emailService,
     IPathProvider pathProvider,
     IConfiguration configuration,
     IAiRubricClient aiRubricClient)
    {
        _unitOfWork = unitOfWork;
        _identityRepository = identityRepository;
        _notificationService = notificationService;
        _aIService = aIService;
        _elasticsearchService = elasticsearchService;
        this._emailService = emailService;
        this._pathProvider = pathProvider;
        this._configuration = configuration;

        this._logger = logger;
        _aiRubricClient = aiRubricClient;
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
            var title = submission.TopicVersion?.EN_Title ?? submission.Topic.EN_Title;
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
                .WithInclude(x => x.Topic)
                .WithInclude(x => x.Phase)
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
            try
            {
                var (stream, fileName) = await GetPrimaryDocxStreamAsync(submission.Id);
                if (stream == null)
                {
                    // Không chặn quy trình, nhưng set trạng thái AI = Error
                    submissionT.AiCheckStatus = AiCheckStatus.Failed;
                    submissionT.AiCheckScore = null;
                    submissionT.AiCheckDetails = "Không tìm thấy file .docx primary để chấm.";
                }
                else
                {
                    // Gọi AI
                    var titlE = submissionT.Topic.EN_Title ?? submissionT.TopicVersion?.EN_Title;
                    var categoryId = submissionT.Topic?.CategoryId;
                    var supervisorId = submissionT.Topic?.SupervisorId ?? userId;
                    var semesterId = submissionT.Phase?.SemesterId ?? submission.Topic?.SemesterId ?? 0;
                    var maxStudents = submissionT.Topic?.MaxStudents ?? 4;

                    var ai = await _aiRubricClient.EvaluateDocxAsync(
                        stream, fileName!, title, supervisorId, semesterId, categoryId, maxStudents);

                    // Map kết quả: Score thang 10, Status lấy overall_rating, Details lưu full JSON
                    submissionT.AiCheckStatus = AiCheckStatus.Passed;                         // ví dụ: Excellent/Good/Fair/Poor
                    submissionT.AiCheckScore = (decimal)Math.Round(ai.OverallScore / 10.0, 2);     // thang 10
                    submissionT.AiCheckDetails = ai.RawJson;                               // full rubric JSON
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AI Rubric evaluation failed for submission {SubmissionId}", submission.Id);
                submissionT.AiCheckStatus = AiCheckStatus.Failed;
                submissionT.AiCheckScore = null;
                submissionT.AiCheckDetails = $"AI Rubric evaluation error: {ex.Message}";
            }
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
                    Message = $"Submission #{submissionT.Id} đã được submit với chủ đề {submissionT.Topic.EN_Title}",
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

            try
            {
                var moderatorEmails = moderators
                 .Where(x => !string.IsNullOrWhiteSpace(x.Email))
                 .Select(x => x.Email!)
                 .Distinct()
                 .ToList();
                var modTemplatePath = _pathProvider.GetEmailTemplatePath(Path.Combine("Email", "Submission", "submit-topic-moderator.html"));
                var supTemplatePath = _pathProvider.GetEmailTemplatePath(Path.Combine("Email", "Submission", "submit-topic-supervisor.html"));
                var htmlMod = await File.ReadAllTextAsync(modTemplatePath);
                var htmlSup = await File.ReadAllTextAsync(supTemplatePath);
                _logger.LogInformation("Email template path (moderator): {path}", modTemplatePath);
                _logger.LogInformation("Email template path (supervisor): {path}", supTemplatePath);
                var submissionUrl = $"{_configuration["AppSettings:HomeUrl"]}/api/submission/detail/{submissionT.Id}";
                var topicUrl = $"{_configuration["AppSettings:HomeUrl"]}/api/topic/detail/{submissionT.Topic.Id}";
                var callbackUrl = $"{_configuration["AppSettings:HomeUrl"]}/index.html";
                var bodyMod = new ContentBuilder(htmlMod)
                    .BuildCallback(new List<ObjectReplace>
                    {
                        new ObjectReplace { Name = "__submission_id__", Value = submissionT.Id.ToString() },
                        new ObjectReplace { Name = "__submission_url__", Value = submissionUrl },
                        new ObjectReplace { Name = "__topic_url__", Value = topicUrl },
                        new ObjectReplace { Name = "__topic_id__", Value = submissionT.Topic.Id.ToString() },
                        new ObjectReplace { Name = "__callback_url__", Value = callbackUrl },
                        new ObjectReplace { Name = "__user_name__", Value = user.UserName },
                        new ObjectReplace { Name = "__topic_title__", Value = submissionT.Topic.EN_Title }
                    })
                    .GetContent();
                // Email to moderators about the new submission
                if (moderatorEmails.Count > 0)
                {
                    var modMail = new EmailModel(moderatorEmails, $"Bài nộp mới: {submissionT.Topic.EN_Title}", bodyMod)
                    {
                        BodyHtml = bodyMod
                    };
                    await _emailService.SendEmailAsync(modMail);
                }

                // Email to supervisor confirming submit success
                var supervisorId = submissionT.Topic?.SupervisorId;
                if (supervisorId.HasValue)
                {
                    var supervisor = await _identityRepository.GetByIdAsync((long)supervisorId.Value);
                    if (!string.IsNullOrWhiteSpace(supervisor?.Email))
                    {
                        var bodySup = new ContentBuilder(htmlSup)
                            .BuildCallback(new List<ObjectReplace>
                            {
                                new ObjectReplace { Name = "__submission_id__", Value = submissionT.Id.ToString() },
                                new ObjectReplace { Name = "__submission_url__", Value = submissionUrl },
                                new ObjectReplace { Name = "__topic_url__", Value = topicUrl },
                                new ObjectReplace { Name = "__topic_id__", Value = submissionT.Topic.Id.ToString() },
                                new ObjectReplace { Name = "__callback_url__", Value = callbackUrl },
                                new ObjectReplace { Name = "__user_name__", Value = user.UserName },
                                new ObjectReplace { Name = "__topic_title__", Value = submissionT.Topic.EN_Title }
                            })
                            .GetContent();
                        var supRecipients = new List<string> { supervisor!.Email! };
                        var supMail = new EmailModel(supRecipients, $"Nộp đề tài thành công: {submissionT.Topic.EN_Title}", bodySup)
                        {
                            BodyHtml = bodySup
                        };
                        await _emailService.SendEmailAsync(supMail);
                    }
                }
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex.Message, ex.StackTrace, "Failed to send notifications for submission {SubmissionId}", submissionT.Id);

            }

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
            var title = topicVersion.EN_Title ?? submission.Topic.EN_Title;
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
            try
            {
                var (stream, fileName) = await GetPrimaryDocxStreamAsync(dto.Id);
                if (stream == null)
                {
                    // Không chặn quy trình, nhưng set trạng thái AI = Error
                    submissionT.AiCheckStatus = AiCheckStatus.Failed;
                    submissionT.AiCheckScore = null;
                    submissionT.AiCheckDetails = "Không tìm thấy file .docx primary để chấm.";
                }
                else
                {
                    // Gọi AI
                    var titlE = submissionT.TopicVersion?.EN_Title ?? submissionT.Topic?.EN_Title;
                    var categoryId = submissionT.Topic?.CategoryId;
                    var supervisorId = submissionT.Topic?.SupervisorId ?? userId;
                    var semesterId = submissionT.Phase?.SemesterId ?? submissionT.Topic?.SemesterId ?? 0;
                    var maxStudents = submissionT.Topic?.MaxStudents ?? 4;

                    var ai = await _aiRubricClient.EvaluateDocxAsync(
                        stream, fileName!, title, supervisorId, semesterId, categoryId, maxStudents);

                    // Map kết quả: Score thang 10, Status lấy overall_rating, Details lưu full JSON
                    submissionT.AiCheckStatus = AiCheckStatus.Passed;                         // ví dụ: Excellent/Good/Fair/Poor
                    submissionT.AiCheckScore = (decimal)Math.Round(ai.OverallScore / 10.0, 2);     // thang 10
                    submissionT.AiCheckDetails = ai.RawJson;                               // full rubric JSON
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AI Rubric evaluation failed for submission {SubmissionId}", submission.Id);
                submissionT.AiCheckStatus = AiCheckStatus.Failed;
                submissionT.AiCheckScore = null;
                submissionT.AiCheckDetails = $"AI Rubric evaluation error: {ex.Message}";
            }
            await submissionRepoT.UpdateAsync(submissionT);

            var topicVersionT = await versionRepoT.GetSingleAsync(new QueryBuilder<TopicVersion>()
                .WithPredicate(x => x.Id == topicVersion.Id && x.IsActive && x.DeletedAt == null)
                .WithTracking(true)
                .Build());
            topicVersionT.Status = TopicStatus.Submitted;
            await versionRepoT.UpdateAsync(topicVersionT);

            var moderators = await _identityRepository.GetUsersInRoleAsync(SystemRoleConstants.Moderator);
            var moderatorIds = moderators.Select(x => (int)x.Id).Distinct().ToList();
            var topicRepo = _unitOfWork.GetRepo<Topic>();
            var topic = await topicRepo.GetSingleAsync(new QueryBuilder<Topic>()
                .WithPredicate(x => x.Id == submission.TopicId && x.IsActive && x.DeletedAt == null)
                .WithTracking(true)
                .Build());

            if (moderatorIds.Count > 0)
            {
                var createBulkNotification = await _notificationService.CreateBulkAsync(new CreateBulkNotificationsDTO
                {
                    UserIds = moderatorIds,
                    Title = "Thông báo về submission mới",
                    Message = $"Submission #{submission.Id} đã được resubmit với đề tài {topic.EN_Title} và phiên bản đề tài {topicVersion.EN_Title}",
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
                    Message = $"Submission #{submission.Id} đã được resubmit với đề tài {topic.EN_Title} và phiên bản đề tài {topicVersion.EN_Title}",
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

            try
            {
                var moderatorEmails = moderators
                    .Where(x => !string.IsNullOrWhiteSpace(x.Email))
                    .Select(x => x.Email!)
                    .Distinct()
                    .ToList();

                var modTemplatePath = _pathProvider.GetEmailTemplatePath(Path.Combine("Email", "Submission", "submit-topic-moderator.html"));
                var supTemplatePath = _pathProvider.GetEmailTemplatePath(Path.Combine("Email", "Submission", "submit-topic-supervisor.html"));
                var htmlMod = await File.ReadAllTextAsync(modTemplatePath);
                var htmlSup = await File.ReadAllTextAsync(supTemplatePath);
                _logger.LogInformation("Email template path (moderator): {path}", modTemplatePath);
                _logger.LogInformation("Email template path (supervisor): {path}", supTemplatePath);
                var submissionUrl = $"{_configuration["AppSettings:HomeUrl"]}/api/submission/detail/{submissionT.Id}";
                var topicUrl = $"{_configuration["AppSettings:HomeUrl"]}/api/topic/detail/{submissionT.Topic.Id}";
                var callbackUrl = $"{_configuration["AppSettings:HomeUrl"]}/index.html";
                var bodyMod = new ContentBuilder(htmlMod)
                    .BuildCallback(new List<ObjectReplace>
                    {
                        new ObjectReplace { Name = "__submission_id__", Value = submissionT.Id.ToString() },
                        new ObjectReplace { Name = "__submission_round__", Value = submissionT.SubmissionRound.ToString() },
                        new ObjectReplace { Name = "__submission_url__", Value = submissionUrl },
                        new ObjectReplace { Name = "__topic_url__", Value = topicUrl },
                        new ObjectReplace { Name = "__topic_id__", Value = submissionT.Topic.Id.ToString() },
                        new ObjectReplace { Name = "__callback_url__", Value = callbackUrl },
                        new ObjectReplace { Name = "__user_name__", Value = user.UserName },
                        new ObjectReplace { Name = "__topic_title__", Value = submissionT.Topic.EN_Title }
                    })
                    .GetContent();
                if (moderatorEmails.Count > 0)
                {
                    var modMail = new EmailModel(moderatorEmails, $"Nộp lại bài nộp: {submissionT.Topic.EN_Title}", bodyMod)
                    {
                        BodyHtml = bodyMod
                    };
                    await _emailService.SendEmailAsync(modMail);
                }

                var supervisorId = submissionT.Topic?.SupervisorId;
                if (supervisorId.HasValue)
                {
                    var supervisor = await _identityRepository.GetByIdAsync((long)supervisorId.Value);
                    if (!string.IsNullOrWhiteSpace(supervisor?.Email))
                    {
                        var bodySup = new ContentBuilder(htmlSup)
                            .BuildCallback(new List<ObjectReplace>
                            {
                                new ObjectReplace { Name = "__submission_id__", Value = submissionT.Id.ToString() },
                                new ObjectReplace { Name = "__submission_round__", Value = submissionT.SubmissionRound.ToString() },
                                new ObjectReplace { Name = "__submission_url__", Value = submissionUrl },
                                new ObjectReplace { Name = "__topic_url__", Value = topicUrl },
                                new ObjectReplace { Name = "__topic_id__", Value = submissionT.Topic.Id.ToString() },
                                new ObjectReplace { Name = "__callback_url__", Value = callbackUrl },
                                new ObjectReplace { Name = "__user_name__", Value = user.UserName },
                                new ObjectReplace { Name = "__topic_title__", Value = submissionT.Topic.EN_Title }
                            })
                            .GetContent();

                        var supRecipients = new List<string> { supervisor!.Email! };
                        var supMail = new EmailModel(supRecipients, $"Xác nhận nộp lại bài nộp: {submissionT.Topic.EN_Title}", bodySup)
                        {
                            BodyHtml = bodySup
                        };
                        await _emailService.SendEmailAsync(supMail);
                    }
                }
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex.Message, ex.StackTrace, "Failed to send notifications for submission {SubmissionId}", submissionT.Id);

            }

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
    private async Task<(Stream? stream, string? fileName)> GetPrimaryDocxStreamAsync(long submissionId)
    {
        var entityFileRepo = _unitOfWork.GetRepo<EntityFile>();

        var submissionRepo = _unitOfWork.GetRepo<Submission>();
        var submission =    submissionRepo.GetSingleAsync(new QueryBuilder<Submission>()
                .WithPredicate(x => x.Id == submissionId && x.IsActive)
                .WithTracking(false)
                .Build());

        var topicIdInSubmission = submission.Result.TopicId;
        var topicVersionIdSubmission = submission.Result.TopicVersionId;
        var link = null as EntityFile;
        if (topicVersionIdSubmission != null)
        {
            link = await entityFileRepo.GetSingleAsync(new QueryBuilder<EntityFile>()
            .WithPredicate(x => x.EntityId == topicVersionIdSubmission && x.EntityType == EntityType.TopicVersion && x.IsPrimary)
            .WithInclude(x => x.File!)
            .WithTracking(false)
            .Build());

        } else if (topicIdInSubmission != null)
        {
             link = await entityFileRepo.GetSingleAsync(new QueryBuilder<EntityFile>()
            .WithPredicate(x => x.EntityId == topicIdInSubmission && x.EntityType == EntityType.Topic && x.IsPrimary)
            .WithInclude(x => x.File!)
            .WithTracking(false)
            .Build());
        }

        //var link = await entityFileRepo.GetSingleAsync(new QueryBuilder<EntityFile>()
        //    .WithPredicate(x => x.EntityId == submissionId && x.EntityType == EntityType.Submission && x.IsPrimary)
        //    .WithInclude(x => x.File!)
        //    .WithTracking(false)
        //    .Build());

        if (link?.File == null) return (null, null);

        var file = link.File;
        var path = file.FilePath; // giả định lưu local path; nếu chỉ có Url thì bạn có thể tải về bằng HttpClient

        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            return (null, null);

        if (!path.EndsWith(".docx", StringComparison.OrdinalIgnoreCase))
            return (null, null);

        var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        var name = string.IsNullOrWhiteSpace(file.FileName) ? Path.GetFileName(path) : file.FileName;
        return (stream, name);
    }
}