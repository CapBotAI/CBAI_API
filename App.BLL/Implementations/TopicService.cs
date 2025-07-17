using System;
using App.BLL.Interfaces;
using App.Commons.Extensions;
using App.Commons.Paging;
using App.Commons.ResponseModel;
using App.DAL.Interfaces;
using App.DAL.Queries.Implementations;
using App.DAL.UnitOfWork;
using App.Entities.DTOs.Topics;
using App.Entities.Entities.App;
using App.Entities.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace App.BLL.Implementations;

public class TopicService : ITopicService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IIdentityRepository _identityRepository;

    public TopicService(IUnitOfWork unitOfWork, IIdentityRepository identityRepository)
    {
        this._unitOfWork = unitOfWork;
        this._identityRepository = identityRepository;
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

            await _unitOfWork.BeginTransactionAsync();

            var topicRepo = _unitOfWork.GetRepo<Topic>();
            var versionRepo = _unitOfWork.GetRepo<TopicVersion>();

            var topic = createTopicDTO.GetEntity();
            topic.SupervisorId = userId;
            topic.IsApproved = false;
            topic.IsLegacy = false;
            topic.IsActive = true;
            topic.CreatedBy = user.UserName;
            topic.CreatedAt = DateTime.Now;

            await topicRepo.CreateAsync(topic);
            await _unitOfWork.SaveChangesAsync();

            // Create initial TopicVersion
            var topicVersion = new TopicVersion
            {
                TopicId = topic.Id,
                VersionNumber = 1,
                Title = createTopicDTO.Title.Trim(),
                Description = createTopicDTO.Description?.Trim(),
                Objectives = createTopicDTO.Objectives?.Trim(),
                Methodology = createTopicDTO.Methodology?.Trim(),
                ExpectedOutcomes = createTopicDTO.ExpectedOutcomes?.Trim(),
                Requirements = createTopicDTO.Requirements?.Trim(),
                DocumentUrl = createTopicDTO.DocumentUrl?.Trim(),
                Status = TopicStatus.Draft,
                IsActive = true,
                CreatedBy = user.UserName,
                CreatedAt = DateTime.Now
            };

            await versionRepo.CreateAsync(topicVersion);

            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitTransactionAsync();

            topic.Supervisor = user;
            topic.Category = category;
            topic.Semester = semester;
            topic.TopicVersions = new List<TopicVersion> { topicVersion };

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

    public async Task<BaseResponseModel<PagingDataModel<TopicOverviewResDTO, GetTopicsQueryDTO>>> GetAllTopics(GetTopicsQueryDTO query)
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

            return new BaseResponseModel<TopicDetailDTO>
            {
                Data = new TopicDetailDTO(topic),
                IsSuccess = true,
                StatusCode = StatusCodes.Status200OK
            };
        }
        catch (System.Exception)
        {
            throw;
        }
    }

    public async Task<BaseResponseModel<TopicDetailDTO>> UpdateTopic(UpdateTopicDTO updateTopicDTO, int userId)
    {
        try
        {
            var user = await _identityRepository.GetByIdAsync((long)userId);
            if (user == null)
            {
                return new BaseResponseModel<TopicDetailDTO>
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
                .WithTracking(true)
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

            // Check permission - only supervisor or admin can update
            if (topic.SupervisorId != userId && !IsUserAdmin(user))
            {
                return new BaseResponseModel<TopicDetailDTO>
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status403Forbidden,
                    Message = "Bạn không có quyền cập nhật chủ đề này"
                };
            }

            // Validate Category exists
            var categoryRepo = _unitOfWork.GetRepo<TopicCategory>();
            var category = await categoryRepo.GetSingleAsync(new QueryBuilder<TopicCategory>()
                .WithPredicate(x => x.Id == updateTopicDTO.CategoryId && x.IsActive && x.DeletedAt == null)
                .WithTracking(false)
                .Build());

            if (category == null)
            {
                return new BaseResponseModel<TopicDetailDTO>
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status404NotFound,
                    Message = "Danh mục chủ đề không tồn tại"
                };
            }

            topic.Title = updateTopicDTO.Title.Trim();
            topic.Description = updateTopicDTO.Description?.Trim();
            topic.Objectives = updateTopicDTO.Objectives?.Trim();
            topic.CategoryId = updateTopicDTO.CategoryId;
            topic.MaxStudents = updateTopicDTO.MaxStudents;
            topic.LastModifiedBy = user.UserName;
            topic.LastModifiedAt = DateTime.Now;

            await topicRepo.UpdateAsync(topic);
            await _unitOfWork.SaveChangesAsync();

            return new BaseResponseModel<TopicDetailDTO>
            {
                Data = new TopicDetailDTO(topic),
                IsSuccess = true,
                StatusCode = StatusCodes.Status200OK
            };
        }
        catch (System.Exception)
        {
            throw;
        }
    }

    public async Task<BaseResponseModel> DeleteTopic(int topicId)
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

    public async Task<BaseResponseModel> ApproveTopic(int topicId, int userId)
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

            if (!IsUserAdmin(user))
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

    public async Task<BaseResponseModel<List<TopicOverviewResDTO>>> GetMyTopics(int userId)
    {
        try
        {
            var topicRepo = _unitOfWork.GetRepo<Topic>();
            var topics = await topicRepo.GetAllAsync(new QueryBuilder<Topic>()
                .WithPredicate(x => x.SupervisorId == userId && x.IsActive && x.DeletedAt == null)
                .WithInclude(x => x.Supervisor)
                .WithInclude(x => x.Category)
                .WithInclude(x => x.Semester)
                .WithInclude(x => x.TopicVersions)
                .WithTracking(false)
                .Build());

            return new BaseResponseModel<List<TopicOverviewResDTO>>
            {
                Data = topics.Select(x => new TopicOverviewResDTO(x)).ToList(),
                IsSuccess = true,
                StatusCode = StatusCodes.Status200OK
            };
        }
        catch (System.Exception)
        {
            throw;
        }
    }

    private bool IsUserAdmin(object user)
    {
        // Implement admin check logic based on your user roles structure
        // This is a placeholder - adjust according to your UserRole implementation
        return true; // Replace with actual implementation
    }
}
