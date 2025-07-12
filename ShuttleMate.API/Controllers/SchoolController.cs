using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ShuttleMate.Contract.Services.Interfaces;
using ShuttleMate.Core.Bases;
using ShuttleMate.Core.Constants;
using ShuttleMate.ModelViews.SchoolModelView;
using ShuttleMate.ModelViews.StopModelViews;
using ShuttleMate.Services.Services;

namespace ShuttleMate.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SchoolController : ControllerBase
    {
        private readonly ISchoolService _schoolService;
        public SchoolController(ISchoolService schoolService)
        {
            _schoolService = schoolService;
        }
        [HttpPost]
        public async Task<IActionResult> CreateSchool(CreateSchoolModel model)
        {
            await _schoolService.CreateSchool(model);
            return Ok(new BaseResponseModel<string>(
                statusCode: StatusCodes.Status200OK,
                code: ResponseCodeConstants.SUCCESS,
                message: "Tạo trường thành công!"
            ));
        }
        [HttpGet]
        public async Task<IActionResult> GetAllSchools()
        {
            var res = await _schoolService.GetAllAsync();
            return Ok(new BaseResponseModel<IEnumerable<SchoolResponseModel>>(
                statusCode: StatusCodes.Status200OK,
                code: ResponseCodeConstants.SUCCESS,
                data: res
            ));
        }
        [HttpGet("{schoolId}")]
        public async Task<IActionResult> GetSchoolById(Guid schoolId)
        {
            var res = await _schoolService.GetById(schoolId);
            return Ok(new BaseResponseModel<SchoolResponseModel>(
                statusCode: StatusCodes.Status200OK,
                code: ResponseCodeConstants.SUCCESS,
                data: res
            ));
        }
        [HttpPatch]
        public async Task<IActionResult> UpdateSchool(UpdateSchoolModel model)
        {
            await _schoolService.UpdateSchool(model);
            return Ok(new BaseResponseModel<string>(
                statusCode: StatusCodes.Status200OK,
                code: ResponseCodeConstants.SUCCESS,
                message: "Cập nhật trường thành công"
            ));
        }
        [HttpDelete]
        public async Task<IActionResult> DeleteSchool(DeleteSchoolModel model)
        {
            await _schoolService.DeleteSchool(model);
            return Ok(new BaseResponseModel<string>(
                statusCode: StatusCodes.Status200OK,
                code: ResponseCodeConstants.SUCCESS,
                message: "Xóa trường thành công"
            ));
        }
    }
}
