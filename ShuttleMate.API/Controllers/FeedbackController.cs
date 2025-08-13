using Microsoft.AspNetCore.Mvc;
using ShuttleMate.Contract.Services.Interfaces;
using ShuttleMate.Core.Bases;
using ShuttleMate.Core.Constants;
using ShuttleMate.ModelViews.FeedbackModelViews;

namespace ShuttleMate.API.Controllers
{
    [Route("api/feedback")]
    [ApiController]
    public class FeedbackController : ControllerBase
    {
        private readonly IFeedbackService _feedbackService;

        public FeedbackController(IFeedbackService feedbackService)
        {
            _feedbackService = feedbackService;
        }

        /// <summary>
        /// Lấy toàn bộ đánh giá.
        /// </summary>
        /// <param name="search">Từ khóa tìm kiếm theo tên khuyến mãi (tùy chọn).</param>
        /// <param name="category">Loại đánh giá: SHUTTLE_OPERATION, APP_TECHNICAL, OTHER (tùy chọn).</param>
        /// <param name="from">Lọc từ ngày (tùy chọn).</param>
        /// <param name="to">Lọc đến ngày (tùy chọn).</param>
        /// <param name="userId">ID người dùng tạo đánh giá (tùy chọn).</param>
        /// <param name="tripId">ID chuyến đi được đánh giá (tùy chọn).</param>
        /// <param name="minRating">Điểm đánh giá tối thiểu (1–5, tùy chọn).</param>
        /// <param name="maxRating">Điểm đánh giá tối đa (1–5, tùy chọn).</param>
        /// <param name="sortAsc">Sắp xếp tăng dần theo ngày tạo (true) hoặc giảm dần (false, mặc định).</param>
        /// <param name="page">Trang (mặc định 0).</param>
        /// <param name="pageSize">Số bản ghi mỗi trang (mặc định 10).</param>
        [HttpGet]
        public async Task<IActionResult> GetAllFeedbacks(
        [FromQuery] string? search,
        [FromQuery] string? category,
        [FromQuery] DateOnly? from,
        [FromQuery] DateOnly? to,
        [FromQuery] Guid? userId,
        [FromQuery] Guid? tripId,
        [FromQuery] int? minRating,
        [FromQuery] int? maxRating,
        [FromQuery] bool sortAsc = false,
        [FromQuery] int page = 0,
        [FromQuery] int pageSize = 10)
        {
            return Ok(new BaseResponseModel<BasePaginatedList<ResponseFeedbackModel>>(
                statusCode: StatusCodes.Status200OK,
                code: ResponseCodeConstants.SUCCESS,
                data: await _feedbackService.GetAllAsync(search, category, from, to, userId, tripId, minRating, maxRating, sortAsc, page, pageSize)));
        }

        /// <summary>
        /// Lấy chi tiết đánh giá.
        /// </summary>
        [HttpGet("{feedbackId}")]
        public async Task<IActionResult> GetFeedbackById(Guid feedbackId)
        {
            return Ok(new BaseResponseModel<ResponseFeedbackModel>(
                statusCode: StatusCodes.Status200OK,
                code: ResponseCodeConstants.SUCCESS,
                data: await _feedbackService.GetByIdAsync(feedbackId)));
        }

        /// <summary>
        /// Tạo một đánh giá mới.
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> CreateFeedback(CreateFeedbackModel model)
        {
            await _feedbackService.CreateAsync(model);
            return Ok(new BaseResponseModel<string?>(
              statusCode: StatusCodes.Status200OK,
              code: ResponseCodeConstants.SUCCESS,
              message: "Tạo mới đánh giá thành công."));
        }

        /// <summary>
        /// Xóa một đánh giá.
        /// </summary>
        [HttpDelete("{feedbackId}")]
        public async Task<IActionResult> DeleteFeedback(Guid feedbackId)
        {
            await _feedbackService.DeleteAsync(feedbackId);
            return Ok(new BaseResponseModel<string?>(
                statusCode: StatusCodes.Status200OK,
                code: ResponseCodeConstants.SUCCESS,
                message: "Xóa đánh giá thành công."));
        }
    }
}
