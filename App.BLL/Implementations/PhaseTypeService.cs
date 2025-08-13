using System.Linq.Expressions;
using App.BLL.Interfaces;
using App.Commons.Extensions;
using App.Commons.Paging;
using App.Commons.ResponseModel;
using App.DAL.Interfaces;
using App.DAL.Queries.Implementations;
using App.DAL.UnitOfWork;
using App.Entities.DTOs.PhaseTypes;
using App.Entities.Entities.App;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace App.BLL.Implementations;

public class PhaseTypeService : IPhaseTypeService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IIdentityRepository _identityRepository;

    public PhaseTypeService(IUnitOfWork unitOfWork, IIdentityRepository identityRepository)
    {
        _unitOfWork = unitOfWork;
        _identityRepository = identityRepository;
    }

    public async Task<BaseResponseModel<CreatePhaseTypeResDTO>> CreatePhaseType(CreatePhaseTypeDTO createPhaseTypeDTO, int userId)
    {
        try
        {
            var user = await _identityRepository.GetByIdAsync((long)userId);
            if (user == null)
            {
                return new BaseResponseModel<CreatePhaseTypeResDTO>
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status404NotFound,
                    Message = "Người dùng không tồn tại"
                };
            }

            var phaseTypeRepo = _unitOfWork.GetRepo<PhaseType>();

            // Kiểm tra tên loại giai đoạn đã tồn tại chưa
            var existingPhaseType = await phaseTypeRepo.GetSingleAsync(new QueryBuilder<PhaseType>()
                .WithPredicate(x => x.Name.ToLower() == createPhaseTypeDTO.Name.ToLower() && x.IsActive && x.DeletedAt == null)
                .WithTracking(false)
                .Build());

            if (existingPhaseType != null)
            {
                return new BaseResponseModel<CreatePhaseTypeResDTO>
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status409Conflict,
                    Message = "Tên loại giai đoạn đã tồn tại"
                };
            }

            var phaseType = createPhaseTypeDTO.GetEntity();
            phaseType.CreatedBy = user.UserName;
            phaseType.CreatedAt = DateTime.Now;

            await phaseTypeRepo.CreateAsync(phaseType);
            await _unitOfWork.SaveChangesAsync();

            return new BaseResponseModel<CreatePhaseTypeResDTO>
            {
                Data = new CreatePhaseTypeResDTO(phaseType),
                IsSuccess = true,
                StatusCode = StatusCodes.Status201Created,
                Message = "Tạo loại giai đoạn thành công"
            };
        }
        catch (Exception)
        {
            throw;
        }
    }

    public async Task<BaseResponseModel<List<PhaseTypeOverviewResDTO>>> GetAllPhaseTypes()
    {
        try
        {
            var phaseTypeRepo = _unitOfWork.GetRepo<PhaseType>();
            var phaseTypes = await phaseTypeRepo.GetAllAsync(new QueryBuilder<PhaseType>()
                .WithPredicate(x => x.IsActive && x.DeletedAt == null)
                .WithTracking(false)
                .Build());

            return new BaseResponseModel<List<PhaseTypeOverviewResDTO>>
            {
                Data = phaseTypes.Select(x => new PhaseTypeOverviewResDTO(x)).ToList(),
                IsSuccess = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Lấy danh sách loại giai đoạn thành công"
            };
        }
        catch (Exception)
        {
            throw;
        }
    }

    public async Task<BaseResponseModel<PagingDataModel<PhaseTypeOverviewResDTO, GetPhaseTypesQueryDTO>>> GetPhaseTypes(GetPhaseTypesQueryDTO getPhaseTypesQueryDTO)
    {
        try
        {
            var phaseTypeRepo = _unitOfWork.GetRepo<PhaseType>();

            Expression<Func<PhaseType, bool>> predicate = x => x.IsActive && x.DeletedAt == null;

            if (!string.IsNullOrEmpty(getPhaseTypesQueryDTO.Keyword))
            {
                predicate = x => x.IsActive && x.DeletedAt == null && x.Name.Contains(getPhaseTypesQueryDTO.Keyword);
            }

            var query = new QueryBuilder<PhaseType>()
                .WithPredicate(predicate)
                .WithTracking(false);

            var phaseTypes = phaseTypeRepo.Get(query.Build());

            getPhaseTypesQueryDTO.TotalRecord = await phaseTypes.CountAsync();

            var listTypes = await phaseTypes.ToPagedList(getPhaseTypesQueryDTO.PageNumber, getPhaseTypesQueryDTO.PageSize).ToListAsync();

            return new BaseResponseModel<PagingDataModel<PhaseTypeOverviewResDTO, GetPhaseTypesQueryDTO>>
            {
                Data = new PagingDataModel<PhaseTypeOverviewResDTO, GetPhaseTypesQueryDTO>(listTypes.Select(x => new PhaseTypeOverviewResDTO(x)).ToList(), getPhaseTypesQueryDTO),
                IsSuccess = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Lấy danh sách loại giai đoạn thành công"
            };
        }
        catch (Exception)
        {
            throw;
        }
    }

    public async Task<BaseResponseModel<UpdatePhaseTypeResDTO>> UpdatePhaseType(UpdatePhaseTypeDTO updatePhaseTypeDTO, int userId)
    {
        try
        {
            var user = await _identityRepository.GetByIdAsync((long)userId);
            if (user == null)
            {
                return new BaseResponseModel<UpdatePhaseTypeResDTO>
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status404NotFound,
                    Message = "Người dùng không tồn tại"
                };
            }

            var phaseTypeRepo = _unitOfWork.GetRepo<PhaseType>();
            var phaseType = await phaseTypeRepo.GetSingleAsync(new QueryBuilder<PhaseType>()
                .WithPredicate(x => x.Id == updatePhaseTypeDTO.Id && x.IsActive && x.DeletedAt == null)
                .WithTracking(false)
                .Build());

            if (phaseType == null)
            {
                return new BaseResponseModel<UpdatePhaseTypeResDTO>
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status404NotFound,
                    Message = "Loại giai đoạn không tồn tại"
                };
            }

            // Kiểm tra tên loại giai đoạn đã tồn tại chưa (trừ chính nó)
            var existingPhaseType = await phaseTypeRepo.GetSingleAsync(new QueryBuilder<PhaseType>()
                .WithPredicate(x => x.Name.ToLower() == updatePhaseTypeDTO.Name.ToLower() &&
                                   x.Id != updatePhaseTypeDTO.Id &&
                                   x.IsActive &&
                                   x.DeletedAt == null)
                .WithTracking(false)
                .Build());

            if (existingPhaseType != null)
            {
                return new BaseResponseModel<UpdatePhaseTypeResDTO>
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status409Conflict,
                    Message = "Tên loại giai đoạn đã tồn tại"
                };
            }

            phaseType.Name = updatePhaseTypeDTO.Name;
            phaseType.Description = updatePhaseTypeDTO.Description;
            phaseType.LastModifiedBy = user.UserName;
            phaseType.LastModifiedAt = DateTime.Now;

            await phaseTypeRepo.UpdateAsync(phaseType);
            await _unitOfWork.SaveChangesAsync();

            return new BaseResponseModel<UpdatePhaseTypeResDTO>
            {
                Data = new UpdatePhaseTypeResDTO(phaseType),
                IsSuccess = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Cập nhật loại giai đoạn thành công"
            };
        }
        catch (Exception)
        {
            throw;
        }
    }

    public async Task<BaseResponseModel<PhaseTypeDetailDTO>> GetPhaseTypeDetail(int phaseTypeId)
    {
        try
        {
            var phaseTypeRepo = _unitOfWork.GetRepo<PhaseType>();
            var phaseType = await phaseTypeRepo.GetSingleAsync(new QueryBuilder<PhaseType>()
                .WithPredicate(x => x.Id == phaseTypeId && x.IsActive && x.DeletedAt == null)
                .WithTracking(false)
                .Build());

            if (phaseType == null)
            {
                return new BaseResponseModel<PhaseTypeDetailDTO>
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status404NotFound,
                    Message = "Loại giai đoạn không tồn tại"
                };
            }

            return new BaseResponseModel<PhaseTypeDetailDTO>
            {
                Data = new PhaseTypeDetailDTO(phaseType),
                IsSuccess = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Lấy chi tiết loại giai đoạn thành công"
            };
        }
        catch (Exception)
        {
            throw;
        }
    }

    public async Task<BaseResponseModel> DeletePhaseType(int phaseTypeId)
    {
        try
        {
            var phaseTypeRepo = _unitOfWork.GetRepo<PhaseType>();
            var phaseType = await phaseTypeRepo.GetSingleAsync(new QueryBuilder<PhaseType>()
                .WithPredicate(x => x.Id == phaseTypeId && x.IsActive && x.DeletedAt == null)
                .WithTracking(false)
                .Build());

            if (phaseType == null)
            {
                return new BaseResponseModel
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status404NotFound,
                    Message = "Loại giai đoạn không tồn tại"
                };
            }

            // Kiểm tra xem có Phase nào đang sử dụng PhaseType này không
            var phaseRepo = _unitOfWork.GetRepo<Phase>();
            var hasRelatedPhases = await phaseRepo.GetSingleAsync(new QueryBuilder<Phase>()
                .WithPredicate(x => x.PhaseTypeId == phaseTypeId && x.IsActive && x.DeletedAt == null)
                .WithTracking(false)
                .Build());

            if (hasRelatedPhases != null)
            {
                return new BaseResponseModel
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status409Conflict,
                    Message = "Không thể xóa loại giai đoạn này vì đang được sử dụng trong các giai đoạn khác"
                };
            }

            phaseType.IsActive = false;
            phaseType.DeletedAt = DateTime.Now;

            await phaseTypeRepo.UpdateAsync(phaseType);
            await _unitOfWork.SaveChangesAsync();

            return new BaseResponseModel
            {
                IsSuccess = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Xóa loại giai đoạn thành công"
            };
        }
        catch (Exception)
        {
            throw;
        }
    }
}
