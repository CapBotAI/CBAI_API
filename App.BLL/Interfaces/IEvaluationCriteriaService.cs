using App.Commons.ResponseModel;
using App.Entities.DTOs.EvaluationCriteria;
using App.Commons.Paging;

namespace App.BLL.Interfaces;

public interface IEvaluationCriteriaService
{
    Task<BaseResponseModel<EvaluationCriteriaResponseDTO>> CreateAsync(CreateEvaluationCriteriaDTO createDTO);
    Task<BaseResponseModel<EvaluationCriteriaResponseDTO>> UpdateAsync(UpdateEvaluationCriteriaDTO updateDTO);
    Task<BaseResponseModel> DeleteAsync(int id);
    Task<BaseResponseModel<EvaluationCriteriaResponseDTO>> GetByIdAsync(int id);
    Task<BaseResponseModel<PagingDataModel<EvaluationCriteriaResponseDTO>>> GetAllAsync(PagingModel pagingModel);
    Task<BaseResponseModel<List<EvaluationCriteriaResponseDTO>>> GetAllActiveAsync();
}