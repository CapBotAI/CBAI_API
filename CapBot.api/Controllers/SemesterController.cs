using App.BLL.Interfaces;
using App.Commons;
using App.Commons.BaseAPI;
using App.Entities.Constants;
using App.Entities.DTOs.Semester;
using App.Entities.DTOs.Semesters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace CapBot.api.Controllers
{
    [Route("api/semester")]
    [ApiController]
    public class SemesterController : BaseAPIController
    {
        private readonly ISemesterService _semesterService;
        private readonly ILogger<SemesterController> _logger;
        public SemesterController(ISemesterService semesterService, ILogger<SemesterController> logger)
        {
            _semesterService = semesterService;
            _logger = logger;
        }

        /// <summary>
        /// Tạo học kỳ mới
        /// </summary>
        /// <param name="createSemesterDTO">Thông tin tạo học kỳ</param>
        /// <returns>Kết quả tạo học kỳ</returns>
        /// <remarks>
        /// Tạo học kỳ mới với thông tin đầy đủ
        /// - Tên học kỳ (bắt buộc)
        /// - Ngày bắt đầu (bắt buộc)
        /// - Ngày kết thúc (bắt buộc)
        ///
        /// Sample request:
        ///
        ///     POST /api/semester/create
        ///     {
        ///         "name": "Học kỳ 1",
        ///         "startDate": "2025-01-01",
        ///         "endDate": "2025-05-31"
        ///     }
        ///
        /// </remarks>
        [Authorize(Roles = SystemRoleConstants.Administrator)]
        [HttpPost("create")]
        [SwaggerOperation(
            Summary = "Tạo học kỳ mới",
            Description = "Tạo học kỳ mới với thông tin đầy đủ"
        )]
        [SwaggerResponse(201, "Tạo học kỳ thành công")]
        [SwaggerResponse(401, "Lỗi xác thực")]
        [SwaggerResponse(403, "Quyền truy cập bị từ chối")]
        [SwaggerResponse(422, "Model không hợp lệ.")]
        [SwaggerResponse(500, "Lỗi máy chủ nội bộ")]
        [Consumes("application/json")]
        [Produces("application/json")]
        public async Task<IActionResult> Create([FromBody] CreateSemesterDTO createSemesterDTO)
        {
            if (!ModelState.IsValid)
            {
                return ModelInvalid();
            }

            if (!createSemesterDTO.Validate().IsSuccess)
            {
                return ProcessServiceResponse(createSemesterDTO.Validate());
            }

            try
            {
                var result = await _semesterService.CreateSemester(createSemesterDTO, UserId);

                return ProcessServiceResponse(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while creating semester");
                return Error(ConstantModel.ErrorMessage);
            }
        }

        /// <summary>
        /// Lấy danh sách học kỳ
        /// </summary>
        /// <returns>Danh sách học kỳ</returns>
        /// <remarks>
        /// Lấy danh sách học kỳ
        ///
        /// Sample request:
        ///
        ///     GET /api/semester/all
        ///
        /// </remarks>
        [Authorize]
        [HttpGet("all")]
        [SwaggerOperation(
            Summary = "Lấy danh sách học kỳ",
            Description = "Lấy danh sách học kỳ"
        )]
        [SwaggerResponse(200, "Lấy danh sách học kỳ thành công")]
        [SwaggerResponse(401, "Lỗi xác thực")]
        [SwaggerResponse(403, "Quyền truy cập bị từ chối")]
        [SwaggerResponse(422, "Model không hợp lệ.")]
        [SwaggerResponse(500, "Lỗi máy chủ nội bộ")]
        [Consumes("application/json")]
        [Produces("application/json")]
        public async Task<IActionResult> GetAll()
        {
            try
            {
                var result = await _semesterService.GetAllSemester();

                return ProcessServiceResponse(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting all semester");
                return Error(ConstantModel.ErrorMessage);
            }
        }


        /// <summary>
        /// Cập nhật học kỳ
        /// </summary>
        /// <param name="updateSemesterDTO">Thông tin cập nhật học kỳ</param>
        /// <returns>Kết quả cập nhật học kỳ</returns>
        /// <remarks>
        /// Cập nhật học kỳ với thông tin đầy đủ
        /// - Tên học kỳ (bắt buộc)
        /// - Ngày bắt đầu (bắt buộc)
        /// - Ngày kết thúc (bắt buộc)
        ///
        /// Sample request:
        ///
        ///     POST /api/semester/update
        ///     {
        ///         "id": 1,
        ///         "name": "Học kỳ 2",
        ///         "startDate": "2025-01-01",
        ///         "endDate": "2025-05-31"
        ///     }
        ///
        /// </remarks>
        [Authorize(Roles = SystemRoleConstants.Administrator)]
        [HttpPut("update")]
        [SwaggerOperation(
            Summary = "Cập nhật học kỳ",
            Description = "Cập nhật học kỳ với thông tin đầy đủ"
        )]
        [SwaggerResponse(200, "Cập nhật học kỳ thành công")]
        [SwaggerResponse(401, "Lỗi xác thực")]
        [SwaggerResponse(403, "Quyền truy cập bị từ chối")]
        [SwaggerResponse(422, "Model không hợp lệ.")]
        [SwaggerResponse(500, "Lỗi máy chủ nội bộ")]
        [Consumes("application/json")]
        [Produces("application/json")]
        public async Task<IActionResult> Update([FromBody] UpdateSemesterDTO updateSemesterDTO)
        {
            if (!ModelState.IsValid)
            {
                return ModelInvalid();
            }

            if (!updateSemesterDTO.Validate().IsSuccess)
            {
                return ProcessServiceResponse(updateSemesterDTO.Validate());
            }

            try
            {
                var result = await _semesterService.UpdateSemester(updateSemesterDTO, UserId);

                return ProcessServiceResponse(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while updating semester");
                return Error(ConstantModel.ErrorMessage);
            }
        }

        /// <summary>
        /// Lấy chi tiết học kỳ
        /// </summary>
        /// <param name="semesterId">Id của học kỳ</param>
        /// <returns>Chi tiết học kỳ</returns>
        /// <remarks>
        /// Lấy chi tiết học kỳ
        ///
        /// Sample request:
        ///
        ///     GET /api/semester/detail/1
        ///
        /// </remarks>
        [Authorize]
        [HttpGet("detail/{semesterId}")]
        [SwaggerOperation(
            Summary = "Lấy chi tiết học kỳ",
            Description = "Lấy chi tiết học kỳ"
        )]
        [SwaggerResponse(200, "Lấy chi tiết học kỳ thành công")]
        [SwaggerResponse(401, "Lỗi xác thực")]
        [SwaggerResponse(403, "Quyền truy cập bị từ chối")]
        [SwaggerResponse(422, "Model không hợp lệ.")]
        [SwaggerResponse(500, "Lỗi máy chủ nội bộ")]
        [Consumes("application/json")]
        [Produces("application/json")]
        public async Task<IActionResult> GetDetail(int semesterId)
        {
            try
            {
                var result = await _semesterService.GetSemesterDetail(semesterId);

                return ProcessServiceResponse(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting semester detail");
                return Error(ConstantModel.ErrorMessage);
            }
        }

    }
}
