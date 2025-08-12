using System;
using App.Commons.ResponseModel;
using App.Entities.Entities.Core;
using Microsoft.AspNetCore.Identity;

namespace App.DAL.Interfaces;

public interface IIdentityRepository
{
    Task<User> GetByEmailAsync(string email);
    Task<BaseResponseModel> AddUserAsync(User user, string password, string role);
    Task<long> AddUserAsync(User dto, string password);
    Task<bool> UpdateAsync(User dto);
    Task<User?> GetByIdAsync(long id);
    Task<User> GetByExternalIdAsync(string id);
    Task<bool> CheckPasswordAsync(User dto, string password);
    Task<bool> HasPasswordAsync(User dto);
    Task<IdentityResult> AddPasswordAsync(User dto, string password);

    /// <summary>
    /// lấy thông tin user theo điện thoại
    /// </summary>
    /// <param name="phoneNumber">số điện thoại</param>
    /// <returns></returns>
    Task<User> GetByPhoneAsync(string phoneNumber);

    /// <summary>
    /// xác thực email
    /// </summary>
    /// <param name="userId"></param>
    /// <param name="code"></param>
    /// <returns></returns>
    Task<bool> ConfirmEmailAsync(string userId, string code);

    /// <summary>
    /// Verify Email with Get http method
    /// </summary>
    /// <param name="user"></param>
    /// <param name="token"></param>
    /// <returns></returns>
    Task<bool> VerifyEmailAsync(User user, string token);

    /// <summary>
    /// thay đổi mật khẩu
    /// </summary>
    /// <param name="userId">id user cần thay đổi mật khẩu</param>
    /// <param name="passwordNew">mật khẩu mới</param>
    /// <returns></returns>
    Task<bool> ChangePassword(string userId, string passwordNew);

    /// <summary>
    /// thêm role cho ứng dụng
    /// </summary>
    /// <param name="roleName"></param>
    /// <param name="isAdmin"></param>
    /// <returns></returns>
    Task<bool> CreateUpdateRoleAsync(string roleName, bool isAdmin);

    /// <summary>
    /// thêm role cho người dùng theo tên role
    /// </summary>
    /// <param name="userId"></param>
    /// <param name="roleName"></param>
    /// <returns></returns>
    Task<bool> AddRoleByNameAsync(string userId, string roleName);

    /// <summary>
    /// generate email confirm token
    /// </summary>
    /// <param name="user"></param>
    /// <returns></returns>
    Task<string> GenerateEmailConfirmationTokenAsync(User user);

    Task<string> GeneratePasswordResetTokenAsync(User user);
    Task<bool> ResetPasswordAsync(string userId, string token, string newPassword);

    /// <summary>
    /// Is user in role
    /// </summary>
    /// <param name="user"></param>
    /// <param name="role"></param>
    /// <returns></returns>
    Task<bool> IsUserInRole(User user, string role);

    /// <summary>
    /// xóa quyền của user
    /// </summary>
    /// <param name="userId"></param>
    /// <returns></returns>
    Task<bool> DeleteRoleByUser(long userId);

    /// <summary>
    /// xoá người dùng
    /// </summary>
    /// <param name="userId"></param>
    /// <returns></returns>
    Task<IdentityResult> DeleteUser(long userId);

    /// <summary>
    /// DeleteListRole
    /// </summary>
    /// <param name="ids"></param>
    /// <returns></returns>
    Task<bool> DeleteListRole(long[] ids);

    /// <summary>
    /// Get list roles by userId
    /// </summary>
    /// <param name="userId">The user identified.</param>
    /// <returns></returns>
    Task<string[]> GetRolesAsync(long userId);

    /// <summary>
    /// Get list roles by userId
    /// </summary>
    /// <param name="userId">The user identified.</param>
    /// <returns></returns>
    Task<List<Role>> GetUserRolesAsync(long userId);

    /// <summary>
    /// Verified user can be access to the function.
    /// </summary>
    Task<bool> VerifyPermission(long userId, string claim);

    /// <summary>
    /// lấy toàn bộ role quản trị
    /// </summary>
    /// <returns></returns>
    Task<List<Role>> GetRolesAdmin();

    /// <summary>
    /// Get user bằng sử dụng email hoặc username
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    Task<User?> GetByEmailOrUserNameAsync(string input);

    /// <summary>
    /// Lấy ra danh sách account
    /// </summary>
    /// <param name="dto"></param>
    /// <returns></returns>
    // Task<List<User>> GetAccounts(AccountGetListDTO dto);
}