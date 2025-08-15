using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
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
        private readonly ILogger<SchoolShiftService> _logger;

        public SchoolShiftService(IUnitOfWork unitOfWork, IMapper mapper, IConfiguration configuration, IHttpContextAccessor contextAccessor, IEmailService emailService, ILogger<SchoolShiftService> logger)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _configuration = configuration;
            _contextAccessor = contextAccessor;
            _emailService = emailService;
            _logger = logger;
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
                .Select(x => new ResponseSchoolShiftListByTicketIdMode
                {
                    Id = x.Id,
                    SchoolId = x.SchoolId,
                    SchoolName = x.School.Name,
                    SessionType = x.SessionType.ToString().ToUpper(),
                    ShiftType = x.ShiftType.ToString().ToUpper(),
                    Time = x.Time,
                })
                .Skip(page * pageSize)
                .Take(pageSize)
                .ToListAsync();
            return new BasePaginatedList<ResponseSchoolShiftListByTicketIdMode>(pagedItems, totalCount, page, pageSize);
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
                .Select(x => new ResponseSchoolShiftListByTicketIdMode
                {
                    Id = x.Id,
                    SchoolId = x.SchoolId,
                    SchoolName = x.School.Name,
                    SessionType = x.SessionType.ToString().ToUpper(),
                    ShiftType = x.ShiftType.ToString().ToUpper(),
                    Time = x.Time,
                })
                .Skip(page * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new BasePaginatedList<ResponseSchoolShiftListByTicketIdMode>(pagedItems, totalCount, page, pageSize);
        }

        public async Task<List<ResponseSchoolShiftListByTicketIdMode>> GetSchoolShiftListByTicketId(Guid ticketId)
        {
            var ticket = await _unitOfWork.GetRepository<Ticket>().Entities.FirstOrDefaultAsync(x => x.Id == ticketId
            && x.Route.IsActive == true
            && x.Route.School.IsActive == true
            && !x.DeletedTime.HasValue) ?? throw new ErrorException(StatusCodes.Status404NotFound, ResponseCodeConstants.NOT_FOUND, "Không tìm thấy vé!");
            var schoolShift = await _unitOfWork.GetRepository<SchoolShift>().Entities.Where(x=>x.SchoolId == ticket.Route.SchoolId && !x.DeletedTime.HasValue).ToListAsync() ?? throw new ErrorException(StatusCodes.Status404NotFound, ResponseCodeConstants.NOT_FOUND, "Không tìm thấy trường!");
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
        public async Task UpdateSchoolShift(Guid id, UpdateSchoolShiftModel model)
        {
            
            var schoolShift = await _unitOfWork.GetRepository<SchoolShift>().Entities.FirstOrDefaultAsync(x => x.Id == id && !x.DeletedTime.HasValue) ?? throw new ErrorException(StatusCodes.Status404NotFound, ResponseCodeConstants.NOT_FOUND, "Không tìm thấy ca học!");
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
        public async Task DeleteSchoolShift(Guid id)
        {
            var vnTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Ho_Chi_Minh");
            var vietnamNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, vnTimeZone);
            var todayVN = DateOnly.FromDateTime(vietnamNow);
            // Get the school shift with validation
            var schoolShift = await _unitOfWork.GetRepository<SchoolShift>().Entities
                .Include(x => x.School)
                .FirstOrDefaultAsync(x => x.Id == id && !x.DeletedTime.HasValue)
                ?? throw new ErrorException(StatusCodes.Status404NotFound,
                    ResponseCodeConstants.NOT_FOUND,
                    "Không tìm thấy ca học!");

            // Mark as deleted
            schoolShift.DeletedTime = DateTime.Now;
            await _unitOfWork.GetRepository<SchoolShift>().UpdateAsync(schoolShift);

            // Get all related user school shifts with student info
            var userSchoolShifts = await _unitOfWork.GetRepository<UserSchoolShift>().Entities
                .Where(x => x.SchoolShiftId == id && !x.DeletedTime.HasValue)
                .Include(x => x.Student)
                .ToListAsync();

            if (userSchoolShifts.Any())
            {
                // Get distinct students
                var distinctStudents = userSchoolShifts
                    .Select(x => x.Student)
                    .DistinctBy(x => x.Id)
                    .ToList();

                // Soft delete all user school shift records
                foreach (var userShift in userSchoolShifts)
                {
                    await _unitOfWork.GetRepository<UserSchoolShift>().DeleteAsync(userShift);
                }

                // Prepare email content
                var schoolName = schoolShift.School?.Name ?? "trường học";
                var shiftTime = $"{schoolShift.Time:HH:mm}";

                // Send email to each affected student
                foreach (var student in distinctStudents)
                {
                    try
                    {
                        await _emailService.SendEmailAsync(
                            student.Email,
                            $"THÔNG BÁO HỦY CA HỌC - {schoolName}",
                            $@"
                    <div style='font-family: Arial, sans-serif; max-width: 600px; margin: auto; padding: 20px; background-color: #FAF9F7;'>
                        <h2 style='color: #124DA3; border-bottom: 2px solid #F37022; padding-bottom: 10px;'>THÔNG BÁO HỦY CA HỌC</h2>
                        
                        <p style='color: #333;'>Xin chào {student.FullName},</p>
                        
                        <p style='color: #333;'>Trường đã hủy ca học của bạn với thông tin sau:</p>
                        
                        <div style='background: white; padding: 15px; margin: 15px 0; border-radius: 4px; border-left: 4px solid #124DA3;'>
                            <p style='margin: 5px 0 0; color: #333;'><strong>Ca học:</strong> {shiftTime}</p>
                            <p style='margin: 5px 0 0; color: #F37022;'><strong>Trạng thái:</strong> ĐÃ HỦY</p>
                        </div>
                        
                        <p style='color: #333;'>Vui lòng đăng nhập vào hệ thống để đăng ký lại ca học mới.</p>
                        
                        <a href='#' style='display: inline-block; background-color: #F37022; color: white; 
                                          padding: 10px 20px; text-decoration: none; border-radius: 4px;
                                          margin: 15px 0; font-weight: bold;'>
                            Truy cập hệ thống ngay
                        </a>
                        
                        <p style='color: #ff0000;'><strong>⚠️ Lưu ý:</strong> Bạn cần đăng ký lại ca học mới trước khi hết hạn.</p>
                        
                        <p style='color: #4EB748; font-style: italic;'>
                            <strong>✔️ Thông báo:</strong> Hệ thống đã ghi nhận thay đổi!
                        </p>
                        
                        <p style='color: #333; font-size: 14px;'>
                            Nếu bạn không nhận ra thay đổi này, vui lòng liên hệ bộ phận hỗ trợ.
                        </p>
                        
                        <div style='margin-top: 30px; padding-top: 15px; border-top: 1px solid #eee;'>
                            <p style='color: #124DA3; font-weight: bold;'>Đội ngũ hỗ trợ ShuttleMate</p>
                            <p style='font-size: 12px; color: #999;'>
                                © {vietnamNow.Year} ShuttleMate. Bảo lưu mọi quyền.
                            </p>
                        </div>
                    </div>
                    "
                        );
                    }
                    catch (Exception ex)
                    {
                        // Log email failure but continue processing
                        _logger.LogError(ex, $"Failed to send email to student {student.Id} about deleted shift");
                    }
                }
            }

            await _unitOfWork.SaveAsync();
        }
    }
}

