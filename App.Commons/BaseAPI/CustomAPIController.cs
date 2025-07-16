using App.Commons.ResponseModel;
using Microsoft.AspNetCore.Mvc;

namespace App.Commons.BaseAPI
{
    [Route("api/[controller]")]
    [ApiController]
    public class CustomAPIController : ControllerBase
    {
        protected IActionResult BadRequestWithError(string errorCode, string message)
        {
            return BadRequest(new ErrorResponse(errorCode, message));
        }

        protected IActionResult UnauthorizedWithError(string errorCode, string message)
        {
            return StatusCode(401, new ErrorResponse(errorCode, message));
        }

        protected IActionResult ForbiddenWithError(string errorCode, string message)
        {
            return StatusCode(403, new ErrorResponse(errorCode, message));
        }

        protected IActionResult NotFoundWithError(string errorCode, string message)
        {
            return NotFound(new ErrorResponse(errorCode, message));
        }

        protected IActionResult InternalServerErrorWithError(string errorCode, string message)
        {
            return StatusCode(500, new ErrorResponse(errorCode, message));
        }

        protected IActionResult HandleValidationErrors()
        {
            if (!ModelState.IsValid)
            {
                // Get the first validation error
                var firstError = ModelState
                    .Where(x => x.Value?.Errors.Count > 0)
                    .SelectMany(x => x.Value.Errors)
                    .Select(x => x.ErrorMessage)
                    .FirstOrDefault();

                var errorMessage = firstError ?? ErrorMessages.MissingInvalidInput;

                return BadRequest(new ErrorResponse(ErrorCodes.HB40001, errorMessage));
            }

            return BadRequest(new ErrorResponse(ErrorCodes.HB40001, ErrorMessages.MissingInvalidInput));
        }

        /// <summary>
        /// Check model state and return error response if invalid
        /// </summary>
        /// <returns>True if model is valid, False if invalid (and sets response)</returns>
        protected bool ValidateModel()
        {
            return ModelState.IsValid;
        }

        /// <summary>
        /// Get validation error response if model state is invalid
        /// </summary>
        protected IActionResult GetValidationErrorResponse()
        {
            return HandleValidationErrors();
        }
    }
}
