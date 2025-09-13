using System;
using App.BLL.Interfaces;
using App.Commons.Extensions;
using App.Commons.Paging;
using App.Commons.ResponseModel;
using App.DAL.Interfaces;
using App.DAL.Queries.Implementations;
using App.DAL.UnitOfWork;
using App.Entities.DTOs.Topics;
using App.Entities.DTOs.TopicVersions;
using App.Entities.Entities.App;
using App.Entities.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace App.BLL.Implementations;

public class TopicVersionService : ITopicVersionService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IIdentityRepository _identityRepository;

    public TopicVersionService(IUnitOfWork unitOfWork, IIdentityRepository identityRepository)
    {
        this._unitOfWork = unitOfWork;
        this._identityRepository = identityRepository;
    }

    public async Task<BaseResponseModel<CreaterTopicVersionResDTO>> CreateTopicVersion(CreateTopicVersionDTO createTopicVersionDTO, int userId)
    {
        try
        {
            var user = await _identityRepository.GetByIdAsync((long)userId);
            if (user == null)
            {
                return new BaseResponseModel<CreaterTopicVersionResDTO>
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status404NotFound,
                    Message = "Người dùng không tồn tại"
                };
            }

            var topicRepo = _unitOfWork.GetRepo<Topic>();
            var topic = await topicRepo.GetSingleAsync(new QueryBuilder<Topic>()
                .WithPredicate(x => x.Id == createTopicVersionDTO.TopicId && x.IsActive && x.DeletedAt == null)
                .WithTracking(false)
                .Build());

            if (topic == null)
            {
                return new BaseResponseModel<CreaterTopicVersionResDTO>
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status404NotFound,
                    Message = "Chủ đề không tồn tại"
                };
            }

            if (topic.SupervisorId != userId)
            {
                return new BaseResponseModel<CreaterTopicVersionResDTO>
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status403Forbidden,
                    Message = "Bạn không có quyền tạo phiên bản cho chủ đề này"
                };
            }

            if (createTopicVersionDTO.FileId.HasValue)
            {
                var fileRepo = _unitOfWork.GetRepo<AppFile>();
                var file = await fileRepo.GetSingleAsync(new QueryBuilder<AppFile>()
                    .WithPredicate(x => x.Id == createTopicVersionDTO.FileId.Value && x.CreatedBy == user.UserName && x.IsActive && x.DeletedAt == null)
                    .WithTracking(false)
                    .Build());

                if (file == null)
                {
                    return new BaseResponseModel<CreaterTopicVersionResDTO>
                    {
                        IsSuccess = false,
                        StatusCode = StatusCodes.Status409Conflict,
                        Message = "File không tồn tại hoặc không phải của bạn"
                    };
                }
            }

            await _unitOfWork.BeginTransactionAsync();

            var versionRepo = _unitOfWork.GetRepo<TopicVersion>();

            var latestVersion = await versionRepo.GetSingleAsync(new QueryBuilder<TopicVersion>()
                .WithPredicate(x => x.TopicId == createTopicVersionDTO.TopicId && x.IsActive && x.DeletedAt == null)
                .WithOrderBy(x => x.OrderByDescending(y => y.VersionNumber))
                .WithTracking(false)
                .Build());

            int newVersionNumber = (latestVersion?.VersionNumber ?? 0) + 1;

            var topicVersion = createTopicVersionDTO.GetEntity();

            topicVersion.VersionNumber = newVersionNumber;
            topicVersion.Status = TopicStatus.Draft;
            topicVersion.IsActive = true;
            topicVersion.CreatedBy = user.UserName;
            topicVersion.CreatedAt = DateTime.Now;


            await versionRepo.CreateAsync(topicVersion);
            await _unitOfWork.SaveChangesAsync();

            if (createTopicVersionDTO.FileId.HasValue)
            {
                var entityFileRepo = _unitOfWork.GetRepo<EntityFile>();
                await entityFileRepo.CreateAsync(new EntityFile
                {
                    FileId = createTopicVersionDTO.FileId.Value,
                    EntityType = EntityType.TopicVersion,
                    EntityId = topicVersion.Id,
                    IsPrimary = true,
                    Caption = topicVersion.EN_Title,
                    CreatedAt = DateTime.Now,
                });
            }

            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitTransactionAsync();

            var entityFileRepo_2 = _unitOfWork.GetRepo<EntityFile>();
            var entityFile = await entityFileRepo_2.GetSingleAsync(new QueryBuilder<EntityFile>()
                .WithPredicate(x => x.EntityId == topicVersion.Id && x.EntityType == EntityType.TopicVersion && x.IsPrimary)
                .WithInclude(x => x.File!)
                .WithTracking(false)
                .Build());

            return new BaseResponseModel<CreaterTopicVersionResDTO>
            {
                Data = new CreaterTopicVersionResDTO(topicVersion, entityFile),
                IsSuccess = true,
                StatusCode = StatusCodes.Status201Created
            };
        }
        catch (System.Exception)
        {
            throw;
        }
    }

    public async Task<BaseResponseModel<TopicVersionDetailDTO>> UpdateTopicVersion(UpdateTopicVersionDTO updateTopicVersionDTO, int userId)
    {
        try
        {
            var user = await _identityRepository.GetByIdAsync((long)userId);
            if (user == null)
            {
                return new BaseResponseModel<TopicVersionDetailDTO>
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status404NotFound,
                    Message = "Người dùng không tồn tại"
                };
            }

            var versionRepo = _unitOfWork.GetRepo<TopicVersion>();
            var topicVersion = await versionRepo.GetSingleAsync(new QueryBuilder<TopicVersion>()
                .WithPredicate(x => x.Id == updateTopicVersionDTO.Id && x.IsActive && x.DeletedAt == null)
                .WithInclude(x => x.Topic)
                .WithTracking(true)
                .Build());

            if (topicVersion == null)
            {
                return new BaseResponseModel<TopicVersionDetailDTO>
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status404NotFound,
                    Message = "Phiên bản chủ đề không tồn tại"
                };
            }

            if (topicVersion.Topic.SupervisorId != userId)
            {
                return new BaseResponseModel<TopicVersionDetailDTO>
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status403Forbidden,
                    Message = "Bạn không có quyền cập nhật phiên bản này"
                };
            }

            if (topicVersion.Status != TopicStatus.Draft && topicVersion.Status != TopicStatus.SubmissionPending)
            {
                return new BaseResponseModel<TopicVersionDetailDTO>
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status400BadRequest,
                    Message = "Chỉ có thể cập nhật phiên bản ở trạng thái Draft hoặc SubmissionPending"
                };
            }

            if (updateTopicVersionDTO.FileId.HasValue)
            {
                var fileRepo = _unitOfWork.GetRepo<AppFile>();
                var file = await fileRepo.GetSingleAsync(new QueryBuilder<AppFile>()
                    .WithPredicate(x => x.Id == updateTopicVersionDTO.FileId.Value && x.CreatedBy == user.UserName && x.IsActive && x.DeletedAt == null)
                    .WithTracking(false)
                    .Build());

                if (file == null)
                {
                    return new BaseResponseModel<TopicVersionDetailDTO>
                    {
                        IsSuccess = false,
                        StatusCode = StatusCodes.Status409Conflict,
                        Message = "File không tồn tại hoặc không phải của bạn"
                    };
                }
            }

            await _unitOfWork.BeginTransactionAsync();

            topicVersion.EN_Title = updateTopicVersionDTO.EN_Title.Trim();
            topicVersion.Description = updateTopicVersionDTO.Description?.Trim();
            topicVersion.Objectives = updateTopicVersionDTO.Objectives?.Trim();
            topicVersion.Methodology = updateTopicVersionDTO.Methodology?.Trim();
            topicVersion.ExpectedOutcomes = updateTopicVersionDTO.ExpectedOutcomes?.Trim();
            topicVersion.Requirements = updateTopicVersionDTO.Requirements?.Trim();
            topicVersion.DocumentUrl = updateTopicVersionDTO.DocumentUrl?.Trim();
            topicVersion.LastModifiedBy = user.UserName;
            topicVersion.LastModifiedAt = DateTime.Now;
            topicVersion.VN_title = updateTopicVersionDTO.VN_title?.Trim();
            topicVersion.Problem = updateTopicVersionDTO.Problem?.Trim();
            topicVersion.Context = updateTopicVersionDTO.Context?.Trim();
            topicVersion.Content = updateTopicVersionDTO.Content?.Trim();

            await versionRepo.UpdateAsync(topicVersion);
            await _unitOfWork.SaveChangesAsync();

            if (updateTopicVersionDTO.FileId.HasValue)
            {
                var entityFileRepo = _unitOfWork.GetRepo<EntityFile>();
                var existedEntityFile = await entityFileRepo.GetSingleAsync(new QueryBuilder<EntityFile>()
                    .WithPredicate(x => x.EntityId == topicVersion.Id && x.EntityType == EntityType.TopicVersion && x.IsPrimary)
                    .WithTracking(false)
                    .Build());

                if (existedEntityFile != null)
                {
                    existedEntityFile.FileId = updateTopicVersionDTO.FileId.Value;
                    existedEntityFile.Caption = topicVersion.EN_Title;
                    existedEntityFile.CreatedAt = DateTime.Now;
                    await entityFileRepo.UpdateAsync(existedEntityFile);
                }
                else
                {
                    await entityFileRepo.CreateAsync(new EntityFile
                    {
                        EntityId = topicVersion.Id,
                        EntityType = EntityType.TopicVersion,
                        FileId = updateTopicVersionDTO.FileId.Value,
                        IsPrimary = true,
                        Caption = topicVersion.EN_Title,
                        CreatedAt = DateTime.Now,
                    });
                }
            }

            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitTransactionAsync();

            var entityFileRepo_2 = _unitOfWork.GetRepo<EntityFile>();

            var entityFile = await entityFileRepo_2.GetSingleAsync(new QueryBuilder<EntityFile>()
                .WithPredicate(x => x.EntityId == topicVersion.Id && x.EntityType == EntityType.TopicVersion && x.IsPrimary)
                .WithInclude(x => x.File!)
                .WithTracking(false)
                .Build());

            return new BaseResponseModel<TopicVersionDetailDTO>
            {
                Data = new TopicVersionDetailDTO(topicVersion, entityFile),
                IsSuccess = true,
                StatusCode = StatusCodes.Status200OK
            };
        }
        catch (System.Exception)
        {
            throw;
        }
    }

    public async Task<BaseResponseModel<PagingDataModel<TopicVersionOverviewDTO, GetTopicVersionQueryDTO>>> GetTopicVersionHistory(GetTopicVersionQueryDTO query, int topicId)
    {
        try
        {
            var topicRepo = _unitOfWork.GetRepo<Topic>();
            var topic = await topicRepo.GetSingleAsync(new QueryBuilder<Topic>()
                .WithPredicate(x => x.Id == topicId && x.IsActive && x.DeletedAt == null)
                .WithTracking(false)
                .Build());

            if (topic == null)
            {
                return new BaseResponseModel<PagingDataModel<TopicVersionOverviewDTO, GetTopicVersionQueryDTO>>
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status404NotFound,
                    Message = "Chủ đề không tồn tại"
                };
            }

            var versionRepo = _unitOfWork.GetRepo<TopicVersion>();
            var queryBuilder = new QueryBuilder<TopicVersion>()
                .WithPredicate(x => x.TopicId == topicId && x.IsActive && x.DeletedAt == null)
                .WithInclude(x => x.SubmittedByUser)
                .WithOrderBy(x => x.OrderByDescending(y => y.VersionNumber))
                .WithTracking(false);

            if (!string.IsNullOrEmpty(query.Keyword))
            {
                queryBuilder.WithPredicate(x =>
                    x.EN_Title.Contains(query.Keyword) ||
                    (x.VN_title != null && x.VN_title.Contains(query.Keyword)));
            }

            var versionQuery = versionRepo.Get(queryBuilder.Build());

            query.TotalRecord = await versionQuery.CountAsync();
            var versions = await versionQuery
            .OrderByDescending(x => x.CreatedAt)
            .ToPagedList(query.PageNumber, query.PageSize).ToListAsync();

            return new BaseResponseModel<PagingDataModel<TopicVersionOverviewDTO, GetTopicVersionQueryDTO>>
            {
                Data = new PagingDataModel<TopicVersionOverviewDTO, GetTopicVersionQueryDTO>(versions.Select(x => new TopicVersionOverviewDTO(x)).ToList(), query),
                IsSuccess = true,
                StatusCode = StatusCodes.Status200OK
            };
        }
        catch (System.Exception)
        {
            throw;
        }
    }

    public async Task<BaseResponseModel<TopicVersionDetailDTO>> GetTopicVersionDetail(int versionId)
    {
        try
        {
            var versionRepo = _unitOfWork.GetRepo<TopicVersion>();
            var topicVersion = await versionRepo.GetSingleAsync(new QueryBuilder<TopicVersion>()
                .WithPredicate(x => x.Id == versionId && x.IsActive && x.DeletedAt == null)
                .WithInclude(x => x.SubmittedByUser)
                .WithTracking(false)
                .Build());

            if (topicVersion == null)
            {
                return new BaseResponseModel<TopicVersionDetailDTO>
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status404NotFound,
                    Message = "Phiên bản chủ đề không tồn tại"
                };
            }

            var entityFileRepo = _unitOfWork.GetRepo<EntityFile>();
            var entityFile = await entityFileRepo.GetSingleAsync(new QueryBuilder<EntityFile>()
                .WithPredicate(x => x.EntityId == topicVersion.Id && x.EntityType == EntityType.TopicVersion && x.IsPrimary)
                .WithInclude(x => x.File!)
                .WithTracking(false)
                .Build());

            return new BaseResponseModel<TopicVersionDetailDTO>
            {
                Data = new TopicVersionDetailDTO(topicVersion, entityFile),
                IsSuccess = true,
                StatusCode = StatusCodes.Status200OK
            };
        }
        catch (System.Exception)
        {
            throw;
        }
    }

    /// <summary>
    /// Outdated
    /// </summary>
    /// <param name="submitTopicVersionDTO"></param>
    /// <param name="userId"></param>
    /// <returns></returns>
    public async Task<BaseResponseModel> SubmitTopicVersion(SubmitTopicVersionDTO submitTopicVersionDTO, int userId)
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

            var versionRepo = _unitOfWork.GetRepo<TopicVersion>();
            var topicVersion = await versionRepo.GetSingleAsync(new QueryBuilder<TopicVersion>()
                .WithPredicate(x => x.Id == submitTopicVersionDTO.VersionId && x.IsActive && x.DeletedAt == null)
                .WithInclude(x => x.Topic)
                .WithTracking(true)
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

            if (topicVersion.Topic.SupervisorId != userId)
            {
                return new BaseResponseModel
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status403Forbidden,
                    Message = "Bạn không có quyền submit phiên bản này"
                };
            }

            if (topicVersion.Status != TopicStatus.Draft)
            {
                return new BaseResponseModel
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status400BadRequest,
                    Message = "Chỉ có thể submit phiên bản ở trạng thái Draft"
                };
            }

            topicVersion.Status = TopicStatus.Submitted;
            topicVersion.SubmittedAt = DateTime.Now;
            topicVersion.SubmittedBy = userId;
            topicVersion.LastModifiedBy = user.UserName;
            topicVersion.LastModifiedAt = DateTime.Now;

            await versionRepo.UpdateAsync(topicVersion);
            await _unitOfWork.SaveChangesAsync();

            return new BaseResponseModel
            {
                IsSuccess = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Submit phiên bản chủ đề thành công"
            };
        }
        catch (System.Exception)
        {
            throw;
        }
    }


    /// <summary>
    /// Outdated
    /// </summary>
    /// <param name="reviewTopicVersionDTO"></param>
    /// <param name="userId"></param>
    /// <param name="isReviewer"></param>
    /// <returns></returns>
    public async Task<BaseResponseModel> ReviewTopicVersion(ReviewTopicVersionDTO reviewTopicVersionDTO, int userId, bool isReviewer)
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

            if (!isReviewer)
            {
                return new BaseResponseModel
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status403Forbidden,
                    Message = "Bạn không có quyền review phiên bản chủ đề"
                };
            }

            var versionRepo = _unitOfWork.GetRepo<TopicVersion>();
            var topicVersion = await versionRepo.GetSingleAsync(new QueryBuilder<TopicVersion>()
                .WithPredicate(x => x.Id == reviewTopicVersionDTO.VersionId && x.IsActive && x.DeletedAt == null)
                .WithTracking(true)
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

            if (topicVersion.Status != TopicStatus.Submitted)
            {
                return new BaseResponseModel
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status400BadRequest,
                    Message = "Chỉ có thể review phiên bản ở trạng thái Submitted"
                };
            }

            topicVersion.Status = reviewTopicVersionDTO.Status;
            topicVersion.LastModifiedBy = user.UserName;
            topicVersion.LastModifiedAt = DateTime.Now;

            await versionRepo.UpdateAsync(topicVersion);
            await _unitOfWork.SaveChangesAsync();

            return new BaseResponseModel
            {
                IsSuccess = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Review phiên bản chủ đề thành công"
            };
        }
        catch (System.Exception)
        {
            throw;
        }
    }


    public async Task<BaseResponseModel> DeleteTopicVersion(int versionId, int userId, bool isAdmin)
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

            var versionRepo = _unitOfWork.GetRepo<TopicVersion>();
            var topicVersion = await versionRepo.GetSingleAsync(new QueryBuilder<TopicVersion>()
                .WithPredicate(x => x.Id == versionId && x.IsActive && x.DeletedAt == null)
                .WithInclude(x => x.Topic)
                .WithTracking(true)
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

            if (topicVersion.Topic.SupervisorId != userId && !isAdmin)
            {
                return new BaseResponseModel
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status403Forbidden,
                    Message = "Bạn không có quyền xóa phiên bản này"
                };
            }

            if (topicVersion.Status != TopicStatus.Draft)
            {
                return new BaseResponseModel
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status400BadRequest,
                    Message = "Chỉ có thể xóa phiên bản ở trạng thái Draft"
                };
            }

            topicVersion.IsActive = false;
            topicVersion.DeletedAt = DateTime.Now;

            await versionRepo.UpdateAsync(topicVersion);
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
}