using App.BLL.Interfaces;
using App.Commons;
using App.Commons.BaseAPI;
using App.Entities.Constants;
using App.Entities.DTOs.Accounts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace CapBot.api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AccountController : BaseAPIController
    {
        private readonly IAccountService _accountService;
        private readonly ILogger<AccountController> _logger;
        public AccountController(IAccountService accountService, ILogger<AccountController> logger)
        {
            _accountService = accountService;
            _logger = logger;
        }

        /// <summary>
        /// Thêm roles cho người dùng
        /// </summary>
        /// <returns>Thông tin chi tiết người dùng sau khi thêm role</returns>
        [Authorize(Roles = SystemRoleConstants.Administrator)]
        [HttpPost("user-roles/{userId}")]
        [SwaggerOperation(
            Summary = "Thêm roles cho người dùng",
            Description = "Thêm roles cho người dùng"
        )]
        [SwaggerResponse(200, "Thêm roles thành công")]
        [SwaggerResponse(401, "Lỗi xác thực")]
        [SwaggerResponse(500, "Lỗi máy chủ nội bộ")]
        [Consumes("application/json")]
        [Produces("application/json")]
        public async Task<IActionResult> AddRolesToUser([FromRoute] int userId, [FromBody] List<string> roles)
        {
            try
            {
                var result = await _accountService.AddRoleToUser(userId, roles, UserId);
                return ProcessServiceResponse(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while adding roles to user");
                return Error(ConstantModel.ErrorMessage);
            }
        }

        /// <summary>
        /// Xoá roles cho người dùng
        /// </summary>
        /// <returns>Thông tin chi tiết người dùng sau khi xoá role</returns>
        [Authorize(Roles = SystemRoleConstants.Administrator)]
        [HttpDelete("user-roles/{userId}")]
        [SwaggerOperation(
            Summary = "Xoá roles cho người dùng",
            Description = "Xoá roles cho người dùng"
        )]
        [SwaggerResponse(200, "Xoá roles thành công")]
        [SwaggerResponse(401, "Lỗi xác thực")]
        [SwaggerResponse(500, "Lỗi máy chủ nội bộ")]
        [Consumes("application/json")]
        [Produces("application/json")]
        public async Task<IActionResult> RemoveRolesFromUser([FromRoute] int userId, [FromBody] List<string> roles)
        {
            try
            {
                var result = await _accountService.RemoveRoleFromUserRoles(userId, roles);
                return ProcessServiceResponse(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while removing roles from user");
                return Error(ConstantModel.ErrorMessage);
            }
        }


        /// <summary>
        /// Lấy danh sách quyền của user
        /// </summary>
        /// <param name="query">Thông tin lọc</param>
        /// <returns>Danh sách quyền của user</returns>
        /// <remarks>
        /// Lấy danh sách quyền của user với bộ lọc tùy chọn
        ///
        /// Sample request:
        ///
        ///     GET /api/topic/list
        ///     GET /api/topic/list?semesterId=1
        ///     GET /api/topic/list?categoryId=1
        ///     GET /api/topic/list?semesterId=1&categoryId=1
        ///
        /// </remarks>
        [Authorize]
        [HttpGet("user-roles/{userId}")]
        [SwaggerOperation(
            Summary = "Lấy danh sách quyền của user",
            Description = "Lấy danh sách quyền của user"
        )]
        [SwaggerResponse(200, "Lấy danh sách quyền thành công")]
        [SwaggerResponse(401, "Lỗi xác thực")]
        [SwaggerResponse(500, "Lỗi máy chủ nội bộ")]
        [Consumes("application/json")]
        [Produces("application/json")]
        public async Task<IActionResult> GetUserRoles([FromRoute] long userId)
        {
            try
            {
                var result = await _accountService.GetAllUserRoles(userId);
                return ProcessServiceResponse(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting all user roles");
                return Error(ConstantModel.ErrorMessage);
            }
        }

        /// <summary>
        /// Lấy danh sách user hiện tại
        /// </summary>
        /// <param name="query">Thông tin lọc</param>
        /// <returns>Danh sách user hiện tại</returns>
        /// <remarks>
        /// Lấy danh sáchuser hiện tại với bộ lọc tùy chọn
        ///
        /// Sample request:
        ///
        ///     GET /api/topic/list
        ///     GET /api/topic/list?semesterId=1
        ///     GET /api/topic/list?categoryId=1
        ///     GET /api/topic/list?semesterId=1&categoryId=1
        ///
        /// </remarks>
        [Authorize(Roles = SystemRoleConstants.Administrator)]
        [HttpGet("users")]
        [SwaggerOperation(
            Summary = "Lấy danh sách user",
            Description = "Lấy danh sáchuser"
        )]
        [SwaggerResponse(200, "Lấy danh sách thành công")]
        [SwaggerResponse(401, "Lỗi xác thực")]
        [SwaggerResponse(500, "Lỗi máy chủ nội bộ")]
        [Consumes("application/json")]
        [Produces("application/json")]
        public async Task<IActionResult> GetUsers([FromQuery] GetUsersQueryDTO query)
        {
            try
            {
                var result = await _accountService.GetUsers(query);
                return ProcessServiceResponse(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting all users");
                return Error(ConstantModel.ErrorMessage);
            }
        }

        /// <summary>
        /// Xoá người dùng
        /// </summary>
        /// <param name="userId">ID của người dùng</param>
        /// <returns>Thông tin kết quả</returns>
        /// <remarks>
        /// xoá người dùng
        ///
        /// Sample request:
        ///
        ///     GET /api/topic/list
        ///     GET /api/topic/list?semesterId=1
        ///     GET /api/topic/list?categoryId=1
        ///     GET /api/topic/list?semesterId=1&categoryId=1
        ///
        /// </remarks>
        [Authorize(Roles = SystemRoleConstants.Administrator)]
        [HttpDelete("users/{userId}")]
        [SwaggerOperation(
            Summary = "xoá user",
            Description = "Xoá người dùng"
        )]
        [SwaggerResponse(200, "Xoá người dùng thành công")]
        [SwaggerResponse(401, "Lỗi xác thực")]
        [SwaggerResponse(500, "Lỗi máy chủ nội bộ")]
        [Consumes("application/json")]
        [Produces("application/json")]
        public async Task<IActionResult> SoftDeleteUsers([FromRoute] int userId)
        {
            try
            {
                var result = await _accountService.SoftDeleteUser(userId);
                return ProcessServiceResponse(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while soft deleting user");
                return Error(ConstantModel.ErrorMessage);
            }
        }

        /// <summary>
        /// Lấy danh sách quyền của tài khoản đăng nhập hiện tại
        /// </summary>
        /// <param name="query">Thông tin lọc</param>
        /// <returns>Danh sách quyền của tài khoản đăng nhập hiện tại</returns>
        /// <remarks>
        /// Lấy danh sách quyền của tài khoản đăng nhập hiện tại với bộ lọc tùy chọn
        ///
        /// Sample request:
        ///
        ///     GET /api/topic/list
        ///     GET /api/topic/list?semesterId=1
        ///     GET /api/topic/list?categoryId=1
        ///     GET /api/topic/list?semesterId=1&categoryId=1
        ///
        /// </remarks>
        [Authorize]
        [HttpGet("my-user-roles")]
        [SwaggerOperation(
            Summary = "Lấy danh sách quyền của user hiện tại",
            Description = "Lấy danh sách quyền của user hiện tại"
        )]
        [SwaggerResponse(200, "Lấy danh sách quyền thành công")]
        [SwaggerResponse(401, "Lỗi xác thực")]
        [SwaggerResponse(500, "Lỗi máy chủ nội bộ")]
        [Consumes("application/json")]
        [Produces("application/json")]
        public async Task<IActionResult> GetMyUserRoles()
        {
            try
            {
                var result = await _accountService.GetAllUserRoles(UserId);
                return ProcessServiceResponse(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting all user roles");
                return Error(ConstantModel.ErrorMessage);
            }
        }
    }
}