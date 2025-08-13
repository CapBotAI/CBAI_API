using App.BLL.Interfaces;
using App.Commons;
using App.Commons.BaseAPI;
using App.Entities.Constants;
using App.Entities.DTOs.PhaseTypes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace CapBot.api.Controllers;

[Route("api/phase-type")]
[ApiController]
public class PhaseTypeController : BaseAPIController
{
    private readonly IPhaseTypeService _phaseTypeService;
    private readonly ILogger<PhaseTypeController> _logger;

    public PhaseTypeController(IPhaseTypeService phaseTypeService, ILogger<PhaseTypeController> logger)
    {
        _phaseTypeService = phaseTypeService;
        _logger = logger;
    }

    /// <summary>
    /// Tạo loại giai đoạn mới
    /// </summary>
    /// <param name="createPhaseTypeDTO">Thông tin tạo loại giai đoạn</param>
    /// <returns>Kết quả tạo loại giai đoạn</returns>
    /// <remarks>
    /// Tạo loại giai đoạn mới với thông tin đầy đủ
    /// - Tên loại giai đoạn (bắt buộc, tối đa 100 ký tự)
    /// - Mô tả (tùy chọn, tối đa 500 ký tự)
    ///
    /// Sample request:
    ///
    ///     POST /api/phase-type/create
    ///     {
    ///         "name": "Giai đoạn đề xuất",
    ///         "description": "Giai đoạn sinh viên đề xuất đề tài"
    ///     }
    ///
    /// </remarks>
    [Authorize(Roles = SystemRoleConstants.Administrator)]
    [HttpPost("create")]
    [SwaggerOperation(
        Summary = "Tạo loại giai đoạn mới",
        Description = "Tạo loại giai đoạn mới với thông tin đầy đủ"
    )]
    [SwaggerResponse(201, "Tạo loại giai đoạn thành công")]
    [SwaggerResponse(401, "Lỗi xác thực")]
    [SwaggerResponse(403, "Quyền truy cập bị từ chối")]
    [SwaggerResponse(409, "Tên loại giai đoạn đã tồn tại")]
    [SwaggerResponse(422, "Model không hợp lệ")]
    [SwaggerResponse(500, "Lỗi máy chủ nội bộ")]
    [Consumes("application/json")]
    [Produces("application/json")]
    public async Task<IActionResult> Create([FromBody] CreatePhaseTypeDTO createPhaseTypeDTO)
    {
        if (!ModelState.IsValid)
        {
            return ModelInvalid();
        }

        if (!createPhaseTypeDTO.Validate().IsSuccess)
        {
            return ProcessServiceResponse(createPhaseTypeDTO.Validate());
        }

        try
        {
            var result = await _phaseTypeService.CreatePhaseType(createPhaseTypeDTO, UserId);
            return ProcessServiceResponse(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while creating phase type");
            return Error(ConstantModel.ErrorMessage);
        }
    }

    /// <summary>
    /// Lấy danh sách loại giai đoạn
    /// </summary>
    /// <returns>Danh sách loại giai đoạn</returns>
    /// <remarks>
    /// Lấy danh sách tất cả loại giai đoạn đang hoạt động
    ///
    /// Sample request:
    ///
    ///     GET /api/phase-type/all
    ///
    /// </remarks>
    [Authorize]
    [HttpGet("all")]
    [SwaggerOperation(
        Summary = "Lấy danh sách loại giai đoạn",
        Description = "Lấy danh sách tất cả loại giai đoạn đang hoạt động"
    )]
    [SwaggerResponse(200, "Lấy danh sách loại giai đoạn thành công")]
    [SwaggerResponse(401, "Lỗi xác thực")]
    [SwaggerResponse(500, "Lỗi máy chủ nội bộ")]
    [Produces("application/json")]
    public async Task<IActionResult> GetAll()
    {
        try
        {
            var result = await _phaseTypeService.GetAllPhaseTypes();
            return ProcessServiceResponse(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while getting all phase types");
            return Error(ConstantModel.ErrorMessage);
        }
    }

    /// <summary>
    /// Lấy danh sách loại giai đoạn
    /// </summary>
    /// <returns>Danh sách loại giai đoạn với phân trang</returns>
    /// <remarks>
    /// Lấy danh sách tất cả loại giai đoạn đang hoạt động với phân trang
    ///
    /// Sample request:
    ///
    ///     GET /api/phase-type/all?page=1&pageSize=10&keyword=
    ///     GET /api/phase-type/all?page=1&pageSize=10&keyword=Giai đoạn
    ///
    /// </remarks>
    [Authorize]
    [HttpGet("all-paging")]
    [SwaggerOperation(
        Summary = "Lấy danh sách loại giai đoạn",
        Description = "Lấy danh sách tất cả loại giai đoạn đang hoạt động"
    )]
    [SwaggerResponse(200, "Lấy danh sách loại giai đoạn thành công")]
    [SwaggerResponse(401, "Lỗi xác thực")]
    [SwaggerResponse(500, "Lỗi máy chủ nội bộ")]
    [Produces("application/json")]
    public async Task<IActionResult> GetAllPaging([FromQuery] GetPhaseTypesQueryDTO getPhaseTypesQueryDTO)
    {
        try
        {
            var result = await _phaseTypeService.GetPhaseTypes(getPhaseTypesQueryDTO);
            return ProcessServiceResponse(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while getting all phase types with paging");
            return Error(ConstantModel.ErrorMessage);
        }
    }

    /// <summary>
    /// Cập nhật loại giai đoạn
    /// </summary>
    /// <param name="updatePhaseTypeDTO">Thông tin cập nhật loại giai đoạn</param>
    /// <returns>Kết quả cập nhật loại giai đoạn</returns>
    /// <remarks>
    /// Cập nhật loại giai đoạn với thông tin đầy đủ
    /// - Id loại giai đoạn (bắt buộc)
    /// - Tên loại giai đoạn (bắt buộc, tối đa 100 ký tự)
    /// - Mô tả (tùy chọn, tối đa 500 ký tự)
    ///
    /// Sample request:
    ///
    ///     PUT /api/phase-type/update
    ///     {
    ///         "id": 1,
    ///         "name": "Giai đoạn đề xuất cập nhật",
    ///         "description": "Mô tả cập nhật cho giai đoạn đề xuất"
    ///     }
    ///
    /// </remarks>
    [Authorize(Roles = SystemRoleConstants.Administrator)]
    [HttpPut("update")]
    [SwaggerOperation(
        Summary = "Cập nhật loại giai đoạn",
        Description = "Cập nhật loại giai đoạn với thông tin đầy đủ"
    )]
    [SwaggerResponse(200, "Cập nhật loại giai đoạn thành công")]
    [SwaggerResponse(401, "Lỗi xác thực")]
    [SwaggerResponse(403, "Quyền truy cập bị từ chối")]
    [SwaggerResponse(404, "Loại giai đoạn không tồn tại")]
    [SwaggerResponse(409, "Tên loại giai đoạn đã tồn tại")]
    [SwaggerResponse(422, "Model không hợp lệ")]
    [SwaggerResponse(500, "Lỗi máy chủ nội bộ")]
    [Consumes("application/json")]
    [Produces("application/json")]
    public async Task<IActionResult> Update([FromBody] UpdatePhaseTypeDTO updatePhaseTypeDTO)
    {
        if (!ModelState.IsValid)
        {
            return ModelInvalid();
        }

        if (!updatePhaseTypeDTO.Validate().IsSuccess)
        {
            return ProcessServiceResponse(updatePhaseTypeDTO.Validate());
        }

        try
        {
            var result = await _phaseTypeService.UpdatePhaseType(updatePhaseTypeDTO, UserId);
            return ProcessServiceResponse(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while updating phase type");
            return Error(ConstantModel.ErrorMessage);
        }
    }

    /// <summary>
    /// Lấy chi tiết loại giai đoạn
    /// </summary>
    /// <param name="phaseTypeId">Id của loại giai đoạn</param>
    /// <returns>Chi tiết loại giai đoạn</returns>
    /// <remarks>
    /// Lấy chi tiết loại giai đoạn theo Id
    ///
    /// Sample request:
    ///
    ///     GET /api/phase-type/detail/1
    ///
    /// </remarks>
    [Authorize]
    [HttpGet("detail/{phaseTypeId}")]
    [SwaggerOperation(
        Summary = "Lấy chi tiết loại giai đoạn",
        Description = "Lấy chi tiết loại giai đoạn theo Id"
    )]
    [SwaggerResponse(200, "Lấy chi tiết loại giai đoạn thành công")]
    [SwaggerResponse(401, "Lỗi xác thực")]
    [SwaggerResponse(404, "Loại giai đoạn không tồn tại")]
    [SwaggerResponse(500, "Lỗi máy chủ nội bộ")]
    [Produces("application/json")]
    public async Task<IActionResult> GetDetail(int phaseTypeId)
    {
        try
        {
            var result = await _phaseTypeService.GetPhaseTypeDetail(phaseTypeId);
            return ProcessServiceResponse(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while getting phase type detail");
            return Error(ConstantModel.ErrorMessage);
        }
    }

    /// <summary>
    /// Xóa loại giai đoạn
    /// </summary>
    /// <param name="phaseTypeId">Id của loại giai đoạn</param>
    /// <returns>Kết quả xóa loại giai đoạn</returns>
    /// <remarks>
    /// Xóa loại giai đoạn theo Id (soft delete)
    /// Lưu ý: Không thể xóa loại giai đoạn đang được sử dụng trong các giai đoạn khác
    ///
    /// Sample request:
    ///
    ///     DELETE /api/phase-type/delete/1
    ///
    /// </remarks>
    [Authorize(Roles = SystemRoleConstants.Administrator)]
    [HttpDelete("delete/{phaseTypeId}")]
    [SwaggerOperation(
        Summary = "Xóa loại giai đoạn",
        Description = "Xóa loại giai đoạn theo Id (soft delete)"
    )]
    [SwaggerResponse(200, "Xóa loại giai đoạn thành công")]
    [SwaggerResponse(401, "Lỗi xác thực")]
    [SwaggerResponse(403, "Quyền truy cập bị từ chối")]
    [SwaggerResponse(404, "Loại giai đoạn không tồn tại")]
    [SwaggerResponse(409, "Không thể xóa loại giai đoạn đang được sử dụng")]
    [SwaggerResponse(500, "Lỗi máy chủ nội bộ")]
    [Produces("application/json")]
    public async Task<IActionResult> Delete(int phaseTypeId)
    {
        try
        {
            var result = await _phaseTypeService.DeletePhaseType(phaseTypeId);
            return ProcessServiceResponse(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while deleting phase type");
            return Error(ConstantModel.ErrorMessage);
        }
    }
}
