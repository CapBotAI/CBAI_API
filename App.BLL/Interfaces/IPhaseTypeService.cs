using App.Commons.Paging;
using App.Commons.ResponseModel;
using App.Entities.DTOs.PhaseTypes;

namespace App.BLL.Interfaces;

public interface IPhaseTypeService
{
    Task<BaseResponseModel<CreatePhaseTypeResDTO>> CreatePhaseType(CreatePhaseTypeDTO createPhaseTypeDTO, int userId);
    Task<BaseResponseModel<List<PhaseTypeOverviewResDTO>>> GetAllPhaseTypes();
    Task<BaseResponseModel<PagingDataModel<PhaseTypeOverviewResDTO, GetPhaseTypesQueryDTO>>> GetPhaseTypes(GetPhaseTypesQueryDTO getPhaseTypesQueryDTO);
    Task<BaseResponseModel<UpdatePhaseTypeResDTO>> UpdatePhaseType(UpdatePhaseTypeDTO updatePhaseTypeDTO, int userId);
    Task<BaseResponseModel<PhaseTypeDetailDTO>> GetPhaseTypeDetail(int phaseTypeId);
    Task<BaseResponseModel> DeletePhaseType(int phaseTypeId);
}
