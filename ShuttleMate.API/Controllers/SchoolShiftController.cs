using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ShuttleMate.Contract.Services.Interfaces;
using ShuttleMate.Core.Bases;
using ShuttleMate.Core.Constants;
using ShuttleMate.ModelViews.RoleModelViews;
using ShuttleMate.ModelViews.SchoolModelView;
using ShuttleMate.ModelViews.SchoolShiftModelViews;
using ShuttleMate.ModelViews.TicketTypeModelViews;
using ShuttleMate.Services.Services;

namespace ShuttleMate.API.Controllers
{
    [Route("api/school-shift")]
    [ApiController]
    public class SchoolShiftController : ControllerBase
    {
        private readonly ISchoolShiftService _schoolShiftService;
        public SchoolShiftController(ISchoolShiftService schoolShiftServicee)
        {
            _schoolShiftService = schoolShiftServicee;
        }
        /// <summary>
        /// lấy tất cả ca học từ id của vé học sinh/phụ huynh đặt(PARENT/STUDENT)
        /// </summary>
        /// <param name="ticketId">id của vé học sinh/phụ huynh đặt.</param>

        [HttpGet("list-school-shift-by-ticketid")]
        public async Task<IActionResult> GetSchoolShiftListByTicketId(Guid ticketId)
        {
            var res = await _schoolShiftService.GetSchoolShiftListByTicketId(ticketId);
            return Ok(new BaseResponseModel<List<ResponseSchoolShiftListByTicketIdMode>>(
                statusCode: StatusCodes.Status200OK,
                code: ResponseCodeConstants.SUCCESS,
                data: res
            ));
        }
        /// <summary>
        /// lấy tất cả ca học trường mình quản lí.
        /// </summary>
        /// <param name="sessionType">lọc theo START, END.</param>
        /// <param name="shiftType">Lọc theo MORNING, AFTERNOON.</param>
        /// <param name="sortAsc">Sắp xếp tăng dần theo ngày tạo (true) hoặc giảm dần (false, mặc định).</param>
        /// <param name="page">Trang (mặc định 0).</param>
        /// <param name="pageSize">Số bản ghi mỗi trang (mặc định 10).</param>
        [HttpGet]
        public async Task<IActionResult> GetAllSchoolShift(int page = 0, int pageSize = 10, string? sessionType = null, string? shiftType = null, bool sortAsc = false)
        {
            return Ok(new BaseResponseModel<BasePaginatedList<ResponseSchoolShiftListByTicketIdMode>>(
            statusCode: StatusCodes.Status200OK,
                code: ResponseCodeConstants.SUCCESS,
                data: await _schoolShiftService.GetAllSchoolShift(page, pageSize, sessionType,shiftType, sortAsc)));
        }
        /// <summary>
        /// Tạo ca học
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> CreateSchoolShift(CreateSchoolShiftModel model)
        {
            await _schoolShiftService.CreateSchoolShift(model);

            return Ok(new BaseResponseModel<string>(
                statusCode: StatusCodes.Status200OK,
                code: ResponseCodeConstants.SUCCESS,
                message: "Tạo ca học thành công!"
            ));
        }
        /// <summary>
        /// Cập nhật ca học
        /// </summary>
        [HttpPatch]
        public async Task<IActionResult> UpdateSchoolShift(UpdateSchoolShiftModel model)
        {
            await _schoolShiftService.UpdateSchoolShift(model);

            return Ok(new BaseResponseModel<string>(
                statusCode: StatusCodes.Status200OK,
                code: ResponseCodeConstants.SUCCESS,
                message: "Cập nhật ca học thành công!"
            ));
        }
        /// <summary>
        /// Xóa ca học
        /// </summary>
        [HttpDelete]
        public async Task<IActionResult> DeleteSchoolShift(DeleteSchoolShiftModel model)
        {
            await _schoolShiftService.DeleteSchoolShift(model);

            return Ok(new BaseResponseModel<string>(
                statusCode: StatusCodes.Status200OK,
                code: ResponseCodeConstants.SUCCESS,
                message: "Xóa ca học thành công!"
            ));
        }
    }
}
