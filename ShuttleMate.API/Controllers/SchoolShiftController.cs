using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ShuttleMate.Contract.Services.Interfaces;
using ShuttleMate.Core.Bases;
using ShuttleMate.Core.Constants;
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
