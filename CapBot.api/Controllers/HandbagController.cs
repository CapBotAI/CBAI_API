using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using App.BLL.Interfaces;
using App.Entities.DTOs.Handbag;
using System.Security.Claims;
using Microsoft.AspNetCore.OData.Query;
using App.Commons.BaseAPI;
using App.Commons;

namespace CapBot.api.Controllers
{
    [Route("api")]
    [ApiController]
    [Authorize]
    public class HandbagController : CustomAPIController
    {
        private readonly IHandbagService _handbagService;

        public HandbagController(IHandbagService handbagService)
        {
            _handbagService = handbagService;
        }

        /// <summary>
        /// GET /api/handbags - List all handbags with brand info
        /// Roles: administrator, moderator, developer, member
        /// </summary>
        [Authorize(Roles = "Administrator,Moderator,Developer,Member")]
        [HttpGet("handbags")]
        public async Task<IActionResult> GetAllHandbags()
        {
            try
            {
                var result = await _handbagService.GetAllHandbagsAsync();

                if (!result.IsSuccess)
                {
                    return InternalServerErrorWithError(ErrorCodes.HB50001, result.Message);
                }

                return Ok(result.Data);
            }
            catch (Exception)
            {
                return InternalServerErrorWithError(ErrorCodes.HB50001, ErrorMessages.InternalServerError);
            }
        }

        /// <summary>
        /// GET /api/handbags/{id} - Get handbag by ID
        /// Roles: administrator, moderator, developer, member
        /// </summary>
        [HttpGet("handbags/{id}")]
        [Authorize(Roles = "Administrator,Moderator,Developer,Member")]
        public async Task<IActionResult> GetHandbagById(int id)
        {
            try
            {
                var result = await _handbagService.GetHandbagByIdAsync(id);

                if (!result.IsSuccess)
                {
                    if (result.Message.Contains("not found"))
                    {
                        return NotFoundWithError(ErrorCodes.HB40401, ErrorMessages.HandbagNotFound);
                    }
                    return InternalServerErrorWithError(ErrorCodes.HB50001, result.Message);
                }

                return Ok(result.Data);
            }
            catch (Exception)
            {
                return InternalServerErrorWithError(ErrorCodes.HB50001, ErrorMessages.InternalServerError);
            }
        }

        /// <summary>
        /// POST /api/handbags - Create a new handbag
        /// Roles: administrator, moderator only
        /// </summary>
        [HttpPost("handbags")]
        [Authorize(Roles = "Administrator,Moderator")]
        public async Task<IActionResult> CreateHandbag([FromBody] CreateHandbagDTO createHandbagDTO)
        {
            if (!ModelState.IsValid)
            {
                return GetValidationErrorResponse();
            }

            try
            {
                var result = await _handbagService.CreateHandbagAsync(createHandbagDTO);

                if (!result.IsSuccess)
                {
                    if (result.Message.Contains("Brand not found"))
                    {
                        return NotFoundWithError(ErrorCodes.HB40401, ErrorMessages.BrandNotFound);
                    }
                    return BadRequestWithError(ErrorCodes.HB40001, result.Message);
                }

                return StatusCode(201, result.Data);
            }
            catch (Exception)
            {
                return InternalServerErrorWithError(ErrorCodes.HB50001, ErrorMessages.InternalServerError);
            }
        }

        /// <summary>
        /// PUT /api/handbags/{id} - Update an existing handbag
        /// Roles: administrator, moderator only
        /// </summary>
        [HttpPut("handbags/{id}")]
        [Authorize(Roles = "Administrator,Moderator")]
        public async Task<IActionResult> UpdateHandbag(int id, [FromBody] UpdateHandbagDTO updateHandbagDTO)
        {
            if (!ModelState.IsValid)
            {
                return GetValidationErrorResponse();
            }

            try
            {
                var result = await _handbagService.UpdateHandbagAsync(id, updateHandbagDTO);

                if (!result.IsSuccess)
                {
                    if (result.Message.Contains("not found"))
                    {
                        return NotFoundWithError(ErrorCodes.HB40401, ErrorMessages.HandbagNotFound);
                    }
                    if (result.Message.Contains("Brand not found"))
                    {
                        return NotFoundWithError(ErrorCodes.HB40401, ErrorMessages.BrandNotFound);
                    }
                    return BadRequestWithError(ErrorCodes.HB40001, result.Message);
                }

                return Ok(result.Data);
            }
            catch (Exception)
            {
                return InternalServerErrorWithError(ErrorCodes.HB50001, ErrorMessages.InternalServerError);
            }
        }

        /// <summary>
        /// DELETE /api/handbags/{id} - Delete a handbag
        /// Roles: administrator, moderator only
        /// </summary>
        [HttpDelete("handbags/{id}")]
        [Authorize(Roles = "Administrator,Moderator")]
        public async Task<IActionResult> DeleteHandbag(int id)
        {
            try
            {
                var result = await _handbagService.DeleteHandbagAsync(id);

                if (!result.IsSuccess)
                {
                    if (result.Message.Contains("not found"))
                    {
                        return NotFoundWithError(ErrorCodes.HB40401, ErrorMessages.HandbagNotFound);
                    }
                    return BadRequestWithError(ErrorCodes.HB40001, result.Message);
                }

                return Ok(new { message = result.Message });
            }
            catch (Exception)
            {
                return InternalServerErrorWithError(ErrorCodes.HB50001, ErrorMessages.InternalServerError);
            }
        }

        /// <summary>
        /// GET /api/handbags/search?modelName=...&material=... - Search handbags
        /// Roles: all roles with token
        /// Results grouped by brand name, OData supported
        /// </summary>
        [HttpGet("handbags/search")]
        [Authorize]
        public async Task<IActionResult> SearchHandbags([FromQuery] string? modelName, [FromQuery] string? material)
        {
            try
            {
                var result = await _handbagService.SearchHandbagsAsync(modelName, material);

                if (!result.IsSuccess)
                {
                    return InternalServerErrorWithError(ErrorCodes.HB50001, result.Message);
                }

                return Ok(result.Data);
            }
            catch (Exception)
            {
                return InternalServerErrorWithError(ErrorCodes.HB50001, ErrorMessages.InternalServerError);
            }
        }

        /// <summary>
        /// GET /api/handbags/search - Search handbags with OData support
        /// Supports: $filter, $orderby, $select, $expand, $top, $skip, $count
        /// Examples:
        /// - /api/handbags/search?$filter=contains(ModelName,'Elegant') and contains(Material,'Leather')
        /// - /api/handbags/search?$filter=Price gt 200&$orderby=Price desc
        /// - /api/handbags/search?$expand=Brand&$select=ModelName,Price,Brand/BrandName
        /// - /api/handbags/search?$top=10&$skip=0&$count=true
        /// Roles: all roles with token
        /// </summary>
        [HttpGet("search")]
        [EnableQuery(
            AllowedQueryOptions = AllowedQueryOptions.All,
            MaxTop = 200,
            MaxSkip = 1000,
            MaxExpansionDepth = 3,
            PageSize = 50)]
        [Authorize] // All roles with token
        public IActionResult SearchHandbags()
        {
            try
            {
                var handbags = _handbagService.GetHandbagsQueryable();
                return Ok(handbags);
            }
            catch (Exception)
            {
                return InternalServerErrorWithError(ErrorCodes.HB50001, ErrorMessages.InternalServerError);
            }
        }

        #region Private Helper Methods

        private IActionResult HandleValidationErrors()
        {
            var errors = ModelState
                .Where(x => x.Value.Errors.Count > 0)
                .SelectMany(x => x.Value.Errors)
                .Select(x => x.ErrorMessage)
                .ToList();

            if (errors.Any())
            {
                return BadRequest(new { error = errors.First() });
            }

            return BadRequest(new { error = "Invalid input data" });
        }

        #endregion
    }
}
