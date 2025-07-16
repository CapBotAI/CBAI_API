using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using App.Commons.ResponseModel;
using App.Commons.Utils;
using FS.Commons;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace App.Commons.BaseAPI;

public class BaseAPIController : ControllerBase
{

    /// <summary>
    /// Xử lý BaseResponseModel generic và trả về ActionResult với status code chính xác
    /// </summary>
    /// <typeparam name="T">Loại dữ liệu trong response</typeparam>
    /// <param name="response">BaseResponseModel từ service</param>
    /// <returns>ActionResult với status code tương ứng</returns>
    protected ActionResult HandleServiceResponse<T>(BaseResponseModel<T> response) where T : class
    {
        if (response == null)
        {
            return Error(ConstantModel.ErrorMessage);
        }

        var httpStatusCode = MapToHttpStatusCode(response.StatusCode);
        var fsResponse = new FSResponse
        {
            Data = response.Data,
            StatusCode = (System.Net.HttpStatusCode)response.StatusCode,
            Message = response.Message,
            Success = response.IsSuccess
        };

        return new ObjectResult(fsResponse)
        {
            StatusCode = httpStatusCode
        };
    }

    /// <summary>
    /// Xử lý BaseResponseModel không generic và trả về ActionResult với status code chính xác
    /// </summary>
    /// <param name="response">BaseResponseModel từ service</param>
    /// <returns>ActionResult với status code tương ứng</returns>
    protected ActionResult HandleServiceResponse(BaseResponseModel response)
    {
        if (response == null)
        {
            return Error(ConstantModel.ErrorMessage);
        }

        var httpStatusCode = MapToHttpStatusCode(response.StatusCode);
        var fsResponse = new FSResponse
        {
            Data = null,
            StatusCode = (System.Net.HttpStatusCode)response.StatusCode,
            Message = response.Message,
            Success = response.IsSuccess
        };

        return new ObjectResult(fsResponse)
        {
            StatusCode = httpStatusCode
        };
    }

    /// <summary>
    /// Xử lý BaseResponseModel khi thành công
    /// </summary>
    /// <typeparam name="T">Loại dữ liệu trong response</typeparam>
    /// <param name="response">BaseResponseModel từ service</param>
    /// <returns>ActionResult với status code thành công</returns>
    protected ActionResult HandleSuccessResponse<T>(BaseResponseModel<T> response) where T : class
    {
        if (response == null || !response.IsSuccess)
        {
            return Error(ConstantModel.ErrorMessage);
        }

        return HandleServiceResponse(response);
    }

    /// <summary>
    /// Xử lý BaseResponseModel khi có lỗi
    /// </summary>
    /// <typeparam name="T">Loại dữ liệu trong response</typeparam>
    /// <param name="response">BaseResponseModel từ service</param>
    /// <returns>ActionResult với status code lỗi</returns>
    protected ActionResult HandleErrorResponse<T>(BaseResponseModel<T> response) where T : class
    {
        if (response == null)
        {
            return Error(ConstantModel.ErrorMessage);
        }

        return HandleServiceResponse(response);
    }

    /// <summary>
    /// Map status code từ int sang HTTP status code
    /// </summary>
    /// <param name="statusCode">Status code từ service</param>
    /// <returns>HTTP status code tương ứng</returns>
    private int MapToHttpStatusCode(int statusCode)
    {
        return statusCode switch
        {
            >= 200 and < 300 => statusCode, // 2xx Success
            >= 300 and < 400 => statusCode, // 3xx Redirection
            >= 400 and < 500 => statusCode, // 4xx Client Error
            >= 500 and < 600 => statusCode, // 5xx Server Error
            _ => StatusCodes.Status500InternalServerError // Default fallback
        };
    }

    /// <summary>
    /// Phương thức tiện lợi để xử lý response từ service một cách tự động
    /// </summary>
    /// <typeparam name="T">Loại dữ liệu trong response</typeparam>
    /// <param name="response">BaseResponseModel từ service</param>
    /// <returns>ActionResult phù hợp dựa trên IsSuccess</returns>
    protected ActionResult ProcessServiceResponse<T>(BaseResponseModel<T> response) where T : class
    {
        if (response == null)
        {
            return Error(ConstantModel.ErrorMessage);
        }

        return response.IsSuccess ? HandleSuccessResponse(response) : HandleErrorResponse(response);
    }

    /// <summary>
    /// Phương thức tiện lợi cho BaseResponseModel không generic
    /// </summary>
    /// <param name="response">BaseResponseModel từ service</param>
    /// <returns>ActionResult phù hợp dựa trên IsSuccess</returns>
    protected ActionResult ProcessServiceResponse(BaseResponseModel response)
    {
        if (response == null)
        {
            return Error(ConstantModel.ErrorMessage);
        }

        return response.IsSuccess ? HandleServiceResponse(response) : HandleServiceResponse(response);
    }



    /// <summary>
    /// Errors the specified message.
    /// </summary>
    /// <param name="message">The message.</param>
    /// <param name="data">The extend data.</param>
    /// <returns></returns>
    protected ActionResult Error(string message, object data = null)
    {
        return new ObjectResult(new FSResponse
        {
            Data = data,
            StatusCode = System.Net.HttpStatusCode.InternalServerError,
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
        return new UnprocessableEntityObjectResult(new FSResponse
        {
            Errors = errors,
            StatusCode = System.Net.HttpStatusCode.UnprocessableEntity,
            Message = ConstantModel.ModelInvalid
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
