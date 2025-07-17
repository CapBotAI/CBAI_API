using System;
using App.Commons.ResponseModel;
using App.Entities.DTOs.Semester;
using App.Entities.DTOs.Semesters;

namespace App.BLL.Interfaces;

public interface ISemesterService
{
    Task<BaseResponseModel<CreateSemesterResDTO>> CreateSemester(CreateSemesterDTO createSemesterDTO);
}
