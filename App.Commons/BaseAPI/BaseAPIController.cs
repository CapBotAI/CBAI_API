using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using App.Commons.Utils;
using FS.Commons;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace App.Commons.BaseAPI;

public class BaseAPIController : ControllerBase
{
    /// <summary>
    /// Errors the specified message.
    /// </summary>
    /// <param name="message">The message.</param>
    /// <param name="data">The extend data.</param>
    /// <returns></returns>
    protected ActionResult Error(string message, object data = null)
    {
        return new BadRequestObjectResult(new FSResponse
        {
            Data = data,
            StatusCode = System.Net.HttpStatusCode.BadRequest,
            Message = message
        });
    }

    protected ActionResult GetNotFound(string message, object data = null)
    {
        return new NotFoundObjectResult(new FSResponse
        {
            Data = data,
            Message = message,
            StatusCode = System.Net.HttpStatusCode.NotFound
        });
    }

    protected ActionResult GetUnAuthorized(string message, object data = null)
    {
        return new ObjectResult(new FSResponse
        {
            Data = data,
            Message = message,
            StatusCode = System.Net.HttpStatusCode.Unauthorized
        })
        {
            StatusCode = StatusCodes.Status401Unauthorized
        };
    }

    protected ActionResult GetForbidden()
    {
        return new ForbidResult();
    }

    /// <summary>
    /// Gets the data failed.
    /// </summary>
    /// <returns></returns>
    protected ActionResult GetError()
    {
        return Error(ConstantModel.GetDataFailed);
    }

    /// <summary>
    /// Gets the data failed.
    /// </summary>
    /// <returns></returns>
    protected ActionResult GetError(string message)
    {
        return Error(message);
    }

    /// <summary>
    /// Saves the data failed.
    /// </summary>
    /// <returns></returns>
    protected ActionResult SaveError(object data = null)
    {
        return Error(ConstantModel.SaveDataFailed, data);
    }

    /// <summary>
    /// Models the invalid.
    /// </summary>
    /// <returns></returns>
    protected ActionResult ModelInvalid()
    {
        var errors = ModelState.Where(m => m.Value.Errors.Count > 0)
            .ToDictionary(
                kvp => kvp.Key.ToCamelCase(),
                kvp => kvp.Value.Errors.Select(e => e.ErrorMessage).First()).ToList();
        return new BadRequestObjectResult(new FSResponse
        {
            Errors = errors,
            StatusCode = System.Net.HttpStatusCode.BadRequest,
            Message = ConstantModel.SaveDataFailed
        });
    }

    /// <summary>
    /// Successes request.
    /// </summary>
    /// <param name="data">The data.</param>
    /// <param name="message">The message.</param>
    /// <returns></returns>
    protected ActionResult Success(object data, string message)
    {
        return new OkObjectResult(new FSResponse
        {
            Data = data,
            StatusCode = System.Net.HttpStatusCode.OK,
            Message = message,
            Success = true
        });
    }

    /// <summary>
    /// Gets the data successfully.
    /// </summary>
    /// <param name="data">The data.</param>
    /// <returns></returns>
    protected ActionResult GetSuccess(object data)
    {
        return Success(data, ConstantModel.GetDataSuccess);
    }

    /// <summary>
    /// Saves the data successfully
    /// </summary>
    /// <param name="data">The data.</param>
    /// <returns></returns>
    protected ActionResult SaveSuccess(object data)
    {
        return Success(data, ConstantModel.SaveDataSuccess);
    }

    /// <summary>
    /// Get the loged in UserName;
    /// </summary>
    protected string? UserName => User.FindFirst(ClaimTypes.Name)?.Value;

    /// <summary>
    /// Get the logged in user email.
    /// </summary>
    protected string? UserEmail => User.FindFirst(ClaimTypes.Email)?.Value;

    /// <summary>
    /// Get the loged in UserId;
    /// </summary>
    protected long UserId
    {
        get
        {
            var id = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            long.TryParse(id, out long userId);
            return userId;
        }
    }

    /// <summary>
    /// Get the logged Manager or Employee
    /// </summary>
    protected string ManagerOrEmpId
    {
        get
        {
            var id = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return id;
        }
    }

    /// <summary>
    /// Get jti of logged in user
    /// </summary>
    protected string? Jti => User.FindFirst(JwtRegisteredClaimNames.Jti)?.Value;

    protected bool IsAdmin
    {
        get
        {
            var isadmin = User.FindFirst(ConstantModel.IS_ADMIN)?.Value;
            bool.TryParse(isadmin, out bool isAdmin);
            return isAdmin;
        }
    }

    protected bool IsManager
    {
        get
        {
            var isManager = User.FindFirst(ConstantModel.IS_MANAGER)?.Value;
            bool.TryParse(isManager, out bool isManagerReal);
            return isManagerReal;
        }
    }

    protected bool IsStaff
    {
        get
        {
            var isStaff = User.FindFirst(ConstantModel.IS_STAFF)?.Value;
            bool.TryParse(isStaff, out bool isStaffReal);
            return isStaffReal;
        }
    }


    protected bool IsAuthor
    {
        get
        {
            var isCustomer = User.FindFirst(ConstantModel.IS_AUTHOR)?.Value;
            bool.TryParse(isCustomer, out bool isCustomerReal);
            return isCustomerReal;
        }
    }

    protected bool IsRemember
    {
        get
        {
            var isRemeber = User.FindFirst(ConstantModel.IS_REMEMBER)?.Value;
            bool.TryParse(isRemeber, out bool is_remember);
            return is_remember;
        }
    }

    /// <summary>
    /// It is used to check whether the token has been used or not.
    /// </summary>
    // protected async Task<bool> IsTokenInvoked()
    // {
    //     var serviceProvider = HttpContext.RequestServices;
    //     var identityBizLogic = serviceProvider.GetRequiredService<IIdentityBizLogic>();
    //     if (identityBizLogic != null)
    //     {
    //         var isInvoked = await identityBizLogic.IsTokenInvoked(Jti, UserId);
    //         if (isInvoked) return true;
    //         else return false;
    //     }
    //     return true;
    // }


}
