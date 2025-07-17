using System;
using App.BLL.Interfaces;
using App.Commons.ResponseModel;
using App.DAL.Interfaces;
using App.DAL.Queries.Implementations;
using App.DAL.UnitOfWork;
using App.Entities.DTOs.TopicCategories;
using App.Entities.Entities.App;
using Microsoft.AspNetCore.Http;

namespace App.BLL.Implementations;

public class TopicCategoryService : ITopicCategoryService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IIdentityRepository _identityRepository;

    public TopicCategoryService(IUnitOfWork unitOfWork, IIdentityRepository identityRepository)
    {
        this._unitOfWork = unitOfWork;
        this._identityRepository = identityRepository;
    }

    public async Task<BaseResponseModel<CreateTopicCategoryResDTO>> CreateTopicCategory(CreateTopicCategoryDTO createTopicCategoryDTO, int userId)
    {
        try
        {
            var user = await _identityRepository.GetByIdAsync((long)userId);
            if (user == null)
            {
                return new BaseResponseModel<CreateTopicCategoryResDTO>
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status404NotFound,
                    Message = "Người dùng không tồn tại"
                };
            }

            var topicCategoryRepo = _unitOfWork.GetRepo<TopicCategory>();

            var existingCategory = await topicCategoryRepo.GetSingleAsync(new QueryBuilder<TopicCategory>()
                .WithPredicate(x => x.Name.ToLower() == createTopicCategoryDTO.Name.ToLower().Trim() && x.IsActive && x.DeletedAt == null)
                .WithTracking(false)
                .Build());

            if (existingCategory != null)
            {
                return new BaseResponseModel<CreateTopicCategoryResDTO>
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status409Conflict,
                    Message = "Tên danh mục chủ đề đã tồn tại"
                };
            }

            var topicCategory = createTopicCategoryDTO.GetEntity();
            topicCategory.CreatedBy = user.UserName;
            topicCategory.CreatedAt = DateTime.Now;

            await topicCategoryRepo.CreateAsync(topicCategory);
            await _unitOfWork.SaveChangesAsync();

            return new BaseResponseModel<CreateTopicCategoryResDTO>
            {
                Data = new CreateTopicCategoryResDTO(topicCategory),
                IsSuccess = true,
                StatusCode = StatusCodes.Status201Created
            };
        }
        catch (System.Exception)
        {
            throw;
        }
    }

    public async Task<BaseResponseModel<List<TopicCategoryOverviewResDTO>>> GetAllTopicCategory()
    {
        try
        {
            var topicCategoryRepo = _unitOfWork.GetRepo<TopicCategory>();
            var topicCategories = await topicCategoryRepo.GetAllAsync(new QueryBuilder<TopicCategory>()
                .WithPredicate(x => x.IsActive && x.DeletedAt == null)
                .WithInclude(x => x.Topics)
                .WithTracking(false)
                .Build());

            return new BaseResponseModel<List<TopicCategoryOverviewResDTO>>
            {
                Data = topicCategories.Select(x => new TopicCategoryOverviewResDTO(x)).ToList(),
                IsSuccess = true,
                StatusCode = StatusCodes.Status200OK
            };
        }
        catch (System.Exception)
        {
            throw;
        }
    }

    public async Task<BaseResponseModel<UpdateTopicCategoryResDTO>> UpdateTopicCategory(UpdateTopicCategoryDTO updateTopicCategoryDTO, int userId)
    {
        try
        {
            var user = await _identityRepository.GetByIdAsync((long)userId);
            if (user == null)
            {
                return new BaseResponseModel<UpdateTopicCategoryResDTO>
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status404NotFound,
                    Message = "Người dùng không tồn tại"
                };
            }

            var topicCategoryRepo = _unitOfWork.GetRepo<TopicCategory>();
            var topicCategory = await topicCategoryRepo.GetSingleAsync(new QueryBuilder<TopicCategory>()
                .WithPredicate(x => x.Id == updateTopicCategoryDTO.Id && x.IsActive && x.DeletedAt == null)
                .WithTracking(true)
                .Build());

            if (topicCategory == null)
            {
                return new BaseResponseModel<UpdateTopicCategoryResDTO>
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status404NotFound,
                    Message = "Danh mục chủ đề không tồn tại"
                };
            }

            var existingCategory = await topicCategoryRepo.GetSingleAsync(new QueryBuilder<TopicCategory>()
                .WithPredicate(x => x.Name.ToLower() == updateTopicCategoryDTO.Name.ToLower().Trim()
                    && x.Id != updateTopicCategoryDTO.Id && x.IsActive && x.DeletedAt == null)
                .WithTracking(false)
                .Build());

            if (existingCategory != null)
            {
                return new BaseResponseModel<UpdateTopicCategoryResDTO>
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status409Conflict,
                    Message = "Tên danh mục chủ đề đã tồn tại"
                };
            }

            topicCategory.Name = updateTopicCategoryDTO.Name.Trim();
            topicCategory.Description = updateTopicCategoryDTO.Description?.Trim();
            topicCategory.LastModifiedBy = user.UserName;
            topicCategory.LastModifiedAt = DateTime.Now;

            await topicCategoryRepo.UpdateAsync(topicCategory);
            await _unitOfWork.SaveChangesAsync();

            return new BaseResponseModel<UpdateTopicCategoryResDTO>
            {
                Data = new UpdateTopicCategoryResDTO(topicCategory),
                IsSuccess = true,
                StatusCode = StatusCodes.Status200OK
            };
        }
        catch (System.Exception)
        {
            throw;
        }
    }

    public async Task<BaseResponseModel<TopicCategoryDetailDTO>> GetTopicCategoryDetail(int topicCategoryId)
    {
        try
        {
            var topicCategoryRepo = _unitOfWork.GetRepo<TopicCategory>();
            var topicCategory = await topicCategoryRepo.GetSingleAsync(new QueryBuilder<TopicCategory>()
                .WithPredicate(x => x.Id == topicCategoryId && x.IsActive && x.DeletedAt == null)
                .WithInclude(x => x.Topics)
                .WithTracking(false)
                .Build());

            if (topicCategory == null)
            {
                return new BaseResponseModel<TopicCategoryDetailDTO>
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status404NotFound,
                    Message = "Danh mục chủ đề không tồn tại"
                };
            }

            return new BaseResponseModel<TopicCategoryDetailDTO>
            {
                Data = new TopicCategoryDetailDTO(topicCategory),
                IsSuccess = true,
                StatusCode = StatusCodes.Status200OK
            };
        }
        catch (System.Exception)
        {
            throw;
        }
    }

    public async Task<BaseResponseModel> DeleteTopicCategory(int topicCategoryId)
    {
        try
        {
            var topicCategoryRepo = _unitOfWork.GetRepo<TopicCategory>();
            var topicCategory = await topicCategoryRepo.GetSingleAsync(new QueryBuilder<TopicCategory>()
                .WithPredicate(x => x.Id == topicCategoryId && x.IsActive && x.DeletedAt == null)
                .WithInclude(x => x.Topics)
                .WithTracking(true)
                .Build());

            if (topicCategory == null)
            {
                return new BaseResponseModel
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status404NotFound,
                    Message = "Danh mục chủ đề không tồn tại"
                };
            }

            if (topicCategory.Topics.Any(t => t.IsActive && t.DeletedAt == null))
            {
                return new BaseResponseModel
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status409Conflict,
                    Message = "Không thể xóa danh mục chủ đề đang có chủ đề sử dụng"
                };
            }

            topicCategory.IsActive = false;
            topicCategory.DeletedAt = DateTime.Now;

            await topicCategoryRepo.UpdateAsync(topicCategory);
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
