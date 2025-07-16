using Microsoft.AspNetCore.Mvc;
using App.BLL.Interfaces;
using App.Entities.DTOs.Auth;
using App.Commons.BaseAPI;
using App.Commons;
using Swashbuckle.AspNetCore.Annotations;
using Microsoft.AspNetCore.Authorization;
using App.Entities.Enums;
using App.Entities.Constants;

namespace CapBot.api.Controllers
{
    [Route("api/auth")]
    [ApiController]
    public class AuthController : BaseAPIController
    {
        private readonly IAuthService _authService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(IAuthService authService, ILogger<AuthController> logger)
        {
            _authService = authService;
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
        ///     POST /api/register
        ///     {
        ///         "email": "user@example.com",
        ///         "password": "SecurePass123",
        ///         "fullName": "Nguyễn Văn A",
        ///         "phoneNumber": "+84123456789"
        ///     }
        ///
        /// </remarks>
        [Authorize(Roles = SystemRoleConstants.Administrator)]
        [HttpPost("register")]
        [SwaggerOperation(
            Summary = "Đăng ký tài khoản mới",
            Description = "Tạo tài khoản người dùng mới với thông tin đăng ký đầy đủ"
        )]
        [SwaggerResponse(201, "Đăng ký tài khoản thành công")]
        [SwaggerResponse(400, "Dữ liệu đầu vào không hợp lệ")]
        [SwaggerResponse(401, "Lỗi xác thực")]
        [SwaggerResponse(403, "Quyền truy cập bị từ chối")]
        [SwaggerResponse(409, "Email đã tồn tại trong hệ thống")]
        [SwaggerResponse(422, "Model không hợp lệ.")]
        [SwaggerResponse(500, "Lỗi máy chủ nội bộ")]
        [Consumes("application/json")]
        [Produces("application/json")]
        public async Task<IActionResult> Register([FromBody] RegisterDTO registerDTO)
        {
            if (!ModelState.IsValid)
            {
                return ModelInvalid();
            }

            try
            {
                var result = await _authService.SignUpAsync(registerDTO);

                return ProcessServiceResponse(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while registering user with email {Email}", registerDTO.Email);
                return Error(ConstantModel.ErrorMessage);
            }
        }
    }
}
