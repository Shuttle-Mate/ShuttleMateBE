using Microsoft.AspNetCore.Mvc;
using ShuttleMate.Contract.Services.Interfaces;
using ShuttleMate.Core.Bases;
using ShuttleMate.Core.Constants;
using ShuttleMate.ModelViews.Pagination;
using ShuttleMate.ModelViews.RouteModelViews;
using ShuttleMate.ModelViews.ShuttleModelViews;
using ShuttleMate.Services.Services;

namespace ShuttleMate.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ShuttleController : ControllerBase
    {
        private readonly IShuttleService _shuttleService;
        public ShuttleController(IShuttleService shuttleService)
        {
            _shuttleService = shuttleService;
        }

        [HttpPost]
        public async Task<IActionResult> CreateShuttle(ShuttleModel model)
        {
            await _shuttleService.CreateShuttle(model);
            return Ok(new BaseResponseModel<string>(
                statusCode: StatusCodes.Status200OK,
                code: ResponseCodeConstants.SUCCESS,
                message: "Tạo xe thành công!"
            ));
        }
        //[HttpGet]
        //public async Task<IActionResult> GetAllShuttle()
        //{
        //    var res = await _shuttleService.GetAll();
        //    return Ok(new BaseResponseModel<List<ResponseShuttleModel>>(
        //        statusCode: StatusCodes.Status200OK,
        //        code: ResponseCodeConstants.SUCCESS,
        //        data: res
        //    ));
        //}
        /// <summary>
        /// Lấy tất cả xe theo phân trang
        /// </summary>
        /// <param name="req">Thông tin truy vấn xe</param>

        [HttpGet]
        public async Task<IActionResult> GetAllShuttlePaging([FromQuery] GetShuttleQuery req)
        {
            var res = await _shuttleService.GetAllPaging(req);
            return Ok(new BaseResponseModel<PaginationResp<ResponseShuttleModel>>(
                statusCode: StatusCodes.Status200OK,
                code: ResponseCodeConstants.SUCCESS,
                data: res
            ));
        }
        [HttpGet("{shuttleId}")]
        public async Task<IActionResult> GetShuttleById(Guid shuttleId)
        {
            var res = await _shuttleService.GetById(shuttleId);
            return Ok(new BaseResponseModel<ResponseShuttleModel>(
                statusCode: StatusCodes.Status200OK,
                code: ResponseCodeConstants.SUCCESS,
                data: res
            ));
        }
        [HttpPatch]
        public async Task<IActionResult> UpdateShuttle(UpdateShuttleModel model)
        {
            await _shuttleService.UpdateShuttle(model);
            return Ok(new BaseResponseModel<string>(
                statusCode: StatusCodes.Status200OK,
                code: ResponseCodeConstants.SUCCESS,
                message: "Cập nhật xe thành công"
            ));
        }
        [HttpDelete]
        public async Task<IActionResult> DeleteShuttle(Guid shuttleId)
        {
            await _shuttleService.DeleteShuttle(shuttleId);
            return Ok(new BaseResponseModel<string>(
                statusCode: StatusCodes.Status200OK,
                code: ResponseCodeConstants.SUCCESS,
                message: "Xóa xe thành công"
            ));
        }
    }
}
