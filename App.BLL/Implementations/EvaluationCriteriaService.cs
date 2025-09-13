using AutoMapper;
using App.BLL.Interfaces;
using App.Commons.ResponseModel;
using App.Commons.Paging;
using App.DAL.UnitOfWork;
using App.DAL.Queries;
using App.Entities.DTOs.EvaluationCriteria;
using App.Entities.Entities.App;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace App.BLL.Implementations;

public class EvaluationCriteriaService : IEvaluationCriteriaService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public EvaluationCriteriaService(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<BaseResponseModel<EvaluationCriteriaResponseDTO>> CreateAsync(
        CreateEvaluationCriteriaDTO createDTO)
    {
        try
        {
            var repo = _unitOfWork.GetRepo<EvaluationCriteria>();


            if (createDTO.SemesterId.HasValue)
            {
                var semesterRepo = _unitOfWork.GetRepo<Semester>();
                var semesterExists = await semesterRepo.AnyAsync(new QueryOptions<Semester>
                {
                    Predicate = x => x.Id == createDTO.SemesterId.Value && x.IsActive
                });

                if (!semesterExists)
                {
                    return new BaseResponseModel<EvaluationCriteriaResponseDTO>
                    {
                        IsSuccess = false,
                        StatusCode = StatusCodes.Status404NotFound,
                        Message = "Học kỳ không tồn tại"
                    };
                }
            }


            var existingCriteria = await repo.GetSingleAsync(new QueryOptions<EvaluationCriteria>
            {
                Predicate = x => x.Name.ToLower() == createDTO.Name.ToLower() &&
                                 x.IsActive &&
                                 x.SemesterId == createDTO.SemesterId, // So sánh cả SemesterId
                Tracked = false
            });

            if (existingCriteria != null)
            {
                var semesterInfo = createDTO.SemesterId.HasValue ? $" trong học kỳ này" : " (áp dụng chung)";
                return new BaseResponseModel<EvaluationCriteriaResponseDTO>
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status409Conflict,
                    Message = $"Tên tiêu chí đánh giá đã tồn tại{semesterInfo}"
                };
            }

            var criteria = _mapper.Map<EvaluationCriteria>(createDTO);
            criteria.CreatedAt = DateTime.UtcNow;
            criteria.IsActive = true;

            await repo.CreateAsync(criteria);
            var result = await _unitOfWork.SaveAsync();

            if (!result.IsSuccess)
            {
                return new BaseResponseModel<EvaluationCriteriaResponseDTO>
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status500InternalServerError,
                    Message = result.Message
                };
            }

            var responseDTO = _mapper.Map<EvaluationCriteriaResponseDTO>(criteria);
            return new BaseResponseModel<EvaluationCriteriaResponseDTO>
            {
                IsSuccess = true,
                StatusCode = StatusCodes.Status201Created,
                Message = "Tạo tiêu chí đánh giá thành công",
                Data = responseDTO
            };
        }
        catch (Exception ex)
        {
            return new BaseResponseModel<EvaluationCriteriaResponseDTO>
            {
                IsSuccess = false,
                StatusCode = StatusCodes.Status500InternalServerError,
                Message = $"Lỗi hệ thống: {ex.Message}"
            };
        }
    }


    public async Task<BaseResponseModel<List<EvaluationCriteriaResponseDTO>>> GetBySemesterAsync(int? semesterId)
    {
        try
        {
            var repo = _unitOfWork.GetRepo<EvaluationCriteria>();

            var criteria = await repo.GetAllAsync(new QueryOptions<EvaluationCriteria>
            {
                Predicate = x => x.IsActive &&
                                 (x.SemesterId == semesterId || (semesterId == null && x.SemesterId == null)),
                Tracked = false,
                OrderBy = q => q.OrderBy(x => x.Name)
            });

            var responseItems = _mapper.Map<List<EvaluationCriteriaResponseDTO>>(criteria);

            return new BaseResponseModel<List<EvaluationCriteriaResponseDTO>>
            {
                IsSuccess = true,
                StatusCode = StatusCodes.Status200OK,
                Data = responseItems
            };
        }
        catch (Exception ex)
        {
            return new BaseResponseModel<List<EvaluationCriteriaResponseDTO>>
            {
                IsSuccess = false,
                StatusCode = StatusCodes.Status500InternalServerError,
                Message = $"Lỗi hệ thống: {ex.Message}"
            };
        }
    }

    // THÊM method này vào class EvaluationCriteriaService

    public async Task<BaseResponseModel<List<EvaluationCriteriaResponseDTO>>> GetCurrentSemesterCriteriaAsync()
    {
        try
        {
            // 1. Lấy semester hiện tại
            var semesterRepo = _unitOfWork.GetRepo<Semester>();
            var currentDate = DateTime.Now;

            var currentSemester = await semesterRepo.GetSingleAsync(new QueryOptions<Semester>
            {
                Predicate = x => x.IsActive &&
                               x.DeletedAt == null &&
                               x.StartDate <= currentDate &&
                               x.EndDate >= currentDate,
                Tracked = false
            });

            // 2. Lấy criteria theo semester hiện tại
            var criteriaRepo = _unitOfWork.GetRepo<EvaluationCriteria>();

            IEnumerable<EvaluationCriteria> criteriaEnumerable; // ✅ Đổi thành IEnumerable

            if (currentSemester != null)
            {
                // Có semester hiện tại -> lấy criteria của semester này + criteria chung
                criteriaEnumerable = await criteriaRepo.GetAllAsync(new QueryOptions<EvaluationCriteria>
                {
                    Predicate = x => x.IsActive &&
                                   (x.SemesterId == currentSemester.Id || x.SemesterId == null),
                    Tracked = false,
                    OrderBy = q => q.OrderBy(x => x.Name)
                });
            }
            else
            {
                // Không có semester hiện tại -> chỉ lấy criteria chung
                criteriaEnumerable = await criteriaRepo.GetAllAsync(new QueryOptions<EvaluationCriteria>
                {
                    Predicate = x => x.IsActive && x.SemesterId == null,
                    Tracked = false,
                    OrderBy = q => q.OrderBy(x => x.Name)
                });
            }

            // ✅ Convert sang List nếu cần
            var criteria = criteriaEnumerable.ToList();

            var responseItems = _mapper.Map<List<EvaluationCriteriaResponseDTO>>(criteria);

            var message = currentSemester != null
                ? $"Lấy tiêu chí đánh giá của học kỳ '{currentSemester.Name}' thành công"
                : "Lấy tiêu chí đánh giá chung thành công (không có học kỳ hiện tại)";

            return new BaseResponseModel<List<EvaluationCriteriaResponseDTO>>
            {
                IsSuccess = true,
                StatusCode = StatusCodes.Status200OK,
                Message = message,
                Data = responseItems
            };
        }
        catch (Exception ex)
        {
            return new BaseResponseModel<List<EvaluationCriteriaResponseDTO>>
            {
                IsSuccess = false,
                StatusCode = StatusCodes.Status500InternalServerError,
                Message = $"Lỗi hệ thống: {ex.Message}"
            };
        }
    }
    public async Task<BaseResponseModel<EvaluationCriteriaResponseDTO>> UpdateAsync(
        UpdateEvaluationCriteriaDTO updateDTO)
    {
        try
        {
            var repo = _unitOfWork.GetRepo<EvaluationCriteria>();

            var criteria = await repo.GetSingleAsync(new QueryOptions<EvaluationCriteria>
            {
                Predicate = x => x.Id == updateDTO.Id && x.IsActive
            });

            if (criteria == null)
            {
                return new BaseResponseModel<EvaluationCriteriaResponseDTO>
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status404NotFound,
                    Message = "Không tìm thấy tiêu chí đánh giá"
                };
            }

            // Kiểm tra tên tiêu chí đã tồn tại chưa (trừ chính nó)
            var existingCriteria = await repo.GetSingleAsync(new QueryOptions<EvaluationCriteria>
            {
                Predicate = x => x.Name.ToLower() == updateDTO.Name.ToLower() && x.Id != updateDTO.Id && x.IsActive,
                Tracked = false
            });

            if (existingCriteria != null)
            {
                return new BaseResponseModel<EvaluationCriteriaResponseDTO>
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status409Conflict,
                    Message = "Tên tiêu chí đánh giá đã tồn tại"
                };
            }

            _mapper.Map(updateDTO, criteria);
            criteria.LastModifiedAt = DateTime.UtcNow;

            await repo.UpdateAsync(criteria);
            var result = await _unitOfWork.SaveAsync();

            if (!result.IsSuccess)
            {
                return new BaseResponseModel<EvaluationCriteriaResponseDTO>
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status500InternalServerError,
                    Message = result.Message
                };
            }

            var responseDTO = _mapper.Map<EvaluationCriteriaResponseDTO>(criteria);
            return new BaseResponseModel<EvaluationCriteriaResponseDTO>
            {
                IsSuccess = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Cập nhật tiêu chí đánh giá thành công",
                Data = responseDTO
            };
        }
        catch (Exception ex)
        {
            return new BaseResponseModel<EvaluationCriteriaResponseDTO>
            {
                IsSuccess = false,
                StatusCode = StatusCodes.Status500InternalServerError,
                Message = $"Lỗi hệ thống: {ex.Message}"
            };
        }
    }

    public async Task<BaseResponseModel> DeleteAsync(int id)
    {
        try
        {
            var repo = _unitOfWork.GetRepo<EvaluationCriteria>();

            var criteria = await repo.GetSingleAsync(new QueryOptions<EvaluationCriteria>
            {
                Predicate = x => x.Id == id && x.IsActive
            });

            if (criteria == null)
            {
                return new BaseResponseModel
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status404NotFound,
                    Message = "Không tìm thấy tiêu chí đánh giá"
                };
            }

            // Kiểm tra xem tiêu chí có đang được sử dụng không
            var scoreRepo = _unitOfWork.GetRepo<ReviewCriteriaScore>();
            var isUsed = await scoreRepo.AnyAsync(new QueryOptions<ReviewCriteriaScore>
            {
                Predicate = x => x.CriteriaId == id && x.IsActive
            });

            if (isUsed)
            {
                return new BaseResponseModel
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status409Conflict,
                    Message = "Không thể xóa tiêu chí đang được sử dụng trong các đánh giá"
                };
            }

            // Soft delete
            criteria.IsActive = false;
            criteria.DeletedAt = DateTime.UtcNow;
            criteria.LastModifiedAt = DateTime.UtcNow;

            await repo.UpdateAsync(criteria);
            var result = await _unitOfWork.SaveAsync();

            if (!result.IsSuccess)
            {
                return new BaseResponseModel
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status500InternalServerError,
                    Message = result.Message
                };
            }

            return new BaseResponseModel
            {
                IsSuccess = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Xóa tiêu chí đánh giá thành công"
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

    public async Task<BaseResponseModel<EvaluationCriteriaResponseDTO>> GetByIdAsync(int id)
    {
        try
        {
            var repo = _unitOfWork.GetRepo<EvaluationCriteria>();

            var criteria = await repo.GetSingleAsync(new QueryOptions<EvaluationCriteria>
            {
                Predicate = x => x.Id == id && x.IsActive,
                Tracked = false
            });

            if (criteria == null)
            {
                return new BaseResponseModel<EvaluationCriteriaResponseDTO>
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status404NotFound,
                    Message = "Không tìm thấy tiêu chí đánh giá"
                };
            }

            var responseDTO = _mapper.Map<EvaluationCriteriaResponseDTO>(criteria);
            return new BaseResponseModel<EvaluationCriteriaResponseDTO>
            {
                IsSuccess = true,
                StatusCode = StatusCodes.Status200OK,
                Data = responseDTO
            };
        }
        catch (Exception ex)
        {
            return new BaseResponseModel<EvaluationCriteriaResponseDTO>
            {
                IsSuccess = false,
                StatusCode = StatusCodes.Status500InternalServerError,
                Message = $"Lỗi hệ thống: {ex.Message}"
            };
        }
    }

    public async Task<BaseResponseModel<PagingDataModel<EvaluationCriteriaResponseDTO>>> GetAllAsync(
        PagingModel pagingModel)
    {
        try
        {
            var repo = _unitOfWork.GetRepo<EvaluationCriteria>();

            var query = repo.Get(new QueryOptions<EvaluationCriteria>
            {
                Predicate = x => x.IsActive,
                Tracked = false,
                OrderBy = q => q.OrderBy(x => x.CreatedAt)
            });

            var totalItems = await query.CountAsync();
            var items = await query
                .Skip((pagingModel.PageNumber - 1) * pagingModel.PageSize)
                .Take(pagingModel.PageSize)
                .ToListAsync();

            var responseItems = _mapper.Map<List<EvaluationCriteriaResponseDTO>>(items);

            pagingModel.TotalRecord = totalItems;
            var pagingData = new PagingDataModel<EvaluationCriteriaResponseDTO>(responseItems, pagingModel);

            return new BaseResponseModel<PagingDataModel<EvaluationCriteriaResponseDTO>>
            {
                IsSuccess = true,
                StatusCode = StatusCodes.Status200OK,
                Data = pagingData
            };
        }
        catch (Exception ex)
        {
            return new BaseResponseModel<PagingDataModel<EvaluationCriteriaResponseDTO>>
            {
                IsSuccess = false,
                StatusCode = StatusCodes.Status500InternalServerError,
                Message = $"Lỗi hệ thống: {ex.Message}"
            };
        }
    }

    public async Task<BaseResponseModel<List<EvaluationCriteriaResponseDTO>>> GetAllActiveAsync()
    {
        try
        {
            var repo = _unitOfWork.GetRepo<EvaluationCriteria>();

            var criteria = await repo.GetAllAsync(new QueryOptions<EvaluationCriteria>
            {
                Predicate = x => x.IsActive,
                Tracked = false,
                OrderBy = q => q.OrderBy(x => x.Name)
            });

            var responseItems = _mapper.Map<List<EvaluationCriteriaResponseDTO>>(criteria);

            return new BaseResponseModel<List<EvaluationCriteriaResponseDTO>>
            {
                IsSuccess = true,
                StatusCode = StatusCodes.Status200OK,
                Data = responseItems
            };
        }
        catch (Exception ex)
        {
            return new BaseResponseModel<List<EvaluationCriteriaResponseDTO>>
            {
                IsSuccess = false,
                StatusCode = StatusCodes.Status500InternalServerError,
                Message = $"Lỗi hệ thống: {ex.Message}"
            };
        }
    }
}