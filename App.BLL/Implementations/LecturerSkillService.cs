// App.BLL/Implementations/LecturerSkillService.cs
using AutoMapper;
using App.BLL.Interfaces;
using App.Commons.Paging;
using App.Commons.ResponseModel;
using App.DAL.Queries;
using App.DAL.UnitOfWork;
using App.Entities.DTOs.LecturerSkills;
using App.Entities.Entities.App;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using App.DAL.Interfaces;
using Microsoft.Extensions.Logging;

namespace App.BLL.Implementations;

public class LecturerSkillService : ILecturerSkillService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly IIdentityRepository _identityRepository;

    private readonly ILogger<LecturerSkillService> _logger;


    public LecturerSkillService(IUnitOfWork unitOfWork, IMapper mapper, IIdentityRepository identityRepository, ILogger<LecturerSkillService> logger)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _identityRepository = identityRepository;
        _logger = logger;
    }

    public async Task<BaseResponseModel<LecturerSkillResponseDTO>> CreateAsync(CreateLecturerSkillDTO dto, int userId, bool isAdmin)
    {
        try
        {

            var user = await _identityRepository.GetByIdAsync(userId);
            if (user is null) return new BaseResponseModel<LecturerSkillResponseDTO>
            {
                IsSuccess = false,
                StatusCode = StatusCodes.Status404NotFound,
                Message = "Người dùng không tồn tại"
            };

            var repo = _unitOfWork.GetRepo<LecturerSkill>();

            // Xác định lecturerId hợp lệ
            int targetLecturerId;
            if (isAdmin && dto.LecturerId.HasValue && dto.LecturerId > 0)
            {
                targetLecturerId = dto.LecturerId.Value;
            }
            else
            {
                targetLecturerId = userId;
            }

            _logger.LogInformation($"UserId: {userId}");

            // Non-admin không được tạo cho người khác
            if (!isAdmin && targetLecturerId != userId)
            {
                return new BaseResponseModel<LecturerSkillResponseDTO>
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status403Forbidden,
                    Message = "Bạn không có quyền tạo kỹ năng cho giảng viên khác"
                };
            }

            _logger.LogInformation($"Target lecturerId: {targetLecturerId} - isAdmin: {isAdmin}");

            // Check trùng (LecturerId + SkillTag)
            var existed = await repo.GetSingleAsync(new QueryOptions<LecturerSkill>
            {
                Predicate = x => x.IsActive
                                 && x.LecturerId == targetLecturerId
                                 && x.SkillTag.ToLower() == dto.SkillTag.ToLower(),
                Tracked = false
            });
            if (existed != null)
            {
                return new BaseResponseModel<LecturerSkillResponseDTO>
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status409Conflict,
                    Message = "Kỹ năng đã tồn tại cho giảng viên này"
                };
            }

            var entity = dto.GetEntity();
            entity.LecturerId = targetLecturerId;
            entity.CreatedBy = user.UserName;
            entity.CreatedAt = DateTime.Now;

            await repo.CreateAsync(entity);
            var save = await _unitOfWork.SaveAsync();
            if (!save.IsSuccess)
            {
                return new BaseResponseModel<LecturerSkillResponseDTO>
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status500InternalServerError,
                    Message = save.Message
                };
            }

            var res = new LecturerSkillResponseDTO(entity);
            return new BaseResponseModel<LecturerSkillResponseDTO>
            {
                IsSuccess = true,
                StatusCode = StatusCodes.Status201Created,
                Message = "Tạo kỹ năng thành công",
                Data = res
            };
        }
        catch (Exception ex)
        {
            return new BaseResponseModel<LecturerSkillResponseDTO>
            {
                IsSuccess = false,
                StatusCode = StatusCodes.Status500InternalServerError,
                Message = $"Lỗi hệ thống: {ex.Message}"
            };
        }
    }

    public async Task<BaseResponseModel<LecturerSkillResponseDTO>> UpdateAsync(UpdateLecturerSkillDTO dto, int userId, bool isAdmin)
    {
        try
        {
            var user = await _identityRepository.GetByIdAsync(userId);
            if (user is null) return new BaseResponseModel<LecturerSkillResponseDTO>
            {
                IsSuccess = false,
                StatusCode = StatusCodes.Status404NotFound,
                Message = "Người dùng không tồn tại"
            };
            var repo = _unitOfWork.GetRepo<LecturerSkill>();
            var entity = await repo.GetSingleAsync(new QueryOptions<LecturerSkill>
            {
                Predicate = x => x.Id == dto.Id && x.IsActive
            });

            if (entity == null)
            {
                return new BaseResponseModel<LecturerSkillResponseDTO>
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status404NotFound,
                    Message = "Không tìm thấy kỹ năng"
                };
            }

            // Phân quyền: admin hoặc chủ sở hữu skill
            if (!isAdmin && entity.LecturerId != userId)
            {
                return new BaseResponseModel<LecturerSkillResponseDTO>
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status403Forbidden,
                    Message = "Bạn không có quyền cập nhật kỹ năng này"
                };
            }

            // Check trùng SkillTag (bỏ qua chính nó)
            var dup = await repo.GetSingleAsync(new QueryOptions<LecturerSkill>
            {
                Predicate = x => x.IsActive
                                 && x.LecturerId == entity.LecturerId
                                 && x.SkillTag.ToLower() == dto.SkillTag.ToLower()
                                 && x.Id != entity.Id,
                Tracked = false
            });
            if (dup != null)
            {
                return new BaseResponseModel<LecturerSkillResponseDTO>
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status409Conflict,
                    Message = "Kỹ năng đã tồn tại cho giảng viên này"
                };
            }

            entity.SkillTag = dto.SkillTag;
            entity.ProficiencyLevel = dto.ProficiencyLevel;
            entity.LastModifiedBy = user.UserName;
            entity.LastModifiedAt = DateTime.Now;

            await repo.UpdateAsync(entity);
            var save = await _unitOfWork.SaveAsync();
            if (!save.IsSuccess)
            {
                return new BaseResponseModel<LecturerSkillResponseDTO>
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status500InternalServerError,
                    Message = save.Message
                };
            }

            var res = new LecturerSkillResponseDTO(entity);
            return new BaseResponseModel<LecturerSkillResponseDTO>
            {
                IsSuccess = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Cập nhật kỹ năng thành công",
                Data = res
            };
        }
        catch (Exception ex)
        {
            return new BaseResponseModel<LecturerSkillResponseDTO>
            {
                IsSuccess = false,
                StatusCode = StatusCodes.Status500InternalServerError,
                Message = $"Lỗi hệ thống: {ex.Message}"
            };
        }
    }

    public async Task<BaseResponseModel> DeleteAsync(int id, int userId, bool isAdmin)
    {
        try
        {
            var user = await _identityRepository.GetByIdAsync(userId);
            if (user is null) return new BaseResponseModel
            {
                IsSuccess = false,
                StatusCode = StatusCodes.Status404NotFound,
                Message = "Người dùng không tồn tại"
            };
            var repo = _unitOfWork.GetRepo<LecturerSkill>();
            var entity = await repo.GetSingleAsync(new QueryOptions<LecturerSkill>
            {
                Predicate = x => x.Id == id && x.IsActive && x.DeletedAt == null
            });

            if (entity == null)
            {
                return new BaseResponseModel
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status404NotFound,
                    Message = "Không tìm thấy kỹ năng"
                };
            }

            if (!isAdmin && entity.LecturerId != userId)
            {
                return new BaseResponseModel
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status403Forbidden,
                    Message = "Bạn không có quyền xóa kỹ năng này"
                };
            }

            entity.IsActive = false;
            entity.DeletedAt = DateTime.Now;
            entity.LastModifiedBy = user.UserName;

            await repo.UpdateAsync(entity);
            var save = await _unitOfWork.SaveAsync();
            if (!save.IsSuccess)
            {
                return new BaseResponseModel
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status500InternalServerError,
                    Message = save.Message
                };
            }

            return new BaseResponseModel
            {
                IsSuccess = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Xóa kỹ năng thành công"
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

    public async Task<BaseResponseModel<LecturerSkillResponseDTO>> GetByIdAsync(int id)
    {
        try
        {
            var repo = _unitOfWork.GetRepo<LecturerSkill>();
            var entity = await repo.GetSingleAsync(new QueryOptions<LecturerSkill>
            {
                Predicate = x => x.Id == id && x.IsActive && x.DeletedAt == null,
                Tracked = false
            });

            if (entity == null)
            {
                return new BaseResponseModel<LecturerSkillResponseDTO>
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status404NotFound,
                    Message = "Không tìm thấy kỹ năng"
                };
            }

            var res = new LecturerSkillResponseDTO(entity);
            return new BaseResponseModel<LecturerSkillResponseDTO>
            {
                IsSuccess = true,
                StatusCode = StatusCodes.Status200OK,
                Data = res
            };
        }
        catch (Exception ex)
        {
            return new BaseResponseModel<LecturerSkillResponseDTO>
            {
                IsSuccess = false,
                StatusCode = StatusCodes.Status500InternalServerError,
                Message = $"Lỗi hệ thống: {ex.Message}"
            };
        }
    }

    public async Task<BaseResponseModel<PagingDataModel<LecturerSkillResponseDTO>>> GetByLecturerAsync(int lecturerId, GetLecturerSkillQueryDTO dto)
    {
        try
        {
            var repo = _unitOfWork.GetRepo<LecturerSkill>();
            var query = repo.Get(new QueryOptions<LecturerSkill>
            {
                Predicate = x => x.IsActive && x.LecturerId == lecturerId && x.DeletedAt == null,
                Tracked = false,
                OrderBy = q => q.OrderByDescending(x => x.CreatedAt)
            });

            var total = await query.CountAsync();
            var items = await query
                .Skip((dto.PageNumber - 1) * dto.PageSize)
                .Take(dto.PageSize)
                .ToListAsync();

            var resItems = items.Select(x => new LecturerSkillResponseDTO(x)).ToList();
            dto.TotalRecord = total;
            var pagingData = new PagingDataModel<LecturerSkillResponseDTO>(resItems, dto);

            return new BaseResponseModel<PagingDataModel<LecturerSkillResponseDTO>>
            {
                IsSuccess = true,
                StatusCode = StatusCodes.Status200OK,
                Data = pagingData
            };
        }
        catch (Exception ex)
        {
            return new BaseResponseModel<PagingDataModel<LecturerSkillResponseDTO>>
            {
                IsSuccess = false,
                StatusCode = StatusCodes.Status500InternalServerError,
                Message = $"Lỗi hệ thống: {ex.Message}"
            };
        }
    }

    public Task<BaseResponseModel<PagingDataModel<LecturerSkillResponseDTO>>> GetMySkillsAsync(int userId, GetLecturerSkillQueryDTO dto)
    {
        return GetByLecturerAsync(userId, dto);
    }
}