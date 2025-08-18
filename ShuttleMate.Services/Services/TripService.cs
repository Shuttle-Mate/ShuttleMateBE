using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;
using ShuttleMate.Contract.Repositories.Entities;
using ShuttleMate.Contract.Repositories.IUOW;
using ShuttleMate.Contract.Services.Interfaces;
using ShuttleMate.Core.Bases;
using ShuttleMate.Core.Constants;
using ShuttleMate.Core.Utils;
using ShuttleMate.ModelViews.ScheduleModelViews;
using ShuttleMate.ModelViews.TripModelViews;
using ShuttleMate.ModelViews.UserModelViews;
using ShuttleMate.Services.Services.Infrastructure;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using static ShuttleMate.Contract.Repositories.Enum.GeneralEnum;

namespace ShuttleMate.Services.Services
{
    public class TripService : ITripService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IHttpContextAccessor _contextAccessor;
        private readonly IAttendanceService _attendanceService;
        private readonly INotificationService _notificationService;

        public TripService(IUnitOfWork unitOfWork, IMapper mapper, IHttpContextAccessor contextAccessor, IAttendanceService attendanceService, INotificationService notificationService)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _contextAccessor = contextAccessor;
            _attendanceService = attendanceService;
            _notificationService = notificationService;
        }

        public async Task<Guid> StartTrip(Guid scheduleId)
        {
            string currentUserIdString = Authentication.GetUserIdFromHttpContextAccessor(_contextAccessor);

            if (!Guid.TryParse(currentUserIdString, out Guid actualDriverId))
            {
                throw new ErrorException(StatusCodes.Status401Unauthorized, ResponseCodeConstants.UNAUTHORIZED, "Tài xế không hợp lệ");
            }

            DateTime now = DateTime.Now;
            var tripDate = DateOnly.FromDateTime(now);

            var tripRepository = _unitOfWork.GetRepository<Trip>();
            var activeTrip = await tripRepository.FindAsync(
                predicate: t => t.CreatedBy == currentUserIdString &&
                                 t.Status == TripStatusEnum.IN_PROGESS &&
                                 t.EndTime == null &&
                                 t.DeletedTime == null
            );

            if (activeTrip != null)
            {
                throw new ErrorException(StatusCodes.Status400BadRequest, ResponseCodeConstants.BADREQUEST, "Tài xế hiện đang có một chuyến đi khác đang hoạt động. Vui lòng kết thúc chuyến đi đó trước khi bắt đầu chuyến mới.");
            }

            // kiểm tra trong ScheduleOverrides
            var scheduleOverrideRepository = _unitOfWork.GetRepository<ScheduleOverride>();
            var overrideRecord = await scheduleOverrideRepository.FindAsync(
                predicate: so => so.ScheduleId == scheduleId &&
                                 so.Date == tripDate &&
                                 so.DeletedTime == null
            );

            if (overrideRecord != null)
            {
                // Nếu OverrideUserId null, cho phép tài xế chính start
                if (overrideRecord.OverrideUserId == null)
                {
                    // kiểm tra tài xế hiện tại có phải là tài xế chính không
                    var scheduleRepository = _unitOfWork.GetRepository<Schedule>();
                    var scheduleRecord = await scheduleRepository.FindAsync(
                        predicate: s => s.Id == scheduleId &&
                                        !s.DeletedTime.HasValue
                    );

                    if (scheduleRecord == null)
                    {
                        throw new ErrorException(StatusCodes.Status404NotFound, ResponseCodeConstants.NOT_FOUND, "Lịch trình không hợp lệ hoặc không tồn tại.");
                    }

                    if (actualDriverId != scheduleRecord.DriverId)
                    {
                        throw new ErrorException(StatusCodes.Status400BadRequest, ResponseCodeConstants.BADREQUEST, "Bạn không phải là tài xế được chỉ định cho lịch trình này.");
                    }
                }
                else
                {
                    // Có bản ghi override, kiểm tra xem tài xế hiện tại có phải là tài xế được thay thế không
                    if (actualDriverId != overrideRecord.OverrideUserId)
                    {
                        throw new ErrorException(StatusCodes.Status400BadRequest, ResponseCodeConstants.BADREQUEST, "Bạn không phải là tài xế được phép cho chuyến này theo lịch thay thế.");
                    }
                }
            }
            else
            {
                // không có bản ghi override, kiểm tra trong Schedules gốc
                var scheduleRepository = _unitOfWork.GetRepository<Schedule>();
                var scheduleRecord = await scheduleRepository.FindAsync(
                    predicate: s => s.Id == scheduleId &&
                                    s.DeletedTime == null
                );

                if (scheduleRecord == null)
                {
                    throw new ErrorException(StatusCodes.Status404NotFound, ResponseCodeConstants.NOT_FOUND, "Lịch trình không hợp lệ hoặc không tồn tại.");
                }

                // kiểm tra xem tài xế hiện tại có phải là tài xế được chỉ định trong lịch trình gốc không
                if (actualDriverId != scheduleRecord.DriverId)
                {
                    throw new ErrorException(StatusCodes.Status400BadRequest, ResponseCodeConstants.BADREQUEST, "Bạn không phải là tài xế được chỉ định cho lịch trình này.");
                }
            }

            var newTrip = new Trip();

            newTrip.ScheduleId = scheduleId;
            newTrip.CreatedBy = currentUserIdString;
            newTrip.LastUpdatedBy = currentUserIdString;

            newTrip.TripDate = DateOnly.FromDateTime(now);
            newTrip.StartTime = TimeOnly.FromDateTime(now);
            newTrip.EndTime = null; // Assuming EndTime is nullable and not provided in the model
            newTrip.Status = TripStatusEnum.IN_PROGESS;

            await _unitOfWork.GetRepository<Trip>().InsertAsync(newTrip);
            await _unitOfWork.SaveAsync();

            return newTrip.Id;
        }

        public async Task<BasePaginatedList<ResponseTripModel>> GetAllPaging(GetTripQuery req)
        {
            var page = req.page > 0 ? req.page : 0;
            var pageSize = req.pageSize > 0 ? req.pageSize : 10;

            var query = _unitOfWork.GetRepository<Trip>().Entities
                .Where(x => !x.DeletedTime.HasValue);

            // Filter by status (string to enum, upper-case)
            if (!string.IsNullOrWhiteSpace(req.status))
            {
                if (Enum.TryParse<TripStatusEnum>(req.status.Trim().ToUpperInvariant(), out var statusEnum))
                {
                    query = query.Where(x => x.Status == statusEnum);
                }
                else
                {
                    throw new ErrorException(StatusCodes.Status400BadRequest, ErrorCode.BadRequest, "Trạng thái chuyến xe không hợp lệ!");
                }
            }

            // Filter by startDate (TripDate) and endDate (TripDate)
            if (!string.IsNullOrWhiteSpace(req.startDate) && DateOnly.TryParse(req.startDate, out var startDate))
            {
                query = query.Where(x => x.TripDate >= startDate);
            }
            if (!string.IsNullOrWhiteSpace(req.endDate) && DateOnly.TryParse(req.endDate, out var endDate))
            {
                query = query.Where(x => x.TripDate <= endDate);
            }

            var totalCount = await query.CountAsync();

            var trips = await query
                .Skip(page * pageSize)
                .Take(pageSize)
                .ToListAsync();

            if (!trips.Any())
            {
                throw new ErrorException(StatusCodes.Status404NotFound, ErrorCode.NotFound, "Không có chuyến xe nào tồn tại!");
            }

            var result = _mapper.Map<List<ResponseTripModel>>(trips);

            return new BasePaginatedList<ResponseTripModel>(result, totalCount, page, pageSize);
        }

        public async Task<ResponseTripModel> GetByIdAsync(Guid tripId)
        {
            var trip = await _unitOfWork.GetRepository<Trip>().GetByIdAsync(tripId)
                ?? throw new ErrorException(StatusCodes.Status404NotFound, ResponseCodeConstants.NOT_FOUND, "Chuyến đi không tồn tại.");

            if (trip.DeletedTime.HasValue)
                throw new ErrorException(StatusCodes.Status404NotFound, ResponseCodeConstants.NOT_FOUND, "Chuyến đi đã bị xóa.");

            return _mapper.Map<ResponseTripModel>(trip);
        }

        public async Task EndTrip(Guid tripId, Guid routeId, Guid schoolShiftId)
        {
            string currentUserIdString = Authentication.GetUserIdFromHttpContextAccessor(_contextAccessor);

            if (!Guid.TryParse(currentUserIdString, out Guid actualDriverId))
            {
                throw new ErrorException(StatusCodes.Status401Unauthorized, ResponseCodeConstants.UNAUTHORIZED, "Tài xế không hợp lệ");
            }

            var tripRepository = _unitOfWork.GetRepository<Trip>();
            var tripToEnd = await tripRepository.GetByIdAsync(tripId);

            if (tripToEnd == null)
            {
                throw new ErrorException(StatusCodes.Status404NotFound, ResponseCodeConstants.NOT_FOUND, "Chuyến đi không tồn tại.");
            }

            if (tripToEnd.Status != TripStatusEnum.IN_PROGESS)
            {
                throw new ErrorException(StatusCodes.Status400BadRequest, ResponseCodeConstants.BADREQUEST, "Chuyến đi không ở trạng thái 'Đang tiến hành' để có thể kết thúc.");
            }

            if (tripToEnd.CreatedBy != currentUserIdString)
            {
                throw new ErrorException(StatusCodes.Status403Forbidden, ResponseCodeConstants.FORBIDDEN, "Bạn không có quyền kết thúc chuyến đi này. Chỉ tài xế đã bắt đầu chuyến mới được phép kết thúc.");
            }

            if (tripToEnd.Schedule.RouteId  != routeId)
            {
                throw new ErrorException(StatusCodes.Status400BadRequest, ResponseCodeConstants.BADREQUEST, "Chuyến đi sai tuyến, không thể kết thúc!");
            }

            if (tripToEnd.Schedule.SchoolShiftId != schoolShiftId)
            {
                throw new ErrorException(StatusCodes.Status400BadRequest, ResponseCodeConstants.BADREQUEST, "Chuyến đi sai ca học, không thể kết thúc!");
            }

            // Get all attendance records for this trip
            var listAttendance = await _unitOfWork.GetRepository<Attendance>().Entities
                .Include(x => x.HistoryTicket)
                    .ThenInclude(x => x.User)
                .Where(x => !x.DeletedTime.HasValue && x.TripId == tripId && (x.Status == AttendanceStatusEnum.CHECKED_IN || x.CheckOutLocation == null))
                .ToListAsync();

            if (listAttendance.Any())
            {
                var names = string.Join(", ", listAttendance.Select(a => a.HistoryTicket.User.FullName));
                throw new ErrorException(StatusCodes.Status400BadRequest, ResponseCodeConstants.BADREQUEST, $"Không thể kết thúc chuyến đi. Các học sinh sau chưa checkout: {names}");
            }

            tripToEnd.EndTime = TimeOnly.FromDateTime(DateTime.Now);
            tripToEnd.Status = TripStatusEnum.COMPLETED;
            tripToEnd.LastUpdatedBy = currentUserIdString;
            tripToEnd.LastUpdatedTime = CoreHelper.SystemTimeNow;

            tripRepository.Update(tripToEnd);
            await _unitOfWork.SaveAsync();

            var query = await _unitOfWork.GetRepository<Trip>().Entities
                .Include(x => x.Schedule)
                .Where(x => !x.DeletedTime.HasValue)
                .FirstOrDefaultAsync(x => x.Id == tripId);


            var userRepo = _unitOfWork.GetRepository<User>();
            var userQuery = userRepo.Entities
            .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .Include(u => u.UserSchoolShifts)
            .ThenInclude(u => u.SchoolShift)
            .AsQueryable();

            userQuery = userQuery.Where(x => x.UserSchoolShifts.Any(x => x.SchoolShiftId == schoolShiftId && !x.DeletedTime.HasValue));
            userQuery = userQuery.Where(x => x.HistoryTickets.Any(x => x.Ticket.Route.Id == routeId
            && x.Ticket.Route.IsActive == true
            && x.ValidUntil >= DateOnly.FromDateTime(DateTime.Now)
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
                    FirstOrDefault(x => x.ValidUntil >= DateOnly.FromDateTime(DateTime.Now)
                    && x.Status == HistoryTicketStatus.PAID
                    && !x.DeletedTime.HasValue)!.Id,
                })
                .ToListAsync();

            var listCheckin = await _unitOfWork.GetRepository<Attendance>().Entities
                .Include(x => x.HistoryTicket)
                .Where(x => !x.DeletedTime.HasValue
                        && x.TripId == tripId)
                .ToListAsync();

            var checkedInHistoryTicketIds = listCheckin
                .Select(x => x.HistoryTicket.UserId)
                .ToHashSet(); // Tối ưu tìm kiếm O(1)

            var listAbsent = listStudent
                .Where(student => !checkedInHistoryTicketIds.Contains(student.Id));

            var totalCount = listAbsent.Count();

            var absents = listAbsent.ToList();

            var absentList = _mapper.Map<List<ResponseStudentInRouteAndShiftModel>>(absents);

            var userIds = absentList.Select(s => s.Id).ToList();

            var users = await _unitOfWork.GetRepository<User>()
                .Entities
                .Where(u => userIds.Contains(u.Id))
                .Select(u => new { u.Id, u.FullName, u.ParentId })
                .ToListAsync();

            // 4. Gửi thông báo
            var createdBy = "system";

            

            // Tùy chỉnh metadata nếu dùng template
            foreach (var user in users)
            {
                //var recipientId = (user.ParentId != null && user.ParentId != Guid.Empty) ? user.ParentId.Value : user.Id;

                //DateTime dateTime = DateTime.Now;

                //var metadata = new Dictionary<string, string>
                //{
                //    { "Date", DateOnly.FromDateTime(dateTime).ToString() },
                //    { "StudentName", user.FullName },
                //    { "RouteName", query.Schedule.Route.RouteName}
                //};

                //await _notificationService.SendNotificationFromTemplateAsync(
                //    templateType: "AbsentNotification", // tên template bạn định nghĩa
                //    recipientIds: new List<Guid> { recipientId },
                //    metadata: metadata,
                //    createdBy: createdBy
                //);
                DateTime dateTime = DateTime.Now;

                var metadata = new Dictionary<string, string>
                {
                    { "Date", DateOnly.FromDateTime(dateTime).ToString() },
                    { "StudentName", user.FullName },
                    { "RouteName", query.Schedule.Route.RouteName }
                };

                // Gửi cho học sinh
                await _notificationService.SendNotificationFromTemplateAsync(
                    templateType: "AbsentNotificationForStudent",
                    recipientIds: new List<Guid> { user.Id },
                    metadata: metadata,
                    createdBy: "system",
                    notiCategory: "ATTENDANCE"
                );

                // Nếu có phụ huynh thì gửi cho phụ huynh
                if (user.ParentId != null && user.ParentId != Guid.Empty)
                {
                    await _notificationService.SendNotificationFromTemplateAsync(
                        templateType: "AbsentNotificationForParent",
                        recipientIds: new List<Guid> { user.ParentId.Value },
                        metadata: metadata,
                        createdBy: "system",
                        notiCategory: "ATTENDANCE"
                    );
                }

            }
        }

        public async Task UpdateAsync(Guid tripId, UpdateTripModel model)
        {
            var trip = await _unitOfWork.GetRepository<Trip>().GetByIdAsync(tripId)
                ?? throw new ErrorException(StatusCodes.Status404NotFound, ResponseCodeConstants.NOT_FOUND, "Chuyến đi không tồn tại.");

            var stops = trip.Schedule.Route.RouteStops
                .OrderBy(rs => trip.Schedule.Direction == RouteDirectionEnum.IN_BOUND ? rs.StopOrder : -rs.StopOrder)
                .ToList();

            var currentIndex = trip.CurrentStopIndex;

            var nextStop = trip.Schedule.Direction == RouteDirectionEnum.IN_BOUND
                ? stops.FirstOrDefault(s => s.StopOrder > currentIndex)
                : stops.FirstOrDefault(s => s.StopOrder < currentIndex);

            //if (nextStop == null)
                // noti xe đã đến trạm cuối

            if (model.Distance >= 50)
                // noti xe còn cách trạm bn model.Duration (cái này tính bằng giây nên nhớ chuyển sang phút)

                if (model.Distance < 50)
            {
                trip.CurrentStopIndex = nextStop.StopOrder;
                await _unitOfWork.GetRepository<Trip>().UpdateAsync(trip);
                await _unitOfWork.SaveAsync();

                //noti xe đã đến trạm
            }
        }
    }
}
