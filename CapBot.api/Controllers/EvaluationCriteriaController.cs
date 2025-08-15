using Microsoft.AspNetCore.Mvc;
using App.BLL.Interfaces;
using App.Entities.DTOs.EvaluationCriteria;
using App.Commons.BaseAPI;
using App.Commons;
using App.Commons.Paging;
using Swashbuckle.AspNetCore.Annotations;
using Microsoft.AspNetCore.Authorization;

namespace CapBot.api.Controllers
{
    [Route("api/evaluation-criteria")]
    [ApiController]
    [Authorize]
    public class EvaluationCriteriaController : BaseAPIController
    {
        private readonly IEvaluationCriteriaService _evaluationCriteriaService;
        private readonly ILogger<EvaluationCriteriaController> _logger;

        public EvaluationCriteriaController(
            IEvaluationCriteriaService evaluationCriteriaService,
            ILogger<EvaluationCriteriaController> logger)
        {
            _evaluationCriteriaService = evaluationCriteriaService;
            _logger = logger;
        }

        /// <summary>
        /// Tạo mới tiêu chí đánh giá
        /// </summary>
        /// <param name="createDTO">Thông tin tiêu chí đánh giá</param>
        /// <returns>Tiêu chí đánh giá được tạo</returns>
        [HttpPost]
        [SwaggerOperation(
            Summary = "Tạo mới tiêu chí đánh giá",
            Description = "Tạo tiêu chí đánh giá mới với thông tin đầy đủ"
        )]
        [SwaggerResponse(201, "Tạo tiêu chí thành công")]
        [SwaggerResponse(400, "Dữ liệu đầu vào không hợp lệ")]
        [SwaggerResponse(409, "Tên tiêu chí đã tồn tại")]
        [SwaggerResponse(500, "Lỗi máy chủ nội bộ")]
        public async Task<IActionResult> Create([FromBody] CreateEvaluationCriteriaDTO createDTO)
        {
            if (!ModelState.IsValid)
            {
                return ModelInvalid();
            }

            try
            {
                var result = await _evaluationCriteriaService.CreateAsync(createDTO);
                return ProcessServiceResponse(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while creating evaluation criteria with name {Name}", createDTO.Name);
                return Error(ConstantModel.ErrorMessage);
            }
        }

        /// <summary>
        /// Cập nhật tiêu chí đánh giá
        /// </summary>
        /// <param name="updateDTO">Thông tin cập nhật tiêu chí đánh giá</param>
        /// <returns>Tiêu chí đánh giá được cập nhật</returns>
        [HttpPut]
        [SwaggerOperation(
            Summary = "Cập nhật tiêu chí đánh giá",
            Description = "Cập nhật thông tin tiêu chí đánh giá"
        )]
        [SwaggerResponse(200, "Cập nhật tiêu chí thành công")]
        [SwaggerResponse(400, "Dữ liệu đầu vào không hợp lệ")]
        [SwaggerResponse(404, "Không tìm thấy tiêu chí")]
        [SwaggerResponse(409, "Tên tiêu chí đã tồn tại")]
        [SwaggerResponse(500, "Lỗi máy chủ nội bộ")]
        public async Task<IActionResult> Update([FromBody] UpdateEvaluationCriteriaDTO updateDTO)
        {
            if (!ModelState.IsValid)
            {
                return ModelInvalid();
            }

            try
            {
                var result = await _evaluationCriteriaService.UpdateAsync(updateDTO);
                return ProcessServiceResponse(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while updating evaluation criteria with ID {Id}", updateDTO.Id);
                return Error(ConstantModel.ErrorMessage);
            }
        }

        /// <summary>
        /// Xóa tiêu chí đánh giá
        /// </summary>
        /// <param name="id">ID của tiêu chí cần xóa</param>
        /// <returns>Kết quả xóa</returns>
        [HttpDelete("{id}")]
        [SwaggerOperation(
            Summary = "Xóa tiêu chí đánh giá",
            Description = "Xóa tiêu chí đánh giá theo ID"
        )]
        [SwaggerResponse(200, "Xóa tiêu chí thành công")]
        [SwaggerResponse(404, "Không tìm thấy tiêu chí")]
        [SwaggerResponse(409, "Tiêu chí đang được sử dụng")]
        [SwaggerResponse(500, "Lỗi máy chủ nội bộ")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var result = await _evaluationCriteriaService.DeleteAsync(id);
                return ProcessServiceResponse(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while deleting evaluation criteria with ID {Id}", id);
                return Error(ConstantModel.ErrorMessage);
            }
        }

        /// <summary>
        /// Lấy thông tin tiêu chí đánh giá theo ID
        /// </summary>
        /// <param name="id">ID của tiêu chí</param>
        /// <returns>Thông tin tiêu chí đánh giá</returns>
        [HttpGet("{id}")]
        [SwaggerOperation(
            Summary = "Lấy thông tin tiêu chí đánh giá theo ID",
            Description = "Lấy thông tin chi tiết của tiêu chí đánh giá"
        )]
        [SwaggerResponse(200, "Lấy thông tin thành công")]
        [SwaggerResponse(404, "Không tìm thấy tiêu chí")]
        [SwaggerResponse(500, "Lỗi máy chủ nội bộ")]
        public async Task<IActionResult> GetById(int id)
        {
            try
            {
                var result = await _evaluationCriteriaService.GetByIdAsync(id);
                return ProcessServiceResponse(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting evaluation criteria with ID {Id}", id);
                return Error(ConstantModel.ErrorMessage);
            }
        }

        /// <summary>
        /// Lấy danh sách tiêu chí đánh giá có phân trang
        /// </summary>
        /// <param name="pagingModel">Thông tin phân trang</param>
        /// <returns>Danh sách tiêu chí đánh giá có phân trang</returns>
        [HttpGet]
        [SwaggerOperation(
            Summary = "Lấy danh sách tiêu chí đánh giá có phân trang",
            Description = "Lấy danh sách tiêu chí đánh giá với phân trang"
        )]
        [SwaggerResponse(200, "Lấy danh sách thành công")]
        [SwaggerResponse(500, "Lỗi máy chủ nội bộ")]
        public async Task<IActionResult> GetAll([FromQuery] PagingModel pagingModel)
        {
            try
            {
                var result = await _evaluationCriteriaService.GetAllAsync(pagingModel);
                return ProcessServiceResponse(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting evaluation criteria list");
                return Error(ConstantModel.ErrorMessage);
            }
        }

        /// <summary>
        /// Lấy tất cả tiêu chí đánh giá đang hoạt động
        /// </summary>
        /// <returns>Danh sách tất cả tiêu chí đánh giá đang hoạt động</returns>
        [HttpGet("active")]
        [SwaggerOperation(
            Summary = "Lấy tất cả tiêu chí đánh giá đang hoạt động",
            Description = "Lấy danh sách tất cả tiêu chí đánh giá đang hoạt động (không có phân trang)"
        )]
        [SwaggerResponse(200, "Lấy danh sách thành công")]
        [SwaggerResponse(500, "Lỗi máy chủ nội bộ")]
        public async Task<IActionResult> GetAllActive()
        {
            try
            {
                var result = await _evaluationCriteriaService.GetAllActiveAsync();
                return ProcessServiceResponse(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting active evaluation criteria");
                return Error(ConstantModel.ErrorMessage);
            }
        }
    }
}