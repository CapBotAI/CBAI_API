using System;
using App.BLL.Interfaces;
using App.Commons.ResponseModel;
using App.DAL.Interfaces;
using App.DAL.Queries.Implementations;
using App.DAL.UnitOfWork;
using App.Entities.DTOs.Semester;
using App.Entities.DTOs.Semesters;
using App.Entities.Entities.App;
using App.Entities.Entities.Core;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace App.BLL.Implementations;

public class SemesterService : ISemesterService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IIdentityRepository _identityRepository;

    public SemesterService(IUnitOfWork unitOfWork, IIdentityRepository identityRepository)
    {
        this._unitOfWork = unitOfWork;
        this._identityRepository = identityRepository;
    }

    public async Task<BaseResponseModel<CreateSemesterResDTO>> CreateSemester(CreateSemesterDTO createSemesterDTO, int userId)
    {
        try
        {
            var user = await _identityRepository.GetByIdAsync((long)userId);
            if (user == null)
            {
                return new BaseResponseModel<CreateSemesterResDTO>
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status404NotFound,
                    Message = "Người dùng không tồn tại"
                };
            }

            var semesterRepo = _unitOfWork.GetRepo<App.Entities.Entities.App.Semester>();

            var semester = createSemesterDTO.GetEntity();
            semester.CreatedBy = user.UserName;
            semester.CreatedAt = DateTime.Now;

            await semesterRepo.CreateAsync(semester);
            await _unitOfWork.SaveChangesAsync();
            return new BaseResponseModel<CreateSemesterResDTO>
            {
                Data = new CreateSemesterResDTO(semester),
                IsSuccess = true,
                StatusCode = StatusCodes.Status201Created
            };
        }
        catch (System.Exception)
        {
            throw;
        }
    }

    public async Task<BaseResponseModel<List<SemesterOverviewResDTO>>> GetAllSemester()
    {
        try
        {
            var semesterRepo = _unitOfWork.GetRepo<App.Entities.Entities.App.Semester>();
            var semesters = await semesterRepo.GetAllAsync(new QueryBuilder<Semester>()
            .WithPredicate(x => x.IsActive && x.DeletedAt == null)
            .WithTracking(false)
            .Build());
            return new BaseResponseModel<List<SemesterOverviewResDTO>>
            {
                Data = semesters.Select(x => new SemesterOverviewResDTO(x)).ToList(),
                IsSuccess = true,
                StatusCode = StatusCodes.Status201Created
            };
        }
        catch (System.Exception)
        {
            throw;
        }
    }

    public async Task<BaseResponseModel<UpdateSemesterResDTO>> UpdateSemester(UpdateSemesterDTO updateSemesterDTO, int userId)
    {
        try
        {
            var user = await _identityRepository.GetByIdAsync((long)userId);
            if (user == null)
            {
                return new BaseResponseModel<UpdateSemesterResDTO>
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status404NotFound,
                    Message = "Người dùng không tồn tại"
                };
            }

            var semesterRepo = _unitOfWork.GetRepo<App.Entities.Entities.App.Semester>();
            var semester = await semesterRepo.GetSingleAsync(new QueryBuilder<Semester>()
            .WithPredicate(x => x.Id == updateSemesterDTO.Id && x.IsActive && x.DeletedAt == null)
            .WithTracking(false)
            .Build());
            if (semester == null)
            {
                return new BaseResponseModel<UpdateSemesterResDTO>
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status404NotFound,
                    Message = "Học kỳ không tồn tại"
                };
            }

            semester.Name = updateSemesterDTO.Name;
            semester.StartDate = updateSemesterDTO.StartDate;
            semester.EndDate = updateSemesterDTO.EndDate;
            semester.LastModifiedBy = user.UserName;
            semester.LastModifiedAt = DateTime.Now;

            await semesterRepo.UpdateAsync(semester);

            await _unitOfWork.SaveChangesAsync();

            return new BaseResponseModel<UpdateSemesterResDTO>
            {
                Data = new UpdateSemesterResDTO(semester),
                IsSuccess = true,
                StatusCode = StatusCodes.Status200OK
            };
        }
        catch (System.Exception)
        {
            throw;
        }
    }

    public async Task<BaseResponseModel<SemesterDetailDTO>> GetSemesterDetail(int semesterId)
    {
        try
        {
            var semesterRepo = _unitOfWork.GetRepo<App.Entities.Entities.App.Semester>();
            var semester = await semesterRepo.GetSingleAsync(new QueryBuilder<Semester>()
            .WithPredicate(x => x.Id == semesterId && x.IsActive && x.DeletedAt == null)
            .WithTracking(false)
            .Build());
            if (semester == null)
            {
                return new BaseResponseModel<SemesterDetailDTO>
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status404NotFound,
                    Message = "Học kỳ không tồn tại"
                };
            }

            return new BaseResponseModel<SemesterDetailDTO>
            {
                Data = new SemesterDetailDTO(semester),
                IsSuccess = true,
                StatusCode = StatusCodes.Status200OK
            };
        }
        catch (System.Exception)
        {
            throw;
        }
    }

    public async Task<BaseResponseModel> DeleteSemester(int semesterId)
    {
        try
        {
            var semesterRepo = _unitOfWork.GetRepo<App.Entities.Entities.App.Semester>();
            var semester = await semesterRepo.GetSingleAsync(new QueryBuilder<Semester>()
            .WithPredicate(x => x.Id == semesterId && x.IsActive && x.DeletedAt == null)
            .WithTracking(false)
            .Build());
            if (semester == null)
            {
                return new BaseResponseModel
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status404NotFound,
                    Message = "Học kỳ không tồn tại"
                };
            }

            semester.IsActive = false;
            semester.DeletedAt = DateTime.Now;

            await semesterRepo.UpdateAsync(semester);

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
