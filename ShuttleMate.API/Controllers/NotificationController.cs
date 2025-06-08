using Microsoft.AspNetCore.Mvc;
using ShuttleMate.Contract.Services.Interfaces;
using ShuttleMate.Core.Bases;
using ShuttleMate.Core.Constants;
using ShuttleMate.ModelViews.NotificationModelViews;
using ShuttleMate.ModelViews.ShuttleModelViews;
using ShuttleMate.Services.Services;

namespace ShuttleMate.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class NotificationController : ControllerBase
    {
        private readonly INotificationService _notificationService;

        public NotificationController(INotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        [HttpPost]
        public async Task<IActionResult> CreateNoti(NotiModel model)
        {
            await _notificationService.CreateNotification(model);
            return Ok(new BaseResponseModel<string>(
                statusCode: StatusCodes.Status200OK,
                code: ResponseCodeConstants.SUCCESS,
                data: "Tạo thông báo thành công!"
            ));
        }
        [HttpGet]
        public async Task<IActionResult> GetAllNoti()
        {
            var res = await _notificationService.GetAll();
            return Ok(new BaseResponseModel<List<ResponseNotiModel>>(
                statusCode: StatusCodes.Status200OK,
                code: ResponseCodeConstants.SUCCESS,
                data: res
            ));
        }
        [HttpGet("{notiId}")]
        public async Task<IActionResult> GetNotiById(Guid notiId)
        {
            var res = await _notificationService.GetById(notiId);
            return Ok(new BaseResponseModel<ResponseNotiModel>(
                statusCode: StatusCodes.Status200OK,
                code: ResponseCodeConstants.SUCCESS,
                data: res
            ));
        }
        //[HttpPatch]
        //public async Task<IActionResult> UpdateShuttle(UpdateShuttleModel model)
        //{
        //    await _shuttleService.UpdateShuttle(model);
        //    return Ok(new BaseResponseModel<string>(
        //        statusCode: StatusCodes.Status200OK,
        //        code: ResponseCodeConstants.SUCCESS,
        //        data: "Cập nhật xe thành công"
        //    ));
        //}
        [HttpDelete]
        public async Task<IActionResult> DeleteNotification(Guid notiId)
        {
            await _notificationService.DeleteNoti(notiId);
            return Ok(new BaseResponseModel<string>(
                statusCode: StatusCodes.Status200OK,
                code: ResponseCodeConstants.SUCCESS,
                data: "Xóa thông báo thành công"
            ));
        }
    }
}
