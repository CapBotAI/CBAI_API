using System;
using System.Linq.Expressions;
using App.BLL.Interfaces;
using App.Commons.Extensions;
using App.Commons.Paging;
using App.Commons.ResponseModel;
using App.DAL.Interfaces;
using App.DAL.Queries.Implementations;
using App.DAL.UnitOfWork;
using App.Entities.DTOs.Phases;
using App.Entities.Entities.App;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace App.BLL.Implementations;

public class PhaseService : IPhaseService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IIdentityRepository _identityRepository;

    public PhaseService(IUnitOfWork unitOfWork, IIdentityRepository identityRepository)
    {
        _unitOfWork = unitOfWork;
        _identityRepository = identityRepository;
    }

    public async Task<BaseResponseModel<CreatePhaseResDTO>> CreatePhase(CreatePhaseDTO dto, int userId)
    {
        try
        {
            var user = await _identityRepository.GetByIdAsync((long)userId);
            if (user == null)
            {
                return new BaseResponseModel<CreatePhaseResDTO>
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status404NotFound,
                    Message = "Người dùng không tồn tại"
                };
            }

            // Validate Semester tồn tại
            var semesterRepo = _unitOfWork.GetRepo<Semester>();
            var semester = await semesterRepo.GetSingleAsync(new QueryBuilder<Semester>()
                .WithPredicate(x => x.Id == dto.SemesterId && x.IsActive && x.DeletedAt == null)
                .WithTracking(false)
                .Build());

            if (semester == null)
            {
                return new BaseResponseModel<CreatePhaseResDTO>
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status404NotFound,
                    Message = "Học kỳ không tồn tại"
                };
            }

            // Validate PhaseType tồn tại
            var phaseTypeRepo = _unitOfWork.GetRepo<PhaseType>();
            var phaseType = await phaseTypeRepo.GetSingleAsync(new QueryBuilder<PhaseType>()
                .WithPredicate(x => x.Id == dto.PhaseTypeId && x.IsActive && x.DeletedAt == null)
                .WithTracking(false)
                .Build());

            if (phaseType == null)
            {
                return new BaseResponseModel<CreatePhaseResDTO>
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status404NotFound,
                    Message = "Loại giai đoạn không tồn tại"
                };
            }

            // Optional: kiểm tra trùng tên trong cùng học kỳ (nếu muốn)
            var phaseRepo = _unitOfWork.GetRepo<Phase>();
            var duplicated = await phaseRepo.GetSingleAsync(new QueryBuilder<Phase>()
                .WithPredicate(x => x.SemesterId == dto.SemesterId
                                    && x.Name.ToLower() == dto.Name.Trim().ToLower()
                                    && x.IsActive && x.DeletedAt == null)
                .WithTracking(false)
                .Build());
            if (duplicated != null)
            {
                return new BaseResponseModel<CreatePhaseResDTO>
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status409Conflict,
                    Message = "Tên giai đoạn đã tồn tại trong học kỳ này"
                };
            }

            var entity = dto.GetEntity();
            entity.IsActive = true;
            entity.CreatedBy = user.UserName;
            entity.CreatedAt = DateTime.Now;

            await phaseRepo.CreateAsync(entity);
            await _unitOfWork.SaveChangesAsync();

            return new BaseResponseModel<CreatePhaseResDTO>
            {
                Data = new CreatePhaseResDTO(entity),
                IsSuccess = true,
                StatusCode = StatusCodes.Status201Created,
                Message = "Tạo giai đoạn thành công"
            };
        }
        catch (Exception)
        {
            throw;
        }
    }

    public async Task<BaseResponseModel<UpdatePhaseResDTO>> UpdatePhase(UpdatePhaseDTO dto, int userId)
    {
        try
        {
            var user = await _identityRepository.GetByIdAsync((long)userId);
            if (user == null)
            {
                return new BaseResponseModel<UpdatePhaseResDTO>
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status404NotFound,
                    Message = "Người dùng không tồn tại"
                };
            }

            var phaseRepo = _unitOfWork.GetRepo<Phase>();
            var phase = await phaseRepo.GetSingleAsync(new QueryBuilder<Phase>()
                .WithPredicate(x => x.Id == dto.Id && x.IsActive && x.DeletedAt == null)
                .WithTracking(true)
                .Build());

            if (phase == null)
            {
                return new BaseResponseModel<UpdatePhaseResDTO>
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status404NotFound,
                    Message = "Giai đoạn không tồn tại"
                };
            }

            // Validate Semester tồn tại
            var semesterRepo = _unitOfWork.GetRepo<Semester>();
            var semester = await semesterRepo.GetSingleAsync(new QueryBuilder<Semester>()
                .WithPredicate(x => x.Id == dto.SemesterId && x.IsActive && x.DeletedAt == null)
                .WithTracking(false)
                .Build());

            if (semester == null)
            {
                return new BaseResponseModel<UpdatePhaseResDTO>
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status404NotFound,
                    Message = "Học kỳ không tồn tại"
                };
            }

            // Validate PhaseType tồn tại
            var phaseTypeRepo = _unitOfWork.GetRepo<PhaseType>();
            var phaseType = await phaseTypeRepo.GetSingleAsync(new QueryBuilder<PhaseType>()
                .WithPredicate(x => x.Id == dto.PhaseTypeId && x.IsActive && x.DeletedAt == null)
                .WithTracking(false)
                .Build());

            if (phaseType == null)
            {
                return new BaseResponseModel<UpdatePhaseResDTO>
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status404NotFound,
                    Message = "Loại giai đoạn không tồn tại"
                };
            }

            // Optional: kiểm tra trùng tên trong cùng học kỳ (trừ chính nó)
            var duplicated = await phaseRepo.GetSingleAsync(new QueryBuilder<Phase>()
                .WithPredicate(x => x.Id != dto.Id
                                    && x.SemesterId == dto.SemesterId
                                    && x.Name.ToLower() == dto.Name.Trim().ToLower()
                                    && x.IsActive && x.DeletedAt == null)
                .WithTracking(false)
                .Build());
            if (duplicated != null)
            {
                return new BaseResponseModel<UpdatePhaseResDTO>
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status409Conflict,
                    Message = "Tên giai đoạn đã tồn tại trong học kỳ này"
                };
            }

            phase.SemesterId = dto.SemesterId;
            phase.PhaseTypeId = dto.PhaseTypeId;
            phase.Name = dto.Name.Trim();
            phase.StartDate = dto.StartDate;
            phase.EndDate = dto.EndDate;
            phase.SubmissionDeadline = dto.SubmissionDeadline;
            phase.LastModifiedBy = user.UserName;
            phase.LastModifiedAt = DateTime.Now;

            await phaseRepo.UpdateAsync(phase);
            await _unitOfWork.SaveChangesAsync();

            return new BaseResponseModel<UpdatePhaseResDTO>
            {
                Data = new UpdatePhaseResDTO(phase),
                IsSuccess = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Cập nhật giai đoạn thành công"
            };
        }
        catch (Exception)
        {
            throw;
        }
    }

    public async Task<BaseResponseModel> DeletePhase(int id)
    {
        try
        {
            var phaseRepo = _unitOfWork.GetRepo<Phase>();
            var phase = await phaseRepo.GetSingleAsync(new QueryBuilder<Phase>()
                .WithPredicate(x => x.Id == id && x.IsActive && x.DeletedAt == null)
                .WithTracking(true)
                .Build());

            if (phase == null)
            {
                return new BaseResponseModel
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status404NotFound,
                    Message = "Giai đoạn không tồn tại"
                };
            }

            // Không cho xóa nếu đã có Submission tham chiếu
            var submissionRepo = _unitOfWork.GetRepo<Submission>();
            var hasSubmission = await submissionRepo.GetSingleAsync(new QueryBuilder<Submission>()
                .WithPredicate(x => x.PhaseId == id)
                .WithTracking(false)
                .Build());
            if (hasSubmission != null)
            {
                return new BaseResponseModel
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status409Conflict,
                    Message = "Không thể xóa giai đoạn vì đang có submission sử dụng"
                };
            }

            phase.IsActive = false;
            phase.DeletedAt = DateTime.Now;

            await phaseRepo.UpdateAsync(phase);
            await _unitOfWork.SaveChangesAsync();

            return new BaseResponseModel
            {
                IsSuccess = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Xóa giai đoạn thành công"
            };
        }
        catch (Exception)
        {
            throw;
        }
    }

    public async Task<BaseResponseModel<PagingDataModel<PhaseOverviewResDTO, GetPhasesQueryDTO>>> GetPhases(GetPhasesQueryDTO query)
    {
        try
        {
            var phaseRepo = _unitOfWork.GetRepo<Phase>();

            Expression<Func<Phase, bool>> predicate = x => x.IsActive && x.DeletedAt == null;
            if (query.SemesterId.HasValue)
            {
                var semId = query.SemesterId.Value;
                predicate = x => x.IsActive && x.DeletedAt == null && x.SemesterId == semId;
            }

            var qb = new QueryBuilder<Phase>()
                .WithPredicate(predicate)
                .WithInclude(x => x.PhaseType)
                .WithInclude(x => x.Semester)
                .WithTracking(false);

            var baseQuery = phaseRepo.Get(qb.Build());

            query.TotalRecord = await baseQuery.CountAsync();

            var items = await baseQuery
                .OrderByDescending(x => x.StartDate)
                .ToPagedList(query.PageNumber, query.PageSize)
                .ToListAsync();

            return new BaseResponseModel<PagingDataModel<PhaseOverviewResDTO, GetPhasesQueryDTO>>
            {
                Data = new PagingDataModel<PhaseOverviewResDTO, GetPhasesQueryDTO>(
                    items.Select(x => new PhaseOverviewResDTO(x)).ToList(), query),
                IsSuccess = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Lấy danh sách giai đoạn thành công"
            };
        }
        catch (Exception)
        {
            throw;
        }
    }

    public async Task<BaseResponseModel<PhaseDetailDTO>> GetPhaseDetail(int id)
    {
        try
        {
            var phaseRepo = _unitOfWork.GetRepo<Phase>();
            var phase = await phaseRepo.GetSingleAsync(new QueryBuilder<Phase>()
                .WithPredicate(x => x.Id == id && x.IsActive && x.DeletedAt == null)
                .WithInclude(x => x.PhaseType)
                .WithInclude(x => x.Semester)
                .WithTracking(false)
                .Build());

            if (phase == null)
            {
                return new BaseResponseModel<PhaseDetailDTO>
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status404NotFound,
                    Message = "Giai đoạn không tồn tại"
                };
            }

            return new BaseResponseModel<PhaseDetailDTO>
            {
                Data = new PhaseDetailDTO(phase),
                IsSuccess = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Lấy chi tiết giai đoạn thành công"
            };
        }
        catch (Exception)
        {
            throw;
        }
    }
}
