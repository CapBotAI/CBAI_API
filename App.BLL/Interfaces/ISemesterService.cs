using System;
using App.Commons.ResponseModel;
using App.Entities.DTOs.Semester;
using App.Entities.DTOs.Semesters;
using App.Entities.Entities.Core;

namespace App.BLL.Interfaces;

public interface ISemesterService
{
    Task<BaseResponseModel<CreateSemesterResDTO>> CreateSemester(CreateSemesterDTO createSemesterDTO, int userId);
    Task<BaseResponseModel<List<SemesterOverviewResDTO>>> GetAllSemester();
    Task<BaseResponseModel<UpdateSemesterResDTO>> UpdateSemester(UpdateSemesterDTO updateSemesterDTO, int userId);
    Task<BaseResponseModel<SemesterDetailDTO>> GetSemesterDetail(int semesterId);
}
