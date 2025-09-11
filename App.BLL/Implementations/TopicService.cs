using System;
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
using App.Entities.DTOs.Topics;
using App.Entities.Entities.App;
using App.Entities.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace App.BLL.Implementations;

public class TopicService : ITopicService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IIdentityRepository _identityRepository;
    private readonly INotificationService _notificationService;
    private readonly IAIService _aiService;
    private readonly IElasticsearchService _elasticsearchService;
    private readonly IEmailService _emailService;
    private readonly IPathProvider _pathProvider;
    private readonly IConfiguration _configuration;
    private readonly ILogger<TopicService> _logger;

    public TopicService(IUnitOfWork unitOfWork,
    IIdentityRepository identityRepository,
    INotificationService notificationService,
    IAIService aiService,
    IElasticsearchService elasticsearchService,
    IEmailService emailService,
    IPathProvider pathProvider,
    IConfiguration configuration,
    ILogger<TopicService> logger)
    {
        this._unitOfWork = unitOfWork;
        this._identityRepository = identityRepository;
        this._notificationService = notificationService;
        this._aiService = aiService;
        this._elasticsearchService = elasticsearchService;
        this._emailService = emailService;
        this._pathProvider = pathProvider;
        this._configuration = configuration;
        this._logger = logger;

    }

    public async Task<BaseResponseModel<CreateTopicResDTO>> CreateTopic(CreateTopicDTO createTopicDTO, int userId)
    {
        try
        {
            var user = await _identityRepository.GetByIdAsync((long)userId);
            if (user == null)
            {
                return new BaseResponseModel<CreateTopicResDTO>
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status404NotFound,
                    Message = "Người dùng không tồn tại"
                };
            }

            var categoryRepo = _unitOfWork.GetRepo<TopicCategory>();
            var category = await categoryRepo.GetSingleAsync(new QueryBuilder<TopicCategory>()
                .WithPredicate(x => x.Id == createTopicDTO.CategoryId && x.IsActive && x.DeletedAt == null)
                .WithTracking(false)
                .Build());

            if (category == null)
            {
                return new BaseResponseModel<CreateTopicResDTO>
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status409Conflict,
                    Message = "Danh mục chủ đề không tồn tại"
                };
            }

            var semesterRepo = _unitOfWork.GetRepo<Semester>();
            var semester = await semesterRepo.GetSingleAsync(new QueryBuilder<Semester>()
                .WithPredicate(x => x.Id == createTopicDTO.SemesterId && x.IsActive && x.DeletedAt == null)
                .WithTracking(false)
                .Build());

            if (semester == null)
            {
                return new BaseResponseModel<CreateTopicResDTO>
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status409Conflict,
                    Message = "Học kỳ không tồn tại"
                };
            }

            if (createTopicDTO.FileId.HasValue)
            {
                var fileRepo = _unitOfWork.GetRepo<AppFile>();
                var file = await fileRepo.GetSingleAsync(new QueryBuilder<AppFile>()
                    .WithPredicate(x => x.Id == createTopicDTO.FileId.Value && x.CreatedBy == user.UserName && x.IsActive && x.DeletedAt == null)
                    .WithTracking(false)
                    .Build());

                if (file == null)
                {
                    return new BaseResponseModel<CreateTopicResDTO>
                    {
                        IsSuccess = false,
                        StatusCode = StatusCodes.Status409Conflict,
                        Message = "File không tồn tại hoặc không phải của bạn"
                    };
                }
            }

            await _unitOfWork.BeginTransactionAsync();

            var topicRepo = _unitOfWork.GetRepo<Topic>();
            var versionRepo = _unitOfWork.GetRepo<TopicVersion>();
            var entityFileRepo = _unitOfWork.GetRepo<EntityFile>();

            var topic = createTopicDTO.GetEntity();
            topic.SupervisorId = userId;
            topic.IsApproved = false;
            topic.IsLegacy = false;
            topic.IsActive = true;
            topic.CreatedBy = user.UserName;
            topic.CreatedAt = DateTime.Now;

            await topicRepo.CreateAsync(topic);
            await _unitOfWork.SaveChangesAsync();

            if (createTopicDTO.FileId.HasValue)
            {
                await entityFileRepo.CreateAsync(new EntityFile
                {
                    EntityId = topic.Id,
                    EntityType = EntityType.Topic,
                    FileId = createTopicDTO.FileId.Value,
                    IsPrimary = true,
                    Caption = topic.Title,
                    CreatedAt = DateTime.Now,
                });
            }

            var moderators = await _identityRepository.GetUsersInRoleAsync(SystemRoleConstants.Moderator);
            var moderatorIds = moderators.Select(x => x.Id).Distinct().ToList();
            if (moderatorIds.Count > 0)
            {
                var createBulkNotification = await _notificationService.CreateBulkAsync(new CreateBulkNotificationsDTO
                {
                    UserIds = moderatorIds,
                    Title = "Thông báo về chủ đề mới",
                    Message = $"Chủ đề {topic.Title} đã được tạo bởi {user.UserName}",
                    Type = NotificationTypes.Info,
                    RelatedEntityType = EntityType.Topic.ToString(),
                    RelatedEntityId = topic.Id
                });

                if (!createBulkNotification.IsSuccess)
                {
                    throw new Exception(createBulkNotification.Message);
                }
            }

            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitTransactionAsync();

            topic.Supervisor = user;
            topic.Category = category;
            topic.Semester = semester;

            //Email sender
            var adminEmail = _configuration["AdminAccount:Email"];
            if (!string.IsNullOrWhiteSpace(adminEmail))
            {
                try
                {
                    var templatePath = _pathProvider.GetEmailTemplatePath(Path.Combine("Email", "Topic", "CreateTopic.html"));
                    var html = await File.ReadAllTextAsync(templatePath);
                    _logger.LogInformation("Email template path: {path}", templatePath);
                    _logger.LogInformation("Email template content: {html}", html);

                    var callbackUrl = $"{_configuration["AppSettings:HomeUrl"]}/api/topic/detail/{topic.Id}";

                    var body = new ContentBuilder(html)
                        .BuildCallback(new List<ObjectReplace>
                        {
                            new ObjectReplace { Name = "__calback_url__", Value = callbackUrl },
                            new ObjectReplace { Name = "__user_name__", Value = user.UserName },
                            new ObjectReplace { Name = "__topic_title__", Value = topic.Title }
                        })
                        .GetContent();

                    var mail = new EmailModel(new[] { adminEmail }, $"Chủ đề mới: {topic.Title}", body)
                    {
                        BodyHtml = body
                    };


                    await _emailService.SendEmailAsync(mail);
                }
                catch (Exception ex)
                {
                    // avoid breaking the flow if email fails
                    _logger.LogError(ex.Message, ex.StackTrace, "Failed to send email to admin about new topic created");
                }
            }

            return new BaseResponseModel<CreateTopicResDTO>
            {
                Data = new CreateTopicResDTO(topic),
                IsSuccess = true,
                StatusCode = StatusCodes.Status201Created
            };
        }
        catch (System.Exception)
        {
            await _unitOfWork.RollBackAsync();
            throw;
        }
    }

    public async Task<BaseResponseModel<PagingDataModel<TopicOverviewResDTO, GetTopicsQueryDTO>>> GetTopicsWithPaging(GetTopicsQueryDTO query)
    {
        try
        {
            var topicRepo = _unitOfWork.GetRepo<Topic>();
            var queryBuilder = new QueryBuilder<Topic>()
                .WithPredicate(x => x.IsActive && x.DeletedAt == null)
                .WithInclude(x => x.Supervisor)
                .WithInclude(x => x.Category)
                .WithInclude(x => x.Semester)
                .WithInclude(x => x.TopicVersions)
                .WithTracking(false);

            if (query.SemesterId.HasValue)
            {
                queryBuilder = queryBuilder.WithPredicate(x => x.SemesterId == query.SemesterId.Value);
            }

            if (query.CategoryId.HasValue)
            {
                queryBuilder = queryBuilder.WithPredicate(x => x.CategoryId == query.CategoryId.Value);
            }

            var topicQuery = topicRepo.Get(queryBuilder.Build());

            query.TotalRecord = await topicQuery.CountAsync();

            var topics = await topicQuery
            .OrderByDescending(x => x.CreatedAt)
            .ToPagedList(query.PageNumber, query.PageSize).ToListAsync();

            return new BaseResponseModel<PagingDataModel<TopicOverviewResDTO, GetTopicsQueryDTO>>
            {
                Data = new PagingDataModel<TopicOverviewResDTO, GetTopicsQueryDTO>(topics.Select(x => new TopicOverviewResDTO(x)), query),
                IsSuccess = true,
                StatusCode = StatusCodes.Status200OK
            };
        }
        catch (System.Exception)
        {
            throw;
        }
    }

    public async Task<BaseResponseModel<TopicDetailDTO>> GetTopicDetail(int topicId)
    {
        try
        {
            var topicRepo = _unitOfWork.GetRepo<Topic>();
            var topic = await topicRepo.GetSingleAsync(new QueryBuilder<Topic>()
                .WithPredicate(x => x.Id == topicId && x.IsActive && x.DeletedAt == null)
                .WithInclude(x => x.Supervisor)
                .WithInclude(x => x.Category)
                .WithInclude(x => x.Semester)
                .WithInclude(x => x.TopicVersions)
                .WithTracking(false)
                .Build());

            if (topic == null)
            {
                return new BaseResponseModel<TopicDetailDTO>
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status404NotFound,
                    Message = "Chủ đề không tồn tại"
                };
            }

            var entityFileRepo = _unitOfWork.GetRepo<EntityFile>();
            var entityFile = await entityFileRepo.GetSingleAsync(new QueryBuilder<EntityFile>()
                .WithPredicate(x => x.EntityId == topicId && x.EntityType == EntityType.Topic && x.IsPrimary)
                .WithInclude(x => x.File!)
                .WithTracking(false)
                .Build());

            return new BaseResponseModel<TopicDetailDTO>
            {
                Data = new TopicDetailDTO(topic, entityFile),
                IsSuccess = true,
                StatusCode = StatusCodes.Status200OK
            };
        }
        catch (System.Exception)
        {
            throw;
        }
    }

    public async Task<BaseResponseModel<UpdateTopicResDTO>> UpdateTopic(UpdateTopicDTO updateTopicDTO, int userId, bool isAdmin)
    {
        try
        {
            var user = await _identityRepository.GetByIdAsync((long)userId);
            if (user == null)
            {
                return new BaseResponseModel<UpdateTopicResDTO>
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status404NotFound,
                    Message = "Người dùng không tồn tại"
                };
            }

            var topicRepo = _unitOfWork.GetRepo<Topic>();
            var topic = await topicRepo.GetSingleAsync(new QueryBuilder<Topic>()
                .WithPredicate(x => x.Id == updateTopicDTO.Id && x.IsActive && x.DeletedAt == null)
                .WithInclude(x => x.Supervisor)
                .WithInclude(x => x.Category)
                .WithInclude(x => x.Semester)
                .WithInclude(x => x.TopicVersions)
                .WithTracking(true)
                .Build());

            if (topic == null)
            {
                return new BaseResponseModel<UpdateTopicResDTO>
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status409Conflict,
                    Message = "Chủ đề không tồn tại"
                };
            }

            if (topic.SupervisorId != userId && !isAdmin)
            {
                return new BaseResponseModel<UpdateTopicResDTO>
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status403Forbidden,
                    Message = "Bạn không có quyền cập nhật chủ đề này"
                };
            }

            var categoryRepo = _unitOfWork.GetRepo<TopicCategory>();
            var category = await categoryRepo.GetSingleAsync(new QueryBuilder<TopicCategory>()
                .WithPredicate(x => x.Id == updateTopicDTO.CategoryId && x.IsActive && x.DeletedAt == null)
                .WithTracking(false)
                .Build());

            if (category == null)
            {
                return new BaseResponseModel<UpdateTopicResDTO>
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status409Conflict,
                    Message = "Danh mục chủ đề không tồn tại"
                };
            }

            if (updateTopicDTO.FileId.HasValue)
            {
                var fileRepo = _unitOfWork.GetRepo<AppFile>();
                var file = await fileRepo.GetSingleAsync(new QueryBuilder<AppFile>()
                    .WithPredicate(x => x.Id == updateTopicDTO.FileId.Value && x.IsActive && x.DeletedAt == null)
                    .WithTracking(false)
                    .Build());

                if (file == null)
                {
                    return new BaseResponseModel<UpdateTopicResDTO>
                    {
                        IsSuccess = false,
                        StatusCode = StatusCodes.Status409Conflict,
                        Message = "File không tồn tại"
                    };
                }

                if (file.CreatedBy != user.UserName && !isAdmin)
                {
                    return new BaseResponseModel<UpdateTopicResDTO>
                    {
                        IsSuccess = false,
                        StatusCode = StatusCodes.Status409Conflict,
                        Message = "File không phải của bạn"
                    };
                }
            }

            await _unitOfWork.BeginTransactionAsync();

            topic.Title = updateTopicDTO.Title.Trim();
            topic.Description = updateTopicDTO.Description?.Trim();
            topic.Objectives = updateTopicDTO.Objectives?.Trim();
            topic.CategoryId = updateTopicDTO.CategoryId;
            topic.MaxStudents = updateTopicDTO.MaxStudents;
            topic.LastModifiedBy = user.UserName;
            topic.LastModifiedAt = DateTime.Now;

            await topicRepo.UpdateAsync(topic);

            if (updateTopicDTO.FileId.HasValue)
            {
                var entityFileRepo = _unitOfWork.GetRepo<EntityFile>();
                var existedEntityFile = await entityFileRepo.GetSingleAsync(new QueryBuilder<EntityFile>()
                    .WithPredicate(x => x.EntityId == topic.Id && x.EntityType == EntityType.Topic && x.IsPrimary)
                    .WithTracking(false)
                    .Build());

                if (existedEntityFile != null)
                {
                    existedEntityFile.FileId = updateTopicDTO.FileId.Value;
                    existedEntityFile.Caption = topic.Title;
                    existedEntityFile.CreatedAt = DateTime.Now;
                    await entityFileRepo.UpdateAsync(existedEntityFile);
                }
                else
                {
                    await entityFileRepo.CreateAsync(new EntityFile
                    {
                        EntityId = topic.Id,
                        EntityType = EntityType.Topic,
                        FileId = updateTopicDTO.FileId.Value,
                        IsPrimary = true,
                        Caption = topic.Title,
                        CreatedAt = DateTime.Now,
                    });
                }
            }

            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitTransactionAsync();

            var entityFileRepo_2 = _unitOfWork.GetRepo<EntityFile>();
            var entityFile = await entityFileRepo_2.GetSingleAsync(new QueryBuilder<EntityFile>()
                .WithPredicate(x => x.EntityId == topic.Id && x.EntityType == EntityType.Topic && x.IsPrimary)
                .WithInclude(x => x.File!)
                .WithTracking(false)
                .Build());

            return new BaseResponseModel<UpdateTopicResDTO>
            {
                Data = new UpdateTopicResDTO(topic, entityFile),
                IsSuccess = true,
                StatusCode = StatusCodes.Status200OK
            };
        }
        catch (System.Exception)
        {
            throw;
        }
    }

    public async Task<BaseResponseModel> DeleteTopic(int topicId, int userId, bool isAdmin)
    {
        try
        {
            var topicRepo = _unitOfWork.GetRepo<Topic>();
            var topic = await topicRepo.GetSingleAsync(new QueryBuilder<Topic>()
                .WithPredicate(x => x.Id == topicId && x.IsActive && x.DeletedAt == null)
                .WithTracking(true)
                .Build());

            if (topic == null)
            {
                return new BaseResponseModel
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status404NotFound,
                    Message = "Chủ đề không tồn tại"
                };
            }

            if (topic.SupervisorId != userId && !isAdmin)
            {
                return new BaseResponseModel
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status403Forbidden,
                    Message = "Bạn không có quyền xóa chủ đề này"
                };
            }

            topic.IsActive = false;
            topic.DeletedAt = DateTime.Now;

            await topicRepo.UpdateAsync(topic);
            await _unitOfWork.SaveChangesAsync();

            return new BaseResponseModel
            {
                IsSuccess = true,
                StatusCode = StatusCodes.Status200OK
            };
        }
        catch (System.Exception)
        {
            throw;
        }
    }

    public async Task<BaseResponseModel> ApproveTopic(int topicId, int userId, bool isAdmin, bool isModerator)
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

            if (!isAdmin && !isModerator)
            {
                return new BaseResponseModel
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status403Forbidden,
                    Message = "Bạn không có quyền phê duyệt chủ đề"
                };
            }

            var topicRepo = _unitOfWork.GetRepo<Topic>();
            var topic = await topicRepo.GetSingleAsync(new QueryBuilder<Topic>()
                .WithPredicate(x => x.Id == topicId && x.IsActive && x.DeletedAt == null)
                .WithTracking(true)
                .Build());

            if (topic == null)
            {
                return new BaseResponseModel
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status404NotFound,
                    Message = "Chủ đề không tồn tại"
                };
            }

            topic.IsApproved = true;
            topic.LastModifiedBy = user.UserName;
            topic.LastModifiedAt = DateTime.Now;

            await topicRepo.UpdateAsync(topic);
            await _unitOfWork.SaveChangesAsync();

            return new BaseResponseModel
            {
                IsSuccess = true,
                StatusCode = StatusCodes.Status200OK
            };
        }
        catch (System.Exception)
        {
            throw;
        }
    }

    public async Task<BaseResponseModel<PagingDataModel<TopicOverviewResDTO, GetTopicsQueryDTO>>> GetMyTopics(int userId, GetTopicsQueryDTO query)
    {
        try
        {
            var topicRepo = _unitOfWork.GetRepo<Topic>();
            var queryBuilder = new QueryBuilder<Topic>()
                .WithPredicate(x => x.SupervisorId == userId && x.IsActive && x.DeletedAt == null)
                .WithInclude(x => x.Supervisor)
                .WithInclude(x => x.Category!)
                .WithInclude(x => x.Semester)
                .WithInclude(x => x.TopicVersions)
                .WithTracking(false);

            if (query.SemesterId.HasValue)
            {
                queryBuilder = queryBuilder.WithPredicate(x => x.SemesterId == query.SemesterId.Value);
            }

            if (query.CategoryId.HasValue)
            {
                queryBuilder = queryBuilder.WithPredicate(x => x.CategoryId == query.CategoryId.Value);
            }

            var topicQuery = topicRepo.Get(queryBuilder.Build());

            query.TotalRecord = await topicQuery.CountAsync();

            var topics = await topicQuery
            .OrderByDescending(x => x.CreatedAt)
            .ToPagedList(query.PageNumber, query.PageSize).ToListAsync();

            return new BaseResponseModel<PagingDataModel<TopicOverviewResDTO, GetTopicsQueryDTO>>
            {
                Data = new PagingDataModel<TopicOverviewResDTO, GetTopicsQueryDTO>(topics.Select(x => new TopicOverviewResDTO(x)), query),
                IsSuccess = true,
                StatusCode = StatusCodes.Status200OK
            };
        }
        catch (System.Exception)
        {
            throw;
        }
    }

    public async Task<BaseResponseModel<TopicDuplicateCheckResDTO>> CheckDuplicateByTopicIdAsync(int topicId, double threshold = 0.6)
    {
        try
        {
            var topicRepo = _unitOfWork.GetRepo<Topic>();
            var topic = await topicRepo.GetSingleAsync(new App.DAL.Queries.Implementations.QueryBuilder<Topic>()
                .WithPredicate(x => x.Id == topicId && x.IsActive && x.DeletedAt == null)
                .WithTracking(false)
                .Build());

            if (topic == null)
            {
                return new BaseResponseModel<TopicDuplicateCheckResDTO>
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status404NotFound,
                    Message = "Topic không tồn tại"
                };
            }

            var title = topic.Title;
            var description = topic.Description;
            var keywords = await _aiService.GenerateKeywordsAsync(title, description);

            // Nếu AI không trả từ khóa, fallback dùng tiêu đề/mô tả
            var query = keywords.Count > 0
                ? string.Join(" ", keywords.Distinct())
                : $"{title} {description}".Trim();

            var searchRes = await _elasticsearchService.FindSimilarTopicsAsync(topicId, threshold);
            if (!searchRes.IsSuccess || searchRes.Data == null)
            {
                return new BaseResponseModel<TopicDuplicateCheckResDTO>
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status500InternalServerError,
                    Message = "Không thể tìm kiếm trùng lặp vào lúc này"
                };
            }

            var duplicates = searchRes.Data.SimilarTopics
                .Where(s => s.TopicId != topicId)
                .Select(s => new TopicDuplicateItemDTO
                {
                    TopicId = s.TopicId,
                    Title = s.Title,
                    SemesterName = s.SemesterName,
                    SupervisorName = s.SupervisorName,
                    SimilarityScore = s.SimilarityScore
                })
                .ToList();

            var response = new TopicDuplicateCheckResDTO
            {
                QueryTopicId = topic.Id,
                QueryTopicTitle = topic.Title,
                IsDuplicate = duplicates.Any(),
                Message = duplicates.Any() ? "found duplicates" : "topic passed",
                Duplicates = duplicates
            };

            return new BaseResponseModel<TopicDuplicateCheckResDTO>
            {
                IsSuccess = true,
                StatusCode = StatusCodes.Status200OK,
                Data = response
            };
        }
        catch (Exception ex)
        {
            return new BaseResponseModel<TopicDuplicateCheckResDTO>
            {
                IsSuccess = false,
                StatusCode = StatusCodes.Status500InternalServerError,
                Message = $"Lỗi kiểm tra trùng lặp: {ex.Message}"
            };
        }
    }
}