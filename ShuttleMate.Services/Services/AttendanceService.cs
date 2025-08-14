using AutoMapper;
using Google.Apis.Auth.OAuth2;
using Google.Cloud.Firestore;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Client.Platforms.Features.DesktopOs.Kerberos;
using Org.BouncyCastle.Ocsp;
using ShuttleMate.Contract.Repositories.Entities;
using ShuttleMate.Contract.Repositories.IUOW;
using ShuttleMate.Contract.Services.Interfaces;
using ShuttleMate.Core.Bases;
using ShuttleMate.Core.Constants;
using ShuttleMate.Core.Utils;
using ShuttleMate.ModelViews.AttendanceModelViews;
using ShuttleMate.ModelViews.ShuttleModelViews;
using ShuttleMate.ModelViews.UserModelViews;
using ShuttleMate.Services.Services.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;
using static ShuttleMate.Contract.Repositories.Enum.GeneralEnum;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace ShuttleMate.Services.Services
{
    public class AttendanceService : IAttendanceService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IHttpContextAccessor _contextAccessor;
        private readonly IUserService _userService;
        private readonly INotificationService _notificationService;
        private readonly FirestoreService _firestoreService;

        public AttendanceService(IUnitOfWork unitOfWork, IMapper mapper, IHttpContextAccessor contextAccessor, IUserService userService, INotificationService notificationService, FirestoreService firestoreService)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _contextAccessor = contextAccessor;
            _userService = userService;
            _notificationService = notificationService;
            _firestoreService = firestoreService;
        }

        public async Task CheckIn(CheckInModel model)
        {
            string userId = Authentication.GetUserIdFromHttpContextAccessor(_contextAccessor);

            Attendance attendance = await _unitOfWork.GetRepository<Attendance>()
                .Entities
                .FirstOrDefaultAsync(x => x.Status == AttendanceStatusEnum.CHECKED_IN && x.HistoryTicketId == model.HistoryTicketId);

            if (attendance != null)
            {
                throw new ErrorException(StatusCodes.Status400BadRequest, ErrorCode.BadRequest, "Vé này đã CheckIn nhưng chưa được CheckOut!!");
            }

            var checkin = _mapper.Map<Attendance>(model);
            checkin.CheckInTime = CoreHelper.SystemTimeNow.DateTime;
            checkin.Status = AttendanceStatusEnum.CHECKED_IN;
            checkin.CreatedBy = userId;
            checkin.LastUpdatedBy = userId;
            //checkin.CheckOutTime = null;
            await _unitOfWork.GetRepository<Attendance>().InsertAsync(checkin);
            await _unitOfWork.SaveAsync();

            var checkinWithNav = await _unitOfWork.GetRepository<Attendance>().Entities
                .Include(a => a.HistoryTicket)
                    .ThenInclude(ht => ht.User)
                .Include(a => a.Trip)
                    .ThenInclude(t => t.Schedule)
                        .ThenInclude(s => s.Shuttle)
                .Include(a => a.StopCheckInLocation)
                .FirstOrDefaultAsync(a => a.Id == checkin.Id);

            if (checkinWithNav == null)
                throw new Exception("Không tìm thấy attendance sau khi insert.");

            // ==== Bổ sung Firestore Realtime ====
            var docRef = _firestoreService.GetCollection("attendance").Document(checkin.HistoryTicketId.ToString());
            await docRef.SetAsync(new
            {
                HistoryTicketId = checkin.HistoryTicketId.ToString(),
                StudentName = checkinWithNav.HistoryTicket.User.FullName,
                ShuttleName = checkinWithNav.Trip.Schedule.Shuttle.Name,
                CheckInLocation = checkinWithNav.StopCheckInLocation.Name,
                CheckInTime = CoreHelper.SystemTimeNow.DateTime,
                Status = "CHECKED_IN"
            });

            DateTime dateTime = CoreHelper.SystemTimeNow.DateTime;

            var metadata = new Dictionary<string, string>
                {
                    { "StudentName", checkinWithNav.HistoryTicket.User.FullName },
                    { "ShuttleName", checkinWithNav.Trip.Schedule.Shuttle.Name },
                    { "CheckInLocation", checkinWithNav.StopCheckInLocation.Name },
                    { "CheckInTime", TimeOnly.FromDateTime(dateTime).ToString()}
                };

            // Gửi cho học sinh
            await _notificationService.SendNotificationFromTemplateAsync(
                templateType: "CheckIn",
                recipientIds: new List<Guid> { checkin.HistoryTicket.UserId },
                metadata: metadata,
                createdBy: "system",
                notiCategory: "ATTENDANCE"
            );

            // Nếu có phụ huynh thì gửi cho phụ huynh
            if (checkin.HistoryTicket.User.ParentId != null && checkin.HistoryTicket.User.ParentId != Guid.Empty)
            {
                await _notificationService.SendNotificationFromTemplateAsync(
                    templateType: "CheckIn",
                    recipientIds: new List<Guid> { checkin.HistoryTicket.User.ParentId.Value },
                    metadata: metadata,
                    createdBy: "system",
                    notiCategory: "ATTENDANCE"
                );
            }

        }

        public async Task CheckOut(CheckOutModel model)
        {
            string userId = Authentication.GetUserIdFromHttpContextAccessor(_contextAccessor);

            if (model.CheckOutLocation == null || model.CheckOutLocation == Guid.Empty)
            {
                throw new ErrorException(StatusCodes.Status404NotFound, ErrorCode.NotFound, "Địa điểm checkout không được để trống!");
            }
            var checkout = await _unitOfWork.GetRepository<Attendance>().Entities.FirstOrDefaultAsync(x => x.Id == model.Id && !x.DeletedTime.HasValue) ?? throw new ErrorException(StatusCodes.Status404NotFound, ErrorCode.NotFound, "Không tìm thấy hoặc vé chưa checkin!");

            if (checkout.Status == AttendanceStatusEnum.CHECKED_OUT)
            {
                throw new ErrorException(StatusCodes.Status400BadRequest, ErrorCode.BadRequest, "Vé này đã được CheckOut!!");
            }

            _mapper.Map(model, checkout);
            checkout.Status = AttendanceStatusEnum.CHECKED_OUT;
            checkout.CheckOutTime = CoreHelper.SystemTimeNow.DateTime;
            checkout.LastUpdatedBy = userId;
            checkout.LastUpdatedTime = CoreHelper.SystemTimeNow.DateTime;
            await _unitOfWork.GetRepository<Attendance>().UpdateAsync(checkout);
            await _unitOfWork.SaveAsync();

            var checkoutWithNav = await _unitOfWork.GetRepository<Attendance>().Entities
                .Include(a => a.HistoryTicket)
                    .ThenInclude(ht => ht.User)
                .Include(a => a.Trip)
                    .ThenInclude(t => t.Schedule)
                        .ThenInclude(s => s.Shuttle)
                .Include(a => a.StopCheckOutLocation)
                .FirstOrDefaultAsync(a => a.Id == checkout.Id);

            if (checkoutWithNav == null)
                throw new Exception("Không tìm thấy attendance sau khi insert.");

            // ==== Bổ sung Firestore Realtime ====
            var docRef = _firestoreService.GetCollection("attendance").Document(checkout.HistoryTicketId.ToString());
            await docRef.SetAsync(new
            {
                HistoryTicketId = checkout.HistoryTicketId.ToString(),
                StudentName = checkoutWithNav.HistoryTicket.User.FullName,
                ShuttleName = checkoutWithNav.Trip.Schedule.Shuttle.Name,
                CheckOutLocation = checkoutWithNav.StopCheckOutLocation.Name,
                CheckOutTime = CoreHelper.SystemTimeNow.DateTime,
                Status = "CHECKED_OUT"
            });

            DateTime dateTime = CoreHelper.SystemTimeNow.DateTime;

            var metadata = new Dictionary<string, string>
                {
                    { "StudentName", checkoutWithNav.HistoryTicket.User.FullName },
                    { "ShuttleName", checkoutWithNav.Trip.Schedule.Shuttle.Name },
                    { "CheckOutLocation", checkoutWithNav.StopCheckOutLocation.Name },
                    { "CheckOutTime", TimeOnly.FromDateTime(dateTime).ToString()}
                };

            // Gửi cho học sinh
            await _notificationService.SendNotificationFromTemplateAsync(
                templateType: "CheckOut",
                recipientIds: new List<Guid> { checkout.HistoryTicket.UserId },
                metadata: metadata,
                createdBy: "system",
                notiCategory: "ATTENDANCE"
            );

            // Nếu có phụ huynh thì gửi cho phụ huynh
            if (checkout.HistoryTicket.User.ParentId != null && checkout.HistoryTicket.User.ParentId != Guid.Empty)
            {
                await _notificationService.SendNotificationFromTemplateAsync(
                    templateType: "CheckOut",
                    recipientIds: new List<Guid> { checkout.HistoryTicket.User.ParentId.Value },
                    metadata: metadata,
                    createdBy: "system",
                    notiCategory: "ATTENDANCE"
                );
            }
        }

        public async Task DeleteAttendance(Guid attendanceId)
        {
            string userId = Authentication.GetUserIdFromHttpContextAccessor(_contextAccessor);

            var attendance = await _unitOfWork.GetRepository<Attendance>().Entities.FirstOrDefaultAsync(x => x.Id == attendanceId && !x.DeletedTime.HasValue) ?? throw new ErrorException(StatusCodes.Status404NotFound, ErrorCode.NotFound, "Không tìm thấy thông tin điểm danh!");
            attendance.DeletedTime = CoreHelper.SystemTimeNow.DateTime;
            attendance.DeletedBy = userId;
            await _unitOfWork.GetRepository<Attendance>().UpdateAsync(attendance);
            await _unitOfWork.SaveAsync();
        }

        public async Task<BasePaginatedList<ResponseAttendanceModel>> GetAll(GetAttendanceQuery req)
        {
            var page = req.page > 0 ? req.page : 0;
            var pageSize = req.pageSize > 0 ? req.pageSize : 10;

            var query = _unitOfWork.GetRepository<Attendance>().Entities
                .Include(x => x.HistoryTicket)
                .Where(x => !x.DeletedTime.HasValue);
            //.OrderBy(x => x.Status);

            // Thêm filter theo tripId nếu có truyền vào
            if (req.tripId.HasValue && req.tripId.Value != Guid.Empty)
            {
                query = query.Where(x => x.TripId == req.tripId.Value);
            }

            if (req.userId.HasValue && req.userId.Value != Guid.Empty)
            {
                query = query.Where(x => x.HistoryTicket.UserId == req.userId.Value);
            }

            if (req.fromDate.HasValue)
            {
                query = query.Where(a => a.CheckInTime >= req.fromDate.Value);
            }

            if (req.toDate.HasValue)
            {
                query = query.Where(a => a.CheckInTime <= req.toDate.Value);
            }

            query = query.OrderBy(x => x.Status);

            var totalCount = query.Count();

            //Paging
            var attendances = await query
                .Skip(req.page * req.pageSize)
                .Take(req.pageSize)
                .ToListAsync();

            if (!attendances.Any())
            {
                throw new ErrorException(StatusCodes.Status404NotFound, ErrorCode.NotFound, "Không có lượt điểm danh nào được ghi nhận!");
            }

            var result = _mapper.Map<List<ResponseAttendanceModel>>(attendances);

            return new BasePaginatedList<ResponseAttendanceModel>(result, totalCount, page, pageSize);
        }

        public async Task<ResponseAttendanceModel> GetById(Guid attendanceId)
        {
            var attendance = await _unitOfWork.GetRepository<Attendance>().Entities.FirstOrDefaultAsync(x => x.Id == attendanceId && !x.DeletedTime.HasValue) ?? throw new ErrorException(StatusCodes.Status404NotFound, ErrorCode.NotFound, "Không tìm thấy thông tin điểm danh!");

            return _mapper.Map<ResponseAttendanceModel>(attendance);
        }

        public async Task<List<ResponseAttendanceModel>> GetMyAttendance(DateTime? fromDate, DateTime? toDate)
        {
            string userId = Authentication.GetUserIdFromHttpContextAccessor(_contextAccessor);

            //Guid.TryParse(userId, out Guid cb);

            if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out Guid ui))
            {
                throw new ErrorException(StatusCodes.Status401Unauthorized, ErrorCode.Unauthorized, "Người dùng chưa đăng nhập hoặc không hợp lệ");
            }

            var attendanceQuery = _unitOfWork.GetRepository<Attendance>().Entities
                .Include(a => a.HistoryTicket) // Include HistoryTickets
                .Where(a => !a.DeletedTime.HasValue && a.HistoryTicket.UserId.ToString() == userId);

            if (fromDate.HasValue)
            {
                attendanceQuery = attendanceQuery.Where(a => a.CheckInTime >= fromDate.Value);
            }

            if (toDate.HasValue)
            {
                attendanceQuery = attendanceQuery.Where(a => a.CheckInTime <= toDate.Value);
            }

            var attendances = await attendanceQuery
                .OrderByDescending(a => a.CheckInTime)
                .ToListAsync();

            if (!attendances.Any())
            {
                throw new ErrorException(StatusCodes.Status404NotFound, ErrorCode.NotFound, "Không tìm thấy dữ liệu điểm danh nào.");
            }

            return _mapper.Map<List<ResponseAttendanceModel>>(attendances);
        }

        public async Task BulkCheckOutByTrip(Guid tripId, Guid checkOutLocation, string? notes = null)
        {
            string userId = Authentication.GetUserIdFromHttpContextAccessor(_contextAccessor);

            // lấy tất cả attendance của trip chưa checkout và chưa bị xóa
            var attendances = await _unitOfWork.GetRepository<Attendance>().Entities
                .Include(a => a.Trip)
                    .ThenInclude(a => a.Schedule)
                        .ThenInclude(a => a.Shuttle)
                .Include(a => a.HistoryTicket)
                    .ThenInclude(a => a.User)
                .Where(x => x.TripId == tripId
                    && x.Status == AttendanceStatusEnum.CHECKED_IN
                    && !x.DeletedTime.HasValue)
                .ToListAsync();

            if (!attendances.Any())
            {
                throw new ErrorException(StatusCodes.Status404NotFound, ErrorCode.NotFound, "Không có học sinh nào cần checkout trên chuyến này!");
            }

            foreach (var attendance in attendances)
            {
                attendance.Status = AttendanceStatusEnum.CHECKED_OUT;
                attendance.CheckOutTime = CoreHelper.SystemTimeNow.DateTime;
                attendance.CheckOutLocation = checkOutLocation;
                attendance.Notes = notes;
                attendance.LastUpdatedBy = userId;
                attendance.LastUpdatedTime = CoreHelper.SystemTimeNow.DateTime;
            }

            await _unitOfWork.GetRepository<Attendance>().UpdateRangeAsync(attendances);
            await _unitOfWork.SaveAsync();

            var userIds = attendances.Select(x => x.HistoryTicket.User.Id).ToList();

            var trip = await _unitOfWork.GetRepository<Trip>()
                .Entities
                .Include(a => a.Schedule)
                    .ThenInclude(a => a.Shuttle)
                .FirstOrDefaultAsync(a => a.Id == tripId);

            var users = await _unitOfWork.GetRepository<User>()
                .Entities
                .Where(u => userIds.Contains(u.Id))
                .Select(u => new { u.Id, u.FullName, u.ParentId })
                .ToListAsync();

            var createdBy = "system";

            foreach (var user in users)
            {
                DateTime dateTime = CoreHelper.SystemTimeNow.DateTime;

                var metadata = new Dictionary<string, string>
                {
                    { "StudentName", user.FullName },
                    { "ShuttleName", trip.Schedule.Shuttle.Name },
                    { "CheckOutLocation", attendances.FirstOrDefault().StopCheckOutLocation.Name },
                    { "CheckOutTime", TimeOnly.FromDateTime(dateTime).ToString()}
                };

                // Gửi cho học sinh
                await _notificationService.SendNotificationFromTemplateAsync(
                    templateType: "CheckOut",
                    recipientIds: new List<Guid> { user.Id },
                    metadata: metadata,
                    createdBy: "system",
                    notiCategory: "ATTENDANCE"
                );

                // Nếu có phụ huynh thì gửi cho phụ huynh
                if (user.ParentId != null && user.ParentId != Guid.Empty)
                {
                    await _notificationService.SendNotificationFromTemplateAsync(
                        templateType: "CheckOut",
                        recipientIds: new List<Guid> { user.ParentId.Value },
                        metadata: metadata,
                        createdBy: "system",
                        notiCategory: "ATTENDANCE"
                    );
                }

            }
        }

        public async Task<BasePaginatedList<ResponseStudentInRouteAndShiftModel>> ListAbsentStudent(GetAbsentQuery req)
        {
            var page = req.page > 0 ? req.page : 0;
            var pageSize = req.pageSize > 0 ? req.pageSize : 10;

            var userRepo = _unitOfWork.GetRepository<User>();
            var userQuery = userRepo.Entities
            .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .Include(u => u.UserSchoolShifts)
            .ThenInclude(u => u.SchoolShift)
            .AsQueryable();


            if (req.routeId == null)
            {
                throw new ErrorException(StatusCodes.Status404NotFound, ResponseCodeConstants.NOT_FOUND, "Không tìm thấy tuyến!");
            }
            if (req.schoolShiftId == null)
            {
                throw new ErrorException(StatusCodes.Status404NotFound, ResponseCodeConstants.NOT_FOUND, "Không tìm thấy ca học!");
            }
            userQuery = userQuery.Where(x => x.UserSchoolShifts.Any(x => x.SchoolShiftId == req.schoolShiftId && !x.DeletedTime.HasValue));
            userQuery = userQuery.Where(x => x.HistoryTickets.Any(x => x.Ticket.Route.Id == req.routeId
            && x.Ticket.Route.IsActive == true
            && x.ValidUntil >= DateOnly.FromDateTime(CoreHelper.SystemTimeNow.DateTime)
            && x.Status == HistoryTicketStatus.PAID
            && !x.DeletedTime.HasValue));

            var listStudent = await userQuery
                .Select(u => new ResponseStudentInRouteAndShiftModel
                {
                    Id = u.Id,
                    FullName = u.FullName,
                    Gender = u.Gender,
                    DateOfBirth = u.DateOfBirth,
                    ProfileImageUrl = u.ProfileImageUrl,
                    Address = u.Address,
                    Email = u.Email,
                    ParentName = u.Parent.FullName,
                    PhoneNumber = u.PhoneNumber,
                    SchoolName = u.School.Name,
                    HistoryTicketId = u.HistoryTickets.
                    FirstOrDefault(x => x.ValidUntil >= DateOnly.FromDateTime(CoreHelper.SystemTimeNow.DateTime)
                    && x.Status == HistoryTicketStatus.PAID
                    && !x.DeletedTime.HasValue)!.Id,
                })
                .ToListAsync();

            var listCheckin = await _unitOfWork.GetRepository<Attendance>().Entities
                .Include(x => x.HistoryTicket)
                .Where(x => !x.DeletedTime.HasValue
                        && x.TripId == req.tripId)
                .ToListAsync();

            //lấy danh sách absent
            // Tạo danh sách các HistoryTicketId đã check-in
            var checkedInHistoryTicketIds = listCheckin
                .Select(x => x.HistoryTicket.UserId)
                .ToHashSet(); // Tối ưu tìm kiếm O(1)

            // Lọc danh sách học sinh chưa check-in
            var listAbsent = listStudent
                .Where(student => !checkedInHistoryTicketIds.Contains(student.Id));

            var totalCount = listAbsent.Count();

            //Paging
            var absents = listAbsent
                .Skip(req.page * req.pageSize)
                .Take(req.pageSize)
                .ToList();

            if (!absents.Any())
            {
                throw new ErrorException(StatusCodes.Status404NotFound, ErrorCode.NotFound, "Không có học sinh vắng nào được ghi nhận!");
            }

            var result = _mapper.Map<List<ResponseStudentInRouteAndShiftModel>>(absents);

            return new BasePaginatedList<ResponseStudentInRouteAndShiftModel>(result, totalCount, page, pageSize);
        }

        public async Task<BasePaginatedList<GetAttendanceForUserModel>> GetAttendanceForUser(
            int page = 0,
            int pageSize = 10,
            Guid? userId = null,
            DateOnly? date = null)
        {
            // Validate input
            if (pageSize <= 0) pageSize = 10;
            if (page < 0) page = 0;
            if (userId == null) throw new ArgumentNullException(nameof(userId));

            var schoolShiftQuery = _unitOfWork.GetRepository<SchoolShift>().Entities
                .Where(x => x.UserSchoolShifts.Any(uss => uss.StudentId == userId) && !x.DeletedTime.HasValue);

            var attendanceQuery = _unitOfWork.GetRepository<Attendance>().Entities
                .Where(a => !a.DeletedTime.HasValue &&
                           a.HistoryTicket.UserId == userId &&
                           a.Trip.Schedule.SchoolShift.UserSchoolShifts.Any(uss => uss.StudentId == userId));

            if (date.HasValue)
            {
                var dateTime = date.Value.ToDateTime(TimeOnly.MinValue);
                attendanceQuery = attendanceQuery.Where(a => a.CheckInTime.Date == dateTime.Date);
            }

            var query = from attendance in attendanceQuery
                        join shift in schoolShiftQuery
                            on attendance.Trip.Schedule.SchoolShiftId equals shift.Id
                        select new { attendance, shift };

            var totalCount = await query.CountAsync(); 

            var paginatedData = await query
                .Skip(page * pageSize)
                .Take(pageSize)
                .ToListAsync();

            //Kiểm tra dữ liệu thô
            if (paginatedData.Count == 0 && totalCount > 0)
            {
                Console.WriteLine($"Warning: Data mismatch! Total: {totalCount}, Page: {page}");
            }

            var paginatedItems = paginatedData.Select(x => new GetAttendanceForUserModel
            {
                Id = x.attendance.Id,
                ShiftType = x.shift.ShiftType.ToString(),
                SessionType = x.shift.SessionType.ToString(),
                CheckInTime = x.attendance.CheckInTime,
                CheckOutTime = x.attendance.CheckOutTime,
                CheckInLocation = x.attendance.StopCheckInLocation?.Name,
                CheckOutLocation = x.attendance.StopCheckOutLocation?.Name,
                Time = x.shift.Time
            }).ToList();

            return new BasePaginatedList<GetAttendanceForUserModel>(paginatedItems, totalCount, page, pageSize);
        }
    }
}
