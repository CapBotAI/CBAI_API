using Microsoft.AspNetCore.Mvc;
using App.BLL.Interfaces;
using App.Entities.DTOs.Auth;
using App.Commons.BaseAPI;
using App.Commons;

namespace CapBot.api.Controllers
{
    [Route("api")]
    [ApiController]
    public class AuthController : CustomAPIController
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("auth")]
        public async Task<IActionResult> Auth([FromBody] LoginDTO loginDTO)
        {
            if (!ModelState.IsValid)
            {
                return GetValidationErrorResponse();
            }

            try
            {
                var result = await _authService.SignInAsyncV2(loginDTO);

                if (!result.IsSuccess)
                {
                    Console.WriteLine(result.Message);
                    return UnauthorizedWithError(ErrorCodes.HB40101, ErrorMessages.InvalidEmailPassword);
                }

                return Ok(result.Data);
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Auth Error: " + ex.Message);
                Console.WriteLine("Stack Trace: " + ex.StackTrace);
                Console.ResetColor();
                return InternalServerErrorWithError(ErrorCodes.HB50001, ErrorMessages.InternalServerError);
            }
        }
    }
}
