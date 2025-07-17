using App.BLL.Interfaces;
using App.Commons;
using App.Commons.BaseAPI;
using App.Entities.Constants;
using App.Entities.DTOs.Semester;
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
        /// Đăng ký tài khoản mới
        /// </summary>
        /// <param name="registerDTO">Thông tin đăng ký người dùng</param>
        /// <returns>Kết quả đăng ký tài khoản</returns>
        /// <remarks>
        /// Tạo tài khoản mới với thông tin đăng ký bao gồm:
        /// - Email (bắt buộc)
        /// - Mật khẩu (bắt buộc, tối thiểu 6 ký tự)
        /// - Tên đầy đủ
        /// - Số điện thoại
        ///
        /// Sample request:
        ///
        ///     POST /api/auth/register
        ///     {
        ///         "email": "user@example.com",
        ///         "password": "SecurePass123",
        ///         "fullName": "Nguyễn Văn A",
        ///         "phoneNumber": "+84123456789"
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
                var result = await _semesterService.CreateSemester(createSemesterDTO);

                return ProcessServiceResponse(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while creating semester");
                return Error(ConstantModel.ErrorMessage);
            }
        }

    }
}
