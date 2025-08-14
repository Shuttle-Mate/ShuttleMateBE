using Microsoft.AspNetCore.Mvc;
using ShuttleMate.Contract.Services.Interfaces;
using ShuttleMate.Core.Bases;
using ShuttleMate.Core.Constants;
using ShuttleMate.ModelViews.NotiTemplateModelView;
using ShuttleMate.ModelViews.ShuttleModelViews;
using ShuttleMate.Services.Services;

namespace ShuttleMate.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class NotificationTemplateController : ControllerBase
    {
        private readonly INotificationTemplateService _notificationTemplateService;
        public NotificationTemplateController(INotificationTemplateService notificationTemplateService)
        {
            _notificationTemplateService = notificationTemplateService;
        }

        [HttpPost]
        public async Task<IActionResult> CreateNotificationTemplate(NotiTemplateModel model)
        {
            await _notificationTemplateService.CreateNotiTemplate(model);
            return Ok(new BaseResponseModel<string>(
                statusCode: StatusCodes.Status200OK,
                code: ResponseCodeConstants.SUCCESS,
                message: "Tạo mẫu thông báo thành công!"
            ));
        }
   
        [HttpGet]
        public async Task<IActionResult> GetAllNotiTemplate([FromQuery] GetNotiTemplateQuery req)
        {
            var res = await _notificationTemplateService.GetAll(req);
            return Ok(new BaseResponseModel<BasePaginatedList<ResponseNotiTemplateModel>>(
                statusCode: StatusCodes.Status200OK,
                code: ResponseCodeConstants.SUCCESS,
                data: res
            ));
        }
        [HttpGet("{notiTemplateId}")]
        public async Task<IActionResult> GetNotiTemplateById(Guid notiTemplateId)
        {
            var res = await _notificationTemplateService.GetById(notiTemplateId);
            return Ok(new BaseResponseModel<ResponseNotiTemplateModel>(
                statusCode: StatusCodes.Status200OK,
                code: ResponseCodeConstants.SUCCESS,
                data: res
            ));
        }
        [HttpPatch]
        public async Task<IActionResult> UpdateNotiTemplate(UpdateNotiTemplateModel model)
        {
            await _notificationTemplateService.UpdateNotiTemplate(model);
            return Ok(new BaseResponseModel<string>(
                statusCode: StatusCodes.Status200OK,
                code: ResponseCodeConstants.SUCCESS,
                message: "Cập nhật mẫu thông báo thành công!"
            ));
        }
        [HttpDelete("{notiTemplateId}")]
        public async Task<IActionResult> DeleteNotiTemplate([FromRoute] Guid notiTemplateId)
        {
            await _notificationTemplateService.DeleteNotiTemplate(notiTemplateId);
            return Ok(new BaseResponseModel<string>(
                statusCode: StatusCodes.Status200OK,
                code: ResponseCodeConstants.SUCCESS,
                message: "Xóa mẫu thông báo thành công"
            ));
        }
    }
}
