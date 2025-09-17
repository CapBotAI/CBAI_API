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


        /// <summary>
        /// Đăng nhập
        /// </summary>
        /// <param name="loginDTO">Thông tin đăng nhập</param>
        /// <returns>Kết quả đăng nhập</returns>
        /// <remarks>
        /// Đăng nhập với thông tin đăng nhập bao gồm:
        /// - Email (bắt buộc)
        /// - Mật khẩu (bắt buộc, tối thiểu 6 ký tự)
        ///
        /// Sample request:
        ///
        ///     POST /api/auth/login
        ///     {
        ///         "email": "user@example.com",
        ///         "password": "SecurePass123",
        ///         "fullName": "Nguyễn Văn A",
        ///         "phoneNumber": "+84123456789"
        ///     }
        ///
        /// </remarks>
        [HttpPost("login")]
        [SwaggerOperation(
            Summary = "Đăng nhập",
            Description = "Đăng nhập với thông tin đăng nhập đầy đủ"
        )]
        [SwaggerResponse(200, "Đăng nhập thành công")]
        [SwaggerResponse(400, "Dữ liệu đầu vào không hợp lệ")]
        [SwaggerResponse(401, "Lỗi xác thực")]
        [SwaggerResponse(403, "Quyền truy cập bị từ chối")]
        [SwaggerResponse(422, "Model không hợp lệ.")]
        [SwaggerResponse(500, "Lỗi máy chủ nội bộ")]
        [Consumes("application/json")]
        [Produces("application/json")]
        public async Task<IActionResult> Login([FromBody] LoginDTO loginDTO)
        {
            if (!ModelState.IsValid)
            {
                return ModelInvalid();
            }

            try
            {
                var result = await _authService.SignInAsync(loginDTO);

                return ProcessServiceResponse(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while logging in with email {Email}", loginDTO.EmailOrUsername);
                return Error(ConstantModel.ErrorMessage);
            }
        }

        /// <summary>
        /// Đổi mật khẩu
        /// </summary>
        /// <param name="dto">Dữ liệu đổi mật khẩu</param>
        /// <returns>Kết quả của thao tác đổi mật khẩu</returns>
        /// <remarks>
        /// Cho phép người dùng đã xác thực đổi mật khẩu của mình.
        ///
        /// Sample request:
        ///
        ///     POST /api/auth/change-password
        ///     {
        ///         "oldPassword": "OldSecurePass123",
        ///         "newPassword": "NewSecurePass123"
        ///     }
        ///
        /// </remarks>
        [Authorize]
        [HttpPost("change-password")]
        [SwaggerOperation(
            Summary = "Đổi mật khẩu",
            Description = "Cho phép người dùng đã xác thực đổi mật khẩu của mình"
        )]
        [SwaggerResponse(200, "Đổi mật khẩu thành công")]
        [SwaggerResponse(400, "Dữ liệu đầu vào không hợp lệ")]
        [SwaggerResponse(401, "Truy cập không được phép")]
        [SwaggerResponse(404, "Người dùng không tồn tại")]
        [SwaggerResponse(500, "Lỗi máy chủ nội bộ")]
        [Consumes("application/json")]
        [Produces("application/json")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDTO dto)
        {
            if (!ModelState.IsValid)
            {
                return ModelInvalid();
            }

            try
            {
                var userId = User.FindFirst("id")?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized();
                }

                var result = await _authService.ChangePasswordAsync(dto, userId);
                return ProcessServiceResponse(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while changing password for user ID {UserId}", User.FindFirst("id")?.Value);
                return Error(ConstantModel.ErrorMessage);
            }
        }
    }
}
