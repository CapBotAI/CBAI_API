// CapBot.api/Controllers/DocumentValidationController.cs
using System.Threading.Tasks;
using App.BLL.Interfaces;
using App.Entities.DTOs.DocumentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace CapBot.api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DocumentValidationController : ControllerBase
    {
        private readonly IDocumentValidationService _service;

        public DocumentValidationController(IDocumentValidationService service)
        {
            _service = service;
        }

        /// <summary>
        /// So sánh 1 file submit với template (.docx). Trả JSON các mục: Tên mục, Trạng thái, Ghi chú.
        /// </summary>
        [HttpPost]
        [Consumes("multipart/form-data")]
        [ProducesResponseType(typeof(DocumentValidationRequest), StatusCodes.Status200OK)]
        public async Task<IActionResult> Validate([FromForm] DocumentValidationRequest req)
        {
            var result = await _service.ValidateAsync(req);
            return Ok(result);
        }
    }
}
