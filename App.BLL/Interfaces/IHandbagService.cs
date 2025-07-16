using System;
using App.Commons.ResponseModel;
using App.Entities.DTOs.Handbag;

namespace App.BLL.Interfaces;

public interface IHandbagService
{
    Task<BaseResponseModel<List<HandbagResponseDTO>>> GetAllHandbagsAsync();
    Task<BaseResponseModel<HandbagResponseDTO>> GetHandbagByIdAsync(int id);
    Task<BaseResponseModel<HandbagResponseDTO>> CreateHandbagAsync(CreateHandbagDTO createHandbagDTO);
    Task<BaseResponseModel<HandbagResponseDTO>> UpdateHandbagAsync(int id, UpdateHandbagDTO updateHandbagDTO);
    Task<BaseResponseModel> DeleteHandbagAsync(int id);
    Task<BaseResponseModel<List<SearchHandbagResultDTO>>> SearchHandbagsAsync(string? modelName, string? material);

    IQueryable<HandbagODataDTO> GetHandbagsQueryable();
}
