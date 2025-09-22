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

        /// <summary>
        /// Yêu cầu đặt lại mật khẩu (gửi email chứa token)
        /// </summary>
        [HttpPost("forgot-password")]
        [SwaggerOperation(Summary = "Yêu cầu đặt lại mật khẩu", Description = "Gửi email chứa token đặt lại mật khẩu nếu email tồn tại")]
        [SwaggerResponse(200, "Yêu cầu gửi email thành công")]
        [SwaggerResponse(400, "Dữ liệu đầu vào không hợp lệ")]
        [SwaggerResponse(500, "Lỗi máy chủ nội bộ")]
        [Consumes("application/json")]
        [Produces("application/json")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequestDTO dto)
        {
            if (!ModelState.IsValid)
                return ModelInvalid();

            try
            {
                var result = await _authService.ForgotPasswordAsync(dto);
                return ProcessServiceResponse(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while processing forgot password for email {Email}", dto.Email);
                return Error(ConstantModel.ErrorMessage);
            }
        }

        /// <summary>
        /// Đặt lại mật khẩu bằng token
        /// </summary>
        [HttpPost("reset-password")]
        [SwaggerOperation(Summary = "Đặt lại mật khẩu bằng token", Description = "Đặt lại mật khẩu sử dụng token gửi về email")]
        [SwaggerResponse(200, "Đặt lại mật khẩu thành công")]
        [SwaggerResponse(400, "Dữ liệu đầu vào không hợp lệ")]
        [SwaggerResponse(500, "Lỗi máy chủ nội bộ")]
        [Consumes("application/json")]
        [Produces("application/json")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDTO dto)
        {
            if (!ModelState.IsValid)
                return ModelInvalid();

            try
            {
                var result = await _authService.ResetPasswordAsync(dto);
                return ProcessServiceResponse(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while resetting password for user {UserId}", dto.UserId);
                return Error(ConstantModel.ErrorMessage);
            }
        }

                /// <summary>
                /// Render a simple reset-password HTML form when accessed via GET with userId and token query params.
                /// This makes clicking the reset link (from email or Swagger response) open a usable form in the browser.
                /// </summary>
                [HttpGet("reset-password")]
                [Consumes("text/html")]
                [Produces("text/html")]
                public IActionResult ResetPasswordForm([FromQuery] string userId, [FromQuery] string token)
                {
                        // Basic HTML form that posts JSON to the POST /api/auth/reset-password endpoint
                        var encodedToken = System.Net.WebUtility.UrlDecode(token ?? string.Empty);
                                                var template = @"<!doctype html>
                        <html lang='en'>
                            <head>
                                <meta charset='utf-8'/>
                                <meta name='viewport' content='width=device-width, initial-scale=1'/>
                                <title>Reset Password</title>
                                <style>
                                    body { font-family: Arial, sans-serif; background:#f5f5f5; padding:20px }
                                    .box { background:#fff; padding:20px; border-radius:6px; max-width:600px; margin:30px auto; box-shadow:0 2px 8px rgba(0,0,0,0.1) }
                                    label { display:block; margin-top:10px }
                                    input[type=password], input[type=text] { width:100%; padding:8px; margin-top:6px; box-sizing:border-box }
                                    .btn { margin-top:16px; padding:10px 14px; background:#0066cc; color:#fff; border:none; border-radius:4px; cursor:pointer }
                                    pre { background:#f0f0f0; padding:10px; border-radius:4px; overflow:auto }
                                </style>
                            </head>
                            <body>
                                <div class='box'>
                                    <h2>Reset Password</h2>
                                    <p>If you arrived here from an email link, confirm the details and enter your new password below.</p>
                                    <form id='resetForm' method='post' action='/api/auth/reset-password'>
                                        <input type='hidden' name='UserId' value='{{USERID}}'>
                                        <input type='hidden' name='Token' value='{{TOKEN}}'>
                                        <label>New password
                                            <input type='password' name='NewPassword' required minlength='6'/>
                                        </label>
                                        <label>Confirm new password
                                            <input type='password' name='ConfirmPassword' required minlength='6'/>
                                        </label>
                                        <button class='btn' type='submit'>Reset password</button>
                                    </form>
                                    <hr/>
                                    <h4>Manual JSON</h4>
                                    <p>If you prefer to call the API directly (for example via Swagger), use this JSON payload (replace values):</p>
                                    <pre>{{MANUAL_JSON}}</pre>
                                </div>
                                <script>
                                    // Convert form submit to JSON so it matches the POST API expecting a JSON body
                                    document.getElementById('resetForm').addEventListener('submit', async function (evt) {
                                        evt.preventDefault();
                                        const form = evt.target;
                                        const data = {
                                            UserId: form.UserId.value,
                                            Token: form.Token.value,
                                            NewPassword: form.NewPassword.value,
                                            ConfirmPassword: form.ConfirmPassword.value
                                        };

                                        const resp = await fetch(form.action, {
                                            method: 'POST',
                                            headers: { 'Content-Type': 'application/json' },
                                            body: JSON.stringify(data)
                                        });

                                        const text = await resp.text();
                                        alert('Response: ' + resp.status + '\n' + text);
                                    });
                                </script>
                            </body>
                        </html>";

                                                var manualJson = System.Text.Json.JsonSerializer.Serialize(new { UserId = userId ?? string.Empty, Token = token ?? string.Empty, NewPassword = "<your-new-password>", ConfirmPassword = "<your-new-password>" }, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
                                                // HTML-encode inserted values
                                                var safeUserId = System.Net.WebUtility.HtmlEncode(userId ?? string.Empty);
                                                var safeToken = System.Net.WebUtility.HtmlEncode(encodedToken ?? string.Empty);
                                                var safeManualJson = System.Net.WebUtility.HtmlEncode(manualJson);

                                                var html = template.Replace("{{USERID}}", safeUserId)
                                                                                     .Replace("{{TOKEN}}", safeToken)
                                                                                     .Replace("{{MANUAL_JSON}}", safeManualJson);

                        return Content(html, "text/html");
                }
    }
}
