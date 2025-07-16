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

        /// <summary>
        /// Lấy danh sách trường học.
        /// </summary>
        /// <param name="search">Tìm kiếm theo tên, địa chỉ, email hoặc sđt của trường.</param>
        /// <param name="isActive">lọc theo trường có kích hoạt hay không(True:có, false:ko)(tùy chọn).</param>
        /// <param name="sortAsc">Sắp xếp tăng dần theo ngày tạo (true) hoặc giảm dần (false, mặc định).</param>
        /// <param name="page">Trang (mặc định 0).</param>
        /// <param name="pageSize">Số bản ghi mỗi trang (mặc định 10).</param>
        [HttpGet]
        public async Task<IActionResult> GetAllAsync(int page = 0, int pageSize = 10, string? search = null, bool? isActive = null, bool sortAsc = false)
        {
            return Ok(new BaseResponseModel<BasePaginatedList<SchoolResponseModel>>(
            statusCode: StatusCodes.Status200OK,
                code: ResponseCodeConstants.SUCCESS,
                data: await _schoolService.GetAllAsync(page, pageSize, search, isActive, sortAsc)));
        } 
        /// <summary>
        /// Lấy danh sách học sinh trong trường bạn quản lí.
        /// </summary>
        /// <param name="search">Tìm kiếm theo tên, địa chỉ, email hoặc sđt của trường.</param>
        /// <param name="sortAsc">Sắp xếp tăng dần theo ngày tạo (true) hoặc giảm dần (false, mặc định).</param>
        /// <param name="page">Trang (mặc định 0).</param>
        /// <param name="pageSize">Số bản ghi mỗi trang (mặc định 10).</param>
        [HttpGet("list-student")]
        public async Task<IActionResult> GetAllStudentInSchool(int page = 0, int pageSize = 10, string? search = null, bool sortAsc = false)
        {
            return Ok(new BaseResponseModel<BasePaginatedList<ListStudentInSchoolResponse>>(
            statusCode: StatusCodes.Status200OK,
                code: ResponseCodeConstants.SUCCESS,
                data: await _schoolService.GetAllStudentInSchool(page, pageSize, search, sortAsc)));
        }
        /// <summary>
        /// Lấy chi tiết trường.
        /// </summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetSchoolById (Guid id)
        {
            return Ok(new BaseResponseModel<SchoolResponseModel>(
                statusCode: StatusCodes.Status200OK,
                code: ResponseCodeConstants.SUCCESS,
                data: await _schoolService.GetById(id)));
        }
        ///// <summary>
        ///// Tạo trường mới.
        ///// </summary>
        //[HttpPost]
        //public async Task<IActionResult> CreateSchool(CreateSchoolModel model)
        //{
        //    await _schoolService.CreateSchool(model);
        //    return Ok(new BaseResponseModel<string>(
        //        statusCode: StatusCodes.Status200OK,
        //        code: ResponseCodeConstants.SUCCESS,
        //        message: "Tạo trường thành công!"
        //    ));
        //}
        /// <summary>
        /// Cập nhật một trường.
        /// </summary>
        [HttpPatch("id")]
        public async Task<IActionResult> UpdateSchool(UpdateSchoolModel model)
        {
            await _schoolService.UpdateSchool(model);
            return Ok(new BaseResponseModel<string>(
                statusCode: StatusCodes.Status200OK,
                code: ResponseCodeConstants.SUCCESS,
                message: "Cập nhật trường thành công!"
            ));
        }
        /// <summary>
        /// Gán quản lí cho trường.
        /// </summary>
        [HttpPatch("assign-school")]
        public async Task<IActionResult> AssignSchoolForManager(AssignSchoolForManagerModel model)
        {
            await _schoolService.AssignSchoolForManager(model);
            return Ok(new BaseResponseModel<string>(
                statusCode: StatusCodes.Status200OK,
                code: ResponseCodeConstants.SUCCESS,
                message: "Gán trường với quản lí trường thành công!"
            ));
        }

        /// <summary>
        /// Xóa một trường.
        /// </summary>
        [HttpDelete]
        public async Task<IActionResult> DeleteSchool(DeleteSchoolModel model)
        {
            await _schoolService.DeleteSchool(model);
            return Ok(new BaseResponseModel<string>(
                statusCode: StatusCodes.Status200OK,
                code: ResponseCodeConstants.SUCCESS,
                message: "Xóa trường thành công!"
            ));
        }
    }
}
