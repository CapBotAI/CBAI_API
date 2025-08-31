// App.BLL/Interfaces/ILecturerSkillService.cs
using App.Commons.Paging;
using App.Commons.ResponseModel;
using App.Entities.DTOs.LecturerSkills;

namespace App.BLL.Interfaces;

public interface ILecturerSkillService
{
    Task<BaseResponseModel<LecturerSkillResponseDTO>> CreateAsync(CreateLecturerSkillDTO dto, int userId, bool isAdmin);
    Task<BaseResponseModel<LecturerSkillResponseDTO>> UpdateAsync(UpdateLecturerSkillDTO dto, int userId, bool isAdmin);
    Task<BaseResponseModel> DeleteAsync(int id, int userId, bool isAdmin);
    Task<BaseResponseModel<LecturerSkillResponseDTO>> GetByIdAsync(int id);
    Task<BaseResponseModel<PagingDataModel<LecturerSkillResponseDTO>>> GetByLecturerAsync(int lecturerId, GetLecturerSkillQueryDTO dto);
    Task<BaseResponseModel<PagingDataModel<LecturerSkillResponseDTO>>> GetMySkillsAsync(int userId, GetLecturerSkillQueryDTO dto);
}