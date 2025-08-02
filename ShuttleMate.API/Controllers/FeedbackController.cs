using Microsoft.AspNetCore.Mvc;
using ShuttleMate.Contract.Services.Interfaces;
using ShuttleMate.Core.Bases;
using ShuttleMate.Core.Constants;
using ShuttleMate.ModelViews.FeedbackModelViews;

namespace ShuttleMate.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FeedbackController : ControllerBase
    {
        private readonly IFeedbackService _feedbackService;

        public FeedbackController(IFeedbackService feedbackService)
        {
            _feedbackService = feedbackService;
        }

        /// <summary>
        /// Lấy toàn bộ đánh giá (Admin).
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetAllFeedbacksAdmin()
        {
            return Ok(new BaseResponseModel<IEnumerable<ResponseFeedbackModel>>(
                statusCode: StatusCodes.Status200OK,
                code: ResponseCodeConstants.SUCCESS,
                data: await _feedbackService.GetAllAdminAsync()));
        }

        /// <summary>
        /// Lấy toàn bộ đánh giá của tôi.
        /// </summary>
        [HttpGet("my")]
        public async Task<IActionResult> GetAllFeedbacksMy()
        {
            return Ok(new BaseResponseModel<IEnumerable<ResponseFeedbackModel>>(
                statusCode: StatusCodes.Status200OK,
                code: ResponseCodeConstants.SUCCESS,
                data: await _feedbackService.GetAllMyAsync()));
        }

        /// <summary>
        /// Lấy đánh giá bằng id.
        /// </summary>
        /// <param name="id">ID của đánh giá cần lấy</param>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetFeedbackById(Guid id)
        {
            return Ok(new BaseResponseModel<ResponseFeedbackModel>(
                statusCode: StatusCodes.Status200OK,
                code: ResponseCodeConstants.SUCCESS,
                data: await _feedbackService.GetByIdAsync(id)));
        }

        /// <summary>
        /// Tạo một đánh giá mới.
        /// </summary>
        /// <param name="model">Thông tin đánh giá cần tạo</param>
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
        /// <param name="id">id của đánh giá cần xóa.</param>
        ///
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteFeedback(Guid id)
        {
            await _feedbackService.DeleteAsync(id);
            return Ok(new BaseResponseModel<string?>(
                statusCode: StatusCodes.Status200OK,
                code: ResponseCodeConstants.SUCCESS,
                message: "Xóa đánh giá thành công."));
        }
    }
}
