using System;
using App.Commons.Paging;
using App.Commons.ResponseModel;
using App.Entities.DTOs.Phases;

namespace App.BLL.Interfaces;

public interface IPhaseService
{
    Task<BaseResponseModel<CreatePhaseResDTO>> CreatePhase(CreatePhaseDTO dto, int userId);
    Task<BaseResponseModel<UpdatePhaseResDTO>> UpdatePhase(UpdatePhaseDTO dto, int userId);
    Task<BaseResponseModel> DeletePhase(int id);
    Task<BaseResponseModel<PagingDataModel<PhaseOverviewResDTO, GetPhasesQueryDTO>>> GetPhases(GetPhasesQueryDTO query);
    Task<BaseResponseModel<PhaseDetailDTO>> GetPhaseDetail(int id);
}
