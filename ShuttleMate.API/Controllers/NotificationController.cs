using Microsoft.AspNetCore.Mvc;
using ShuttleMate.Contract.Services.Interfaces;
using ShuttleMate.Core.Bases;
using ShuttleMate.Core.Constants;
using ShuttleMate.ModelViews.NotificationModelViews;
using ShuttleMate.ModelViews.ShuttleModelViews;
using ShuttleMate.Services.Services;
using static ShuttleMate.Contract.Repositories.Enum.GeneralEnum;

namespace ShuttleMate.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class NotificationController : ControllerBase
    {
        private readonly INotificationService _notificationService;
        private readonly IFirebaseService _firebaseService;

        public NotificationController(INotificationService notificationService, IFirebaseService firebaseService)
        {
            _notificationService = notificationService;
            _firebaseService = firebaseService;
        }

        /// <summary>
        /// Gửi thông báo đến toàn người dùng không sử dụng template
        /// </summary>
        [HttpPost("send-to-all")]
        public async Task<IActionResult> SendToAll([FromBody] NotiModel model)
        {
            var notificationId = await _notificationService.CreateNotificationForAllUsers(model);
            return Ok(new BaseResponseModel<Guid>(
                statusCode: StatusCodes.Status200OK,
                code: ResponseCodeConstants.SUCCESS,
                message: "Gửi thông báo đến tất cả người dùng thành công",
                data: notificationId
            ));
        }

        /// <summary>
        /// Gửi thông báo đến toàn người dùng sử dụng template
        /// </summary>
        [HttpPost("send-template-to-all")]
        public async Task<IActionResult> SendTemplateToAll([FromBody] NotificationTemplateSendAllRequest req)
        {
            var notificationId = await _notificationService.SendNotificationForAllFromTemplateAsync(
                templateType: req.TemplateType,
                metadata: req.Metadata,
                notiCategory: req.NotificationCategory
            );
            return Ok(new BaseResponseModel<Guid>(
                statusCode: StatusCodes.Status200OK,
                code: ResponseCodeConstants.SUCCESS,
                message: $"Gửi thông báo bằng template {req.TemplateType} đến tất cả người dùng thành công",
                data: notificationId
            ));
        }

        /// <summary>
        /// API này để test device token
        /// </summary>
        [HttpPost("push/send")]
        public async Task<IActionResult> Send([FromBody] NotificationRequest req)
        {
            await _firebaseService.SendNotificationAsync(req.Title, req.Body, req.DeviceToken);
            return Ok("Notification sent.");
        }

        //[HttpPost]
        //public async Task<IActionResult> CreateNoti(NotiModel model)
        //{
        //    await _notificationService.CreateNotification(model);
        //    return Ok(new BaseResponseModel<string>(
        //        statusCode: StatusCodes.Status200OK,
        //        code: ResponseCodeConstants.SUCCESS,
        //        message: "Tạo thông báo thành công!"
        //    ));
        //}
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
        [HttpPatch("{notiRecipientId}")]
        public async Task<IActionResult> HandleNotiStatus([FromRoute]Guid notiRecipientId, [FromQuery] NotificationStatusEnum status)
        {
            await _notificationService.HandleNotiStatus(notiRecipientId, status);
            return Ok(new BaseResponseModel<string>(
                statusCode: StatusCodes.Status200OK,
                code: ResponseCodeConstants.SUCCESS,
                data: "Cập nhật thông báo thành công!"
            ));
        }
        [HttpDelete("{notiId}")]
        public async Task<IActionResult> DeleteNotification([FromRoute]Guid notiId)
        {
            await _notificationService.DeleteNoti(notiId);
            return Ok(new BaseResponseModel<string>(
                statusCode: StatusCodes.Status200OK,
                code: ResponseCodeConstants.SUCCESS,
                message: "Xóa thông báo thành công"
            ));
        }
        /// <summary>
        /// Gửi thông báo đến list người dùng cụ thể sử dụng template
        /// </summary>
        [HttpPost("send-template")]
        public async Task<IActionResult> SendFromTemplate([FromBody] NotificationTemplateSendRequest request)
        {
            var notificationId = await _notificationService.SendNotificationFromTemplateAsync(
                templateType: request.TemplateType,
                recipientIds: request.RecipientIds,
                metadata: request.Metadata,
                createdBy: request.CreatedBy,
                notiCategory: request.NotificationCategory
            );

            return Ok(new BaseResponseModel<Guid>(
                statusCode: StatusCodes.Status200OK,
                code: ResponseCodeConstants.SUCCESS,
                message: "Gửi thông báo từ template thành công",
                data: notificationId
            ));
        }
    }
}
