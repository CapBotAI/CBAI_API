using System;
using App.BLL.Interfaces;
using App.Commons.ResponseModel;
using App.DAL.UnitOfWork;
using App.Entities.DTOs.Semester;
using App.Entities.DTOs.Semesters;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace App.BLL.Implementations;

public class SemesterService : ISemesterService
{
    private readonly IUnitOfWork _unitOfWork;


    public SemesterService(IUnitOfWork unitOfWork)
    {
        this._unitOfWork = unitOfWork;
    }

    public async Task<BaseResponseModel<CreateSemesterResDTO>> CreateSemester(CreateSemesterDTO createSemesterDTO)
    {
        try
        {
            var semesterRepo = _unitOfWork.GetRepo<App.Entities.Entities.App.Semester>();
            var semester = createSemesterDTO.GetEntity();
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
}
