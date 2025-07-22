using System;
using App.BLL.Interfaces;
using App.Commons.ResponseModel;
using App.DAL.Interfaces;
using App.DAL.Queries.Implementations;
using App.DAL.UnitOfWork;
using App.Entities.DTOs.Topics;
using App.Entities.DTOs.TopicVersions;
using App.Entities.Entities.App;
using App.Entities.Enums;
using Microsoft.AspNetCore.Http;
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

            return new BaseResponseModel<CreaterTopicVersionResDTO>
            {
                Data = new CreaterTopicVersionResDTO(topicVersion),
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

            if (topicVersion.Status != TopicStatus.Draft)
            {
                return new BaseResponseModel<TopicVersionDetailDTO>
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status400BadRequest,
                    Message = "Chỉ có thể cập nhật phiên bản ở trạng thái Draft"
                };
            }

            topicVersion.Title = updateTopicVersionDTO.Title.Trim();
            topicVersion.Description = updateTopicVersionDTO.Description?.Trim();
            topicVersion.Objectives = updateTopicVersionDTO.Objectives?.Trim();
            topicVersion.Methodology = updateTopicVersionDTO.Methodology?.Trim();
            topicVersion.ExpectedOutcomes = updateTopicVersionDTO.ExpectedOutcomes?.Trim();
            topicVersion.Requirements = updateTopicVersionDTO.Requirements?.Trim();
            topicVersion.DocumentUrl = updateTopicVersionDTO.DocumentUrl?.Trim();
            topicVersion.LastModifiedBy = user.UserName;
            topicVersion.LastModifiedAt = DateTime.Now;

            await versionRepo.UpdateAsync(topicVersion);
            await _unitOfWork.SaveChangesAsync();

            return new BaseResponseModel<TopicVersionDetailDTO>
            {
                Data = new TopicVersionDetailDTO(topicVersion),
                IsSuccess = true,
                StatusCode = StatusCodes.Status200OK
            };
        }
        catch (System.Exception)
        {
            throw;
        }
    }

    public async Task<BaseResponseModel<List<TopicVersionDetailDTO>>> GetTopicVersionHistory(int topicId)
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
                return new BaseResponseModel<List<TopicVersionDetailDTO>>
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status404NotFound,
                    Message = "Chủ đề không tồn tại"
                };
            }

            var versionRepo = _unitOfWork.GetRepo<TopicVersion>();
            var versions = await versionRepo.GetAllAsync(new QueryBuilder<TopicVersion>()
                .WithPredicate(x => x.TopicId == topicId && x.IsActive && x.DeletedAt == null)
                .WithInclude(x => x.SubmittedByUser)
                .WithOrderBy(x => x.OrderByDescending(y => y.VersionNumber))
                .WithTracking(false)
                .Build());

            return new BaseResponseModel<List<TopicVersionDetailDTO>>
            {
                Data = versions.Select(x => new TopicVersionDetailDTO(x)).ToList(),
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

            return new BaseResponseModel<TopicVersionDetailDTO>
            {
                Data = new TopicVersionDetailDTO(topicVersion),
                IsSuccess = true,
                StatusCode = StatusCodes.Status200OK
            };
        }
        catch (System.Exception)
        {
            throw;
        }
    }

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

            // Check permission - only supervisor can submit
            if (topicVersion.Topic.SupervisorId != userId)
            {
                return new BaseResponseModel
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status403Forbidden,
                    Message = "Bạn không có quyền submit phiên bản này"
                };
            }

            // Only draft versions can be submitted
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

    public async Task<BaseResponseModel> ReviewTopicVersion(ReviewTopicVersionDTO reviewTopicVersionDTO, int userId)
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

            // Check if user is admin/reviewer (implement based on your role system)
            if (!IsUserReviewer(user))
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

            // Only submitted versions can be reviewed
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

    public async Task<BaseResponseModel> DeleteTopicVersion(int versionId, int userId)
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

            // Check permission - only supervisor or admin can delete
            if (topicVersion.Topic.SupervisorId != userId && !IsUserAdmin(user))
            {
                return new BaseResponseModel
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status403Forbidden,
                    Message = "Bạn không có quyền xóa phiên bản này"
                };
            }

            // Only draft versions can be deleted
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

    private bool IsUserAdmin(object user)
    {
        // Implement admin check logic based on your user roles structure
        return true; // Replace with actual implementation
    }

    private bool IsUserReviewer(object user)
    {
        // Implement reviewer check logic based on your user roles structure
        return true; // Replace with actual implementation
    }
}
