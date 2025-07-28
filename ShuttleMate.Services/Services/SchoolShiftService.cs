using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using ShuttleMate.Contract.Repositories.Entities;
using ShuttleMate.Contract.Repositories.IUOW;
using ShuttleMate.Contract.Services.Interfaces;
using ShuttleMate.Core.Bases;
using ShuttleMate.Core.Constants;
using ShuttleMate.ModelViews.RoleModelViews;
using ShuttleMate.ModelViews.SchoolModelView;
using ShuttleMate.ModelViews.SchoolShiftModelViews;
using ShuttleMate.Services.Services.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static ShuttleMate.Contract.Repositories.Enum.GeneralEnum;

namespace ShuttleMate.Services.Services
{
    public class SchoolShiftService : ISchoolShiftService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IConfiguration _configuration;
        private readonly IHttpContextAccessor _contextAccessor;
        private readonly IEmailService _emailService;

        public SchoolShiftService(IUnitOfWork unitOfWork, IMapper mapper, IConfiguration configuration, IHttpContextAccessor contextAccessor, IEmailService emailService)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _configuration = configuration;
            _contextAccessor = contextAccessor;
            _emailService = emailService;
        }
        public async Task<BasePaginatedList<ResponseSchoolShiftListByTicketIdMode>> GetAllSchoolShift(int page = 0, int pageSize = 10, string? sessionType = null, string? shiftType = null, bool sortAsc = false)
        {
            // Lấy userId từ HttpContext
            string userId = Authentication.GetUserIdFromHttpContextAccessor(_contextAccessor);
            Guid.TryParse(userId, out Guid cb);

            var user = await _unitOfWork.GetRepository<User>().Entities.FirstOrDefaultAsync(x => x.Id == cb && !x.DeletedTime.HasValue);
            if (user == null || user.School == null || user.School?.Id == null)
            {
                var emptyList = Enumerable.Empty<ResponseSchoolShiftListByTicketIdMode>();
                var paginatedList = new BasePaginatedList<ResponseSchoolShiftListByTicketIdMode>(
                    emptyList.ToList(), // Danh sách rỗng
                    0,                  // Total count = 0
                    page,
                    pageSize
                );
                return paginatedList;
            }

            var query = _unitOfWork.GetRepository<SchoolShift>()
                .GetQueryable()
                .Include(x => x.School)
                .Where(x =>x.SchoolId == user.School.Id && !x.DeletedTime.HasValue);

            if (!string.IsNullOrWhiteSpace(sessionType))
            {
                var upper = sessionType.Trim().ToUpper();
                query = query.Where(x => x.SessionType.ToString().ToUpper() == upper);
            }
            if (!string.IsNullOrWhiteSpace(shiftType))
            {
                var upper = shiftType.Trim().ToUpper();
                query = query.Where(x => x.ShiftType.ToString().ToUpper() == upper);
            }

            query = sortAsc
                ? query.OrderBy(x => x.CreatedTime)
                : query.OrderByDescending(x => x.CreatedTime);

            var totalCount = await query.CountAsync();

            var pagedItems = await query
                .Skip(page * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var list = await query
                .Select(x => new ResponseSchoolShiftListByTicketIdMode
                {
                    Id = x.Id,
                    SchoolId = x.SchoolId,
                    SchoolName = x.School.Name,
                    SessionType = x.SessionType.ToString().ToUpper(),
                    ShiftType = x.ShiftType.ToString().ToUpper(),
                    Time = x.Time,
                })
                .ToListAsync();

            return new BasePaginatedList<ResponseSchoolShiftListByTicketIdMode>(list, totalCount, page, pageSize);
        }
        public async Task<BasePaginatedList<ResponseSchoolShiftListByTicketIdMode>> GetAllSchoolShiftForAdmin(int page = 0, int pageSize = 10, string? sessionType = null, string? shiftType = null, bool sortAsc = false, Guid? schoolId = null)
        {

            if (schoolId == null)
            {
                var emptyList = Enumerable.Empty<ResponseSchoolShiftListByTicketIdMode>();
                var paginatedList = new BasePaginatedList<ResponseSchoolShiftListByTicketIdMode>(
                    emptyList.ToList(), // Danh sách rỗng
                    0,                  // Total count = 0
                    page,
                    pageSize
                );
                return paginatedList;
            }

            var query = _unitOfWork.GetRepository<SchoolShift>()
                .GetQueryable()
                .Include(x => x.School)
                .Where(x => x.SchoolId == schoolId 
                && x.School.IsActive == true
                && !x.DeletedTime.HasValue);

            if (!string.IsNullOrWhiteSpace(sessionType))
            {
                var upper = sessionType.Trim().ToUpper();
                query = query.Where(x => x.SessionType.ToString().ToUpper() == upper);
            }
            if (!string.IsNullOrWhiteSpace(shiftType))
            {
                var upper = shiftType.Trim().ToUpper();
                query = query.Where(x => x.ShiftType.ToString().ToUpper() == upper);
            }

            query = sortAsc
                ? query.OrderBy(x => x.CreatedTime)
                : query.OrderByDescending(x => x.CreatedTime);

            var totalCount = await query.CountAsync();

            var pagedItems = await query
                .Skip(page * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var list = await query
                .Select(x => new ResponseSchoolShiftListByTicketIdMode
                {
                    Id = x.Id,
                    SchoolId = x.SchoolId,
                    SchoolName = x.School.Name,
                    SessionType = x.SessionType.ToString().ToUpper(),
                    ShiftType = x.ShiftType.ToString().ToUpper(),
                    Time = x.Time,
                })
                .ToListAsync();

            return new BasePaginatedList<ResponseSchoolShiftListByTicketIdMode>(list, totalCount, page, pageSize);
        }

        public async Task<List<ResponseSchoolShiftListByTicketIdMode>> GetSchoolShiftListByTicketId(Guid ticketId)
        {
            var ticket = await _unitOfWork.GetRepository<Ticket>().Entities.FirstOrDefaultAsync(x => x.Id == ticketId
            && x.Route.IsActive == true
            && x.Route.School.IsActive == true
            && !x.DeletedTime.HasValue) ?? throw new ErrorException(StatusCodes.Status404NotFound, ResponseCodeConstants.NOT_FOUND, "Không tìm thấy vé!");
            var schoolShift = await _unitOfWork.GetRepository<SchoolShift>().Entities.Where(x=>x.SchoolId == ticket.Route.SchoolId).ToListAsync() ?? throw new ErrorException(StatusCodes.Status404NotFound, ResponseCodeConstants.NOT_FOUND, "Không tìm thấy trường!");
            var list = schoolShift.Select(x => new ResponseSchoolShiftListByTicketIdMode
            {
                Id = x.Id,
                SchoolId = x.SchoolId,
                SchoolName = x.School.Name,
                SessionType = x.SessionType.ToString().ToUpper(),
                ShiftType = x.ShiftType.ToString().ToUpper(),
                Time = x.Time,
                
            }).ToList();
            return list;
        }

        public async Task CreateSchoolShift(CreateSchoolShiftModel model)
        {
            var school = await _unitOfWork.GetRepository<School>().Entities.FirstOrDefaultAsync(x => x.Id == model.SchoolId 
            && x.IsActive == true
            && !x.DeletedTime.HasValue) ?? throw new ErrorException(StatusCodes.Status404NotFound, ResponseCodeConstants.NOT_FOUND, "Không tìm thấy trường!");

            if (school.SchoolShifts.Count(x => x.ShiftType.ToString().ToUpper() == model.ShiftType && x.SessionType.ToString().ToUpper() == model.SessionType && !x.DeletedTime.HasValue) > 0)
            {
                throw new ErrorException(StatusCodes.Status400BadRequest, ResponseCodeConstants.BADREQUEST, "Loại giờ này đã được tạo!");
            }
            var schoolShift = new SchoolShift
            {
                Id = Guid.NewGuid(),
                SchoolId = school.Id,
                ShiftType = ShiftTypeEnum.START,//mặc định trc
                Time = model.Time,
                SessionType = SessionTypeEnum.MORNING,//mặc định trc
                CreatedTime = DateTime.Now,
                LastUpdatedTime = DateTime.Now,
            };
            switch (model.ShiftType)
            {
                case "START":
                    schoolShift.ShiftType = ShiftTypeEnum.START;
                    break;
                case "END":
                    schoolShift.ShiftType = ShiftTypeEnum.END;
                    break;
                default:
                    throw new ErrorException(StatusCodes.Status400BadRequest, ResponseCodeConstants.BADREQUEST, "Vui lòng chọn đúng loại giờ!");
            }
            switch (model.SessionType)
            {
                case "MORNING":
                    schoolShift.SessionType = SessionTypeEnum.MORNING;
                    break;
                case "AFTERNOON":
                    schoolShift.SessionType = SessionTypeEnum.AFTERNOON;
                    break;
                default:
                    throw new ErrorException(StatusCodes.Status400BadRequest, ResponseCodeConstants.BADREQUEST, "Vui lòng chọn đúng loại ca!");
            }


            await _unitOfWork.GetRepository<SchoolShift>().InsertAsync(schoolShift);
            await _unitOfWork.SaveAsync();

        }
        public async Task UpdateSchoolShift(UpdateSchoolShiftModel model)
        {
            
            var schoolShift = await _unitOfWork.GetRepository<SchoolShift>().Entities.FirstOrDefaultAsync(x => x.Id == model.Id && !x.DeletedTime.HasValue) ?? throw new ErrorException(StatusCodes.Status404NotFound, ResponseCodeConstants.NOT_FOUND, "Không tìm thấy ca học!");
            var school = await _unitOfWork.GetRepository<School>().Entities.FirstOrDefaultAsync(x => x.Id == schoolShift.SchoolId 
            && x.IsActive == true
            && !x.DeletedTime.HasValue) ?? throw new ErrorException(StatusCodes.Status404NotFound, ResponseCodeConstants.NOT_FOUND, "Không tìm thấy trường!");

            if (schoolShift.ShiftType.ToString().ToUpper() == model.ShiftType
                && schoolShift.Time == model.Time
                && schoolShift.SessionType.ToString().ToUpper() == model.SessionType)
            {
                //bỏ qua cập nhật
            }
            else
            //Cập nhật khi chỉ thay đổi thời gian
            if(schoolShift.ShiftType.ToString().ToUpper() == model.ShiftType
                && schoolShift.SessionType.ToString().ToUpper() == model.SessionType)
            {
                schoolShift.Time = model.Time;
                schoolShift.LastUpdatedTime = DateTime.Now;

                await _unitOfWork.GetRepository<SchoolShift>().UpdateAsync(schoolShift);
                await _unitOfWork.SaveAsync();
            }
            else//tiếp tục cập nhật
            {
                if (school.SchoolShifts.Count(x => x.ShiftType.ToString().ToUpper() == model.ShiftType && x.SessionType.ToString().ToUpper() == model.SessionType && !x.DeletedTime.HasValue) > 0)
                {
                    throw new ErrorException(StatusCodes.Status400BadRequest, ResponseCodeConstants.BADREQUEST, "Loại giờ này đã được tạo!");
                }
                switch (model.ShiftType)
                {
                    case "START":
                        schoolShift.ShiftType = ShiftTypeEnum.START;
                        break;
                    case "END":
                        schoolShift.ShiftType = ShiftTypeEnum.END;
                        break;
                    default:
                        throw new ErrorException(StatusCodes.Status400BadRequest, ResponseCodeConstants.BADREQUEST, "Vui lòng chọn đúng loại giờ!");
                }
                switch (model.SessionType)
                {
                    case "MORNING":
                        schoolShift.SessionType = SessionTypeEnum.MORNING;
                        break;
                    case "AFTERNOON":
                        schoolShift.SessionType = SessionTypeEnum.AFTERNOON;
                        break;
                    default:
                        throw new ErrorException(StatusCodes.Status400BadRequest, ResponseCodeConstants.BADREQUEST, "Vui lòng chọn đúng loại ca!");
                }

                schoolShift.Time = model.Time;
                schoolShift.LastUpdatedTime = DateTime.Now;

                await _unitOfWork.GetRepository<SchoolShift>().UpdateAsync(schoolShift);
                await _unitOfWork.SaveAsync();
            }
        }
        public async Task DeleteSchoolShift(DeleteSchoolShiftModel model)
        {
            var schoolShift = await _unitOfWork.GetRepository<SchoolShift>().Entities.FirstOrDefaultAsync(x => x.Id == model.Id && !x.DeletedTime.HasValue) ?? throw new ErrorException(StatusCodes.Status404NotFound, ResponseCodeConstants.NOT_FOUND, "Không tìm thấy ca học!");

            schoolShift.DeletedTime = DateTime.Now;

            await _unitOfWork.GetRepository<SchoolShift>().UpdateAsync(schoolShift);
            await _unitOfWork.SaveAsync();
        }


    }
}

