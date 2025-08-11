using Microsoft.AspNetCore.Mvc;
using ShuttleMate.Contract.Services.Interfaces;
using ShuttleMate.Core.Bases;
using ShuttleMate.Core.Constants;
using ShuttleMate.ModelViews.NotificationModelViews;
using ShuttleMate.ModelViews.NotiRecipientModelView;

namespace ShuttleMate.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class NotiRecipientController : ControllerBase
    {
        private readonly INotiRecipientService _notiRecipientService;

        public NotiRecipientController(INotiRecipientService notiRecipientService)
        {
            _notiRecipientService = notiRecipientService;
        }

        [HttpPost]
        public async Task<IActionResult> CreateNotiRecipient(NotiRecipientModel model)
        {
            await _notiRecipientService.CreateNotiRecipient(model);
            return Ok(new BaseResponseModel<string>(
                statusCode: StatusCodes.Status200OK,
                code: ResponseCodeConstants.SUCCESS,
                message: "Tạo thông báo người dùng thành công!"
            ));
        }
        [HttpGet]
        public async Task<IActionResult> GetAllNotiRecipient()
        {
            var res = await _notiRecipientService.GetAll();
            return Ok(new BaseResponseModel<List<ResponseNotiRecipientModel>>(
                statusCode: StatusCodes.Status200OK,
                code: ResponseCodeConstants.SUCCESS,
                data: res
            ));
        }
        [HttpGet("{notiRecipientId}")]
        public async Task<IActionResult> GetNotiRecipientById(Guid notiRecipientId)
        {
            var res = await _notiRecipientService.GetById(notiRecipientId);
            return Ok(new BaseResponseModel<ResponseNotiRecipientModel>(
                statusCode: StatusCodes.Status200OK,
                code: ResponseCodeConstants.SUCCESS,
                data: res
            ));
        }
        [HttpPatch]
        public async Task<IActionResult> UpdateStatusNotiRecipient(UpdateNotiRecipientModel model)
        {
            await _notiRecipientService.UpdateStatusNotiRecipient(model);
            return Ok(new BaseResponseModel<string>(
                statusCode: StatusCodes.Status200OK,
                code: ResponseCodeConstants.SUCCESS,
                message: $"Cập nhật trạng thái thành {model.Status} thành công"
            ));
        }
        [HttpDelete("{notiRecipientId}")]
        public async Task<IActionResult> DeleteNotiRecipient([FromRoute]Guid notiRecipientId)
        {
            await _notiRecipientService.DeleteNotiRecipient(notiRecipientId);
            return Ok(new BaseResponseModel<string>(
                statusCode: StatusCodes.Status200OK,
                code: ResponseCodeConstants.SUCCESS,
                message: "Xóa thông báo người dùng thành công"
            ));
        }
    }
}
