using AutoMapper;
using Google.Api.Gax;
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
        private readonly FirestoreService _firestoreService;

        public TripService(IUnitOfWork unitOfWork, IMapper mapper, IHttpContextAccessor contextAccessor, IAttendanceService attendanceService, INotificationService notificationService, FirestoreService firestoreService )
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _contextAccessor = contextAccessor;
            _attendanceService = attendanceService;
            _notificationService = notificationService;
            _firestoreService = firestoreService;
        }

        public async Task<Guid> StartTrip(Guid scheduleId)
        {
            string currentUserIdString = Authentication.GetUserIdFromHttpContextAccessor(_contextAccessor);

            if (!Guid.TryParse(currentUserIdString, out Guid actualDriverId))
            {
                throw new ErrorException(StatusCodes.Status401Unauthorized, ResponseCodeConstants.UNAUTHORIZED, "Tài xế không hợp lệ");
            }

            var vnTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Ho_Chi_Minh");
            var vietnamNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, vnTimeZone);
            //var todayVN = DateOnly.FromDateTime(vietnamNow);
            //DateTime now = DateTime.Now;
            var tripDate = DateOnly.FromDateTime(vietnamNow);

            var tripRepository = _unitOfWork.GetRepository<Trip>();
            var activeTrip = await tripRepository.FindAsync(
                predicate: t => t.CreatedBy == currentUserIdString &&
                                 t.Status == TripStatusEnum.IN_PROGRESS &&
                                 t.EndTime == null &&
                                 t.DeletedTime == null
            );

            if (activeTrip != null)
            {
                throw new ErrorException(StatusCodes.Status400BadRequest, ResponseCodeConstants.BADREQUEST, "Tài xế hiện đang có một chuyến đi khác đang hoạt động. Vui lòng kết thúc chuyến đi đó trước khi bắt đầu chuyến mới.");
            }

            //// Lấy thông tin schedule
            var schedule = await _unitOfWork.GetRepository<Schedule>().Entities
                .Where(s => s.Id == scheduleId && s.DeletedTime == null)
                .Include(s => s.Route)
                    .ThenInclude(r => r.RouteStops)
                    .ThenInclude(rs => rs.Stop)
                .FirstOrDefaultAsync()
                ?? throw new ErrorException(StatusCodes.Status404NotFound, ResponseCodeConstants.NOT_FOUND,
                    "Lịch trình không hợp lệ hoặc không tồn tại.");

            // Validate cùng thứ trong tuần (DayOfWeek dạng "1111100", 0=Thứ 2, 6=Chủ Nhật)
            if (!string.IsNullOrEmpty(schedule.DayOfWeek))
            {
                int dayIndex = ((int)vietnamNow.DayOfWeek + 6) % 7; //Thứ 2 = 0, ..., Chủ Nhật = 6
                if (schedule.DayOfWeek.Length != 7 || schedule.DayOfWeek[dayIndex] != '1')
                {
                    throw new ErrorException(StatusCodes.Status400BadRequest, ResponseCodeConstants.BADREQUEST, "Chỉ được bắt đầu chuyến vào đúng ngày trong tuần của lịch trình.");
                }
            }

            // Validate chỉ được start trước giờ khởi hành tối đa 10 phút
            //if (schedule.SchoolShift != null)
            //{
            //    var startTime = schedule.DepartureTime; // kiểu TimeOnly
            //    var scheduleDateTime = vietnamNow.Date + startTime.ToTimeSpan();
            //    if (vietnamNow < scheduleDateTime.AddMinutes(-10) || vietnamNow > scheduleDateTime)
            //    {
            //        throw new ErrorException(StatusCodes.Status400BadRequest, ResponseCodeConstants.BADREQUEST, "Chỉ được bắt đầu chuyến trước giờ khởi hành tối đa 10 phút.");
            //    }
            //}

            // Kiểm tra quyền tài xế
            var overrideRecord = await _unitOfWork.GetRepository<ScheduleOverride>().FindAsync(
                so => so.ScheduleId == scheduleId &&
                      so.Date == tripDate &&
                      so.DeletedTime == null);

            bool hasPermission = overrideRecord != null
                ? (overrideRecord.OverrideUserId == null ? actualDriverId == schedule.DriverId
                : actualDriverId == overrideRecord.OverrideUserId)
                : actualDriverId == schedule.DriverId;

            if (!hasPermission)
            {
                throw new ErrorException(StatusCodes.Status400BadRequest, ResponseCodeConstants.BADREQUEST, "Bạn không phải là tài xế được phép cho chuyến này.");
            }

            int currentStopIndex = DetermineCurrentStopIndex(schedule);

            var newTrip = new Trip
            {
                ScheduleId = scheduleId,
                CreatedBy = currentUserIdString,
                LastUpdatedBy = currentUserIdString,
                CurrentStopIndex = currentStopIndex,
                TripDate = DateOnly.FromDateTime(vietnamNow),
                StartTime = TimeOnly.FromDateTime(vietnamNow),
                EndTime = null,
                Status = TripStatusEnum.IN_PROGRESS
            };

            await _unitOfWork.GetRepository<Trip>().InsertAsync(newTrip);
            await _unitOfWork.SaveAsync();

            // Sau khi InsertAsync và SaveAsync trip mới
            var trip = await _unitOfWork.GetRepository<Trip>()
                .Entities
                .Include(t => t.Schedule)
                    .ThenInclude(s => s.Route)
                .Include(t => t.Schedule)
                    .ThenInclude(s => s.Shuttle)
                .Include(t => t.Schedule)
                    .ThenInclude(s => s.Driver)
                .FirstOrDefaultAsync(t => t.Id == newTrip.Id);

            var stops = trip.Schedule.Route.RouteStops
                .OrderBy(rs => trip.Schedule.Direction == RouteDirectionEnum.IN_BOUND
                    ? rs.StopOrder
                    : -rs.StopOrder) // Sắp xếp giảm dần cho OUT_BOUND
                .ToList();

            var currentIndex = trip.CurrentStopIndex;

            // Tìm next stop theo hướng
            RouteStop nextStop = null;

            if (trip.Schedule.Direction == RouteDirectionEnum.IN_BOUND)
            {
                // IN_BOUND: đi từ stop nhỏ đến lớn, next là stop có order lớn hơn current
                nextStop = stops.FirstOrDefault(s => s.StopOrder > currentIndex);
            }
            else
            {
                // OUT_BOUND: đi từ stop lớn đến nhỏ, next là stop có order nhỏ hơn current
                nextStop = stops.FirstOrDefault(s => s.StopOrder < currentIndex);
            }

            var nextStopObj = nextStop != null
                ? new { stopId = nextStop.Stop.Id.ToString(), stopName = nextStop.Stop.Name }
                : null;

            var routeStop = await _unitOfWork.GetRepository<RouteStop>()
                    .Entities
                    .Include(rs => rs.Stop)
                    .FirstOrDefaultAsync(rs => rs.RouteId == trip.Schedule.RouteId && rs.StopOrder == trip.CurrentStopIndex);

            var docRef = _firestoreService.GetCollection("active_trips").Document(trip.Id.ToString());
            await docRef.SetAsync(new
            {
                tripId = trip.Id.ToString(),
                routeId = trip.Schedule.RouteId.ToString(),
                shuttleName = trip.Schedule.Shuttle.Name,
                driverName = trip.Schedule.Driver.FullName,
                currentStop = new { stopIndex = trip.CurrentStopIndex, stopName = routeStop.Stop.Name },
                nextStop = nextStopObj,
                status = trip.Status.ToString(),
                updatedTime = DateTime.UtcNow,
                schoolShift = trip.Schedule.SchoolShift.Id.ToString()
            });

            return newTrip.Id;
        }

        private int DetermineCurrentStopIndex(Schedule schedule)
        {
            if (schedule.Route?.RouteStops == null || !schedule.Route.RouteStops.Any())
                return 1;

            var orderedStops = schedule.Route.RouteStops.OrderBy(rs => rs.StopOrder).ToList();

            return schedule.Direction == RouteDirectionEnum.IN_BOUND
                ? orderedStops.First().StopOrder
                : orderedStops.Last().StopOrder;
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

            if (tripToEnd.Status != TripStatusEnum.IN_PROGRESS)
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
                throw new ErrorException(StatusCodes.Status400BadRequest, ResponseCodeConstants.BADREQUEST, $"Không thể kết thúc chuyến đi. Các học sinh sau chưa check out: {names}");
            }

            tripToEnd.EndTime = TimeOnly.FromDateTime(DateTime.Now);
            tripToEnd.Status = TripStatusEnum.COMPLETED;
            tripToEnd.LastUpdatedBy = currentUserIdString;
            tripToEnd.LastUpdatedTime = CoreHelper.SystemTimeNow;

            tripRepository.Update(tripToEnd);
            await _unitOfWork.SaveAsync();

            var vnTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Ho_Chi_Minh");
            var vietnamNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, vnTimeZone);
            var todayVN = DateOnly.FromDateTime(vietnamNow);

            var query = await _unitOfWork.GetRepository<Trip>().Entities
                .Include(x => x.Schedule)
                .Where(x => !x.DeletedTime.HasValue)
                .FirstOrDefaultAsync(x => x.Id == tripId);


            var userRepo = _unitOfWork.GetRepository<User>();
            var userQuery = userRepo.Entities
                .Include(u => u.UserSchoolShifts)
                    .ThenInclude(uss => uss.SchoolShift)
                .Include(u => u.HistoryTickets)
                    .ThenInclude(ht => ht.Attendances)
                        .ThenInclude(a => a.Trip)
                .Include(u => u.HistoryTickets)
                    .ThenInclude(ht => ht.Ticket)
                        .ThenInclude(t => t.Route)
                .Include(u => u.Parent)
                .Include(u => u.School)
                .Where(u => !u.DeletedTime.HasValue)
                .AsQueryable();

            userQuery = userQuery.Where(x => x.HistoryTickets.Any(y =>
                y.Ticket.RouteId == routeId &&
                y.Ticket.Route.IsActive == true &&
                y.ValidUntil >= todayVN &&
                y.ValidFrom <= todayVN &&
                y.HistoryTicketSchoolShifts.Any(hs => hs.SchoolShiftId == schoolShiftId) &&
                y.Status == HistoryTicketStatus.PAID &&
                !y.DeletedTime.HasValue));

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

            var docRef = _firestoreService.GetCollection("active_trips").Document(tripId.ToString());
            await docRef.UpdateAsync(new Dictionary<string, object>
            {
                { "status", tripToEnd.Status.ToString() }
            });
        }

        public async Task UpdateAsync(Guid tripId, UpdateTripModel model)
        {
            var trip = await _unitOfWork.GetRepository<Trip>().GetByIdAsync(tripId)
                ?? throw new ErrorException(StatusCodes.Status404NotFound, ResponseCodeConstants.NOT_FOUND, "Chuyến đi không tồn tại.");

            var stops = trip.Schedule.Route.RouteStops
                .OrderBy(rs => trip.Schedule.Direction == RouteDirectionEnum.IN_BOUND
                    ? rs.StopOrder
                    : -rs.StopOrder) // Sắp xếp giảm dần cho OUT_BOUND
                .ToList();

            var currentIndex = trip.CurrentStopIndex;

            // Tìm next stop theo hướng
            RouteStop nextStop = null;

            if (trip.Schedule.Direction == RouteDirectionEnum.IN_BOUND)
            {
                // IN_BOUND: đi từ stop nhỏ đến lớn, next là stop có order lớn hơn current
                nextStop = stops.FirstOrDefault(s => s.StopOrder > currentIndex);
            }
            else
            {
                // OUT_BOUND: đi từ stop lớn đến nhỏ, next là stop có order nhỏ hơn current
                nextStop = stops.FirstOrDefault(s => s.StopOrder < currentIndex);
            }


            var vnTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Ho_Chi_Minh");
            var vietnamNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, vnTimeZone);
            var todayVN = DateOnly.FromDateTime(vietnamNow);
            var dayOfWeek = vietnamNow.DayOfWeek.ToString();


            var userAttendances = await _unitOfWork.GetRepository<User>().Entities
                .Include(u => u.UserSchoolShifts)
                    .ThenInclude(uss => uss.SchoolShift)
                .Include(u => u.HistoryTickets)
                    .ThenInclude(ht => ht.Attendances)
                        .ThenInclude(a => a.Trip)
                .Include(u => u.HistoryTickets)
                    .ThenInclude(ht => ht.Ticket)
                        .ThenInclude(t => t.Route)
                .Include(u => u.Parent)
                .Include(u => u.School)
                .Where(u => !u.DeletedTime.HasValue)
                .Where(x => x.HistoryTickets.Any(y =>
                y.Ticket.RouteId == trip.Schedule.RouteId &&
                y.Ticket.Route.IsActive == true &&
                y.ValidUntil >= todayVN &&
                y.ValidFrom <= todayVN &&
                y.HistoryTicketSchoolShifts.Any(hs => hs.SchoolShiftId == trip.Schedule.SchoolShiftId) &&
                y.Status == HistoryTicketStatus.PAID &&
                !y.DeletedTime.HasValue))
                .ToListAsync();

            var userIds = userAttendances.Select(s => s.Id).ToList();

            var users = await _unitOfWork.GetRepository<User>()
                .Entities
                .Where(u => userIds.Contains(u.Id))
                .Select(u => new { u.Id, u.FullName, u.ParentId })
                .ToListAsync();

            if (nextStop == null)
            {
                // noti xe đã đến trạm cuối {{StopName}}

                foreach (var user in users)
                {

                    var metadata = new Dictionary<string, string>
                {
                    { "RouteName", trip.Schedule.Route.RouteName },
                    { "ArrivedTime", vietnamNow.ToString() }
                };

                    // Gửi cho học sinh
                    await _notificationService.SendNotificationFromTemplateAsync(
                        templateType: "ArrivedLastStop",
                        recipientIds: new List<Guid> { user.Id },
                        metadata: metadata,
                        createdBy: "system",
                        notiCategory: "TRIP_STATUS"
                    );

                    // Nếu có phụ huynh thì gửi cho phụ huynh
                    if (user.ParentId != null && user.ParentId != Guid.Empty)
                    {
                        await _notificationService.SendNotificationFromTemplateAsync(
                            templateType: "ArrivedLastStop",
                            recipientIds: new List<Guid> { user.ParentId.Value },
                            metadata: metadata,
                            createdBy: "system",
                            notiCategory: "TRIP_STATUS"
                        );
                    }

                }
            }
            var duration = model.Duration / 60;
            //sửa lại duration = 5p thì noti
            if (duration == 5)
            {
                foreach (var user in users)
                {

                    var metadata = new Dictionary<string, string>
                    {
                        { "StopName", nextStop.Stop.Name },
                    };

                    // Gửi cho học sinh
                    await _notificationService.SendNotificationFromTemplateAsync(
                        templateType: "PrepareArrivedStop",
                        recipientIds: new List<Guid> { user.Id },
                        metadata: metadata,
                        createdBy: "system",
                        notiCategory: "TRIP_STATUS"
                    );

                    // Nếu có phụ huynh thì gửi cho phụ huynh
                    if (user.ParentId != null && user.ParentId != Guid.Empty)
                    {
                        await _notificationService.SendNotificationFromTemplateAsync(
                            templateType: "PrepareArrivedStop",
                            recipientIds: new List<Guid> { user.ParentId.Value },
                            metadata: metadata,
                            createdBy: "system",
                            notiCategory: "TRIP_STATUS"
                        );
                    }

                }
            }
            // noti xe còn cách trạm bn model.Duration (cái này tính bằng giây nên nhớ chuyển sang phút)

            if (model.Distance < 100)
            {
                foreach (var user in users)
                {

                    var metadata = new Dictionary<string, string>
                    {
                        { "StopName", nextStop.Stop.Name },
                    };

                    // Gửi cho học sinh
                    await _notificationService.SendNotificationFromTemplateAsync(
                        templateType: "ArrivedStop",
                        recipientIds: new List<Guid> { user.Id },
                        metadata: metadata,
                        createdBy: "system",
                        notiCategory: "TRIP_STATUS"
                    );

                    // Nếu có phụ huynh thì gửi cho phụ huynh
                    if (user.ParentId != null && user.ParentId != Guid.Empty)
                    {
                        await _notificationService.SendNotificationFromTemplateAsync(
                            templateType: "ArrivedStop",
                            recipientIds: new List<Guid> { user.ParentId.Value },
                            metadata: metadata,
                            createdBy: "system",
                            notiCategory: "TRIP_STATUS"
                        );
                    }
                }

                trip.CurrentStopIndex = nextStop.StopOrder;
                await _unitOfWork.GetRepository<Trip>().UpdateAsync(trip);
                await _unitOfWork.SaveAsync();

                var routeStop = await _unitOfWork.GetRepository<RouteStop>()
                    .Entities
                    .Include(rs => rs.Stop)
                    .FirstOrDefaultAsync(rs => rs.RouteId == trip.Schedule.RouteId && rs.StopOrder == trip.CurrentStopIndex);

                var docRef = _firestoreService.GetCollection("active_trips").Document(trip.Id.ToString());
                await docRef.SetAsync(new
                {
                    tripId = trip.Id.ToString(),
                    routeId = trip.Schedule.RouteId.ToString(),
                    shuttleName = trip.Schedule.Shuttle.Name,
                    driverName = trip.Schedule.Driver.FullName,
                    currentStop = new { stopIndex = trip.CurrentStopIndex, stopName = routeStop.Stop.Name},
                    nextStop = new { stopId = nextStop.Stop.Id.ToString(), stopName = nextStop.Stop.Name, duration = model.Duration, distance = model.Distance},
                    status = trip.Status.ToString(),
                    updatedTime = DateTime.UtcNow
                });
            }
        }

        public async Task<BasePaginatedList<RouteShiftModels>> GetRouteShiftAsync(Guid userId)
        {
            var today = DateOnly.FromDateTime(DateTime.Now);

            // Lấy tất cả vé còn hiệu lực của user (đã thanh toán, trong thời hạn, chưa xóa)
            var tickets = await _unitOfWork.GetRepository<HistoryTicket>().Entities
                .Where(ht =>
                    ht.UserId == userId &&
                    ht.Status == HistoryTicketStatus.PAID &&
                    ht.ValidFrom <= today &&
                    ht.ValidUntil >= today &&
                    !ht.DeletedTime.HasValue)
                .Include(ht => ht.Ticket)
                    .ThenInclude(t => t.Route)
                .Include(ht => ht.HistoryTicketSchoolShifts)
                .ToListAsync();

            // Lấy các vé có ít nhất 1 ca học
            var routeShiftPairs = tickets
                .Where(ht => ht.HistoryTicketSchoolShifts != null && ht.HistoryTicketSchoolShifts.Any())
                .SelectMany(ht => ht.HistoryTicketSchoolShifts
                    .Select(hs => new { RouteId = ht.Ticket.RouteId, SchoolShiftId = hs.SchoolShiftId, RouteName = ht.Ticket.Route.RouteName }))
                .Distinct()
                .ToList();

            // Gom nhóm theo RouteId, mỗi nhóm là 1 RouteShiftModels
            var groupedResult = routeShiftPairs
                .GroupBy(x => new { x.RouteId, x.RouteName})
                .Select(g => new RouteShiftModels
                {
                    RouteId = g.Key.RouteId,
                    RouteName = g.Key.RouteName,
                    SchoolShiftId = g.Select(x => x.SchoolShiftId).Distinct().ToList()
                })
                .ToList();

            var result = new BasePaginatedList<RouteShiftModels>(groupedResult, groupedResult.Count, 0, groupedResult.Count);

            return result;
        }

        public async Task<BasePaginatedList<TripHistoryModel>> GetDriverTripHistory(Guid driverId, DateOnly? from, DateOnly? to, int page = 0, int pageSize = 10)
        {
            var query = _unitOfWork.GetRepository<Trip>().Entities
                 .Include(t => t.Schedule)
                     .ThenInclude(s => s.Route)
                 .Include(t => t.Schedule)
                     .ThenInclude(s => s.SchoolShift)
                 .Include(t => t.Schedule)
                     .ThenInclude(s => s.Shuttle)
                 .Include(t => t.Schedule)
                     .ThenInclude(s => s.Driver)
                 .Where(t => t.Schedule.DriverId == driverId && !t.DeletedTime.HasValue)
                 .Where(t => !from.HasValue || t.TripDate >= from)
                 .Where(t => !to.HasValue || t.TripDate <= to);

            // Lọc các trip mà driverId thực sự là người được phép xem (tài xế gốc hoặc override)
            query = query.Where(t =>
                // Lấy override theo ngày trip
                !t.Schedule.ScheduleOverrides.Any(so => so.Date == t.TripDate && so.DeletedTime == null)
                    ? t.Schedule.DriverId == driverId // Không có override, chỉ tài xế gốc
                    : t.Schedule.ScheduleOverrides.Any(so => so.Date == t.TripDate && so.DeletedTime == null && so.OverrideUserId == driverId) // Có override, chỉ tài xế thay thế
            );

            var totalCount = await query.CountAsync();

            var trips = await query
                .OrderByDescending(t => t.TripDate)
                .Skip(page * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var result = new List<TripHistoryModel>();
            foreach (var trip in trips)
            {
                var attendances = await _unitOfWork.GetRepository<Attendance>().Entities
                    .Include(a => a.HistoryTicket)
                        .ThenInclude(ht => ht.User)
                    .Where(a => a.TripId == trip.Id && !a.DeletedTime.HasValue)
                    .ToListAsync();

                result.Add(new TripHistoryModel
                {
                    TripId = trip.Id,
                    TripDate = trip.TripDate,
                    StartTime = trip.StartTime,
                    EndTime = trip.EndTime,
                    RouteName = trip.Schedule.Route?.RouteName,
                    RouteCode = trip.Schedule.Route?.RouteCode,
                    Shift = trip.Schedule.SchoolShift?.ShiftType.ToString(),
                    Session = trip.Schedule.SchoolShift?.SessionType.ToString(),
                    DriverName = trip.Schedule.Driver?.FullName,
                    ShuttleName = trip.Schedule.Shuttle?.Name,
                    Status = trip.Status.ToString(),
                    StudentAttendances = attendances.Select(a => new StudentAttendanceInfo
                    {
                        StudentId = a.HistoryTicket.User.Id,
                        StudentName = a.HistoryTicket.User.FullName,
                        CheckInLocation = a.StopCheckInLocation?.Name,
                        CheckInTime = a.CheckInTime,
                        CheckOutLocation = a.StopCheckOutLocation?.Name,
                        CheckOutTime = a.CheckOutTime,
                        AttendanceStatus = a.Status.ToString()
                    }).ToList()
                });
            }

            return new BasePaginatedList<TripHistoryModel>(result, totalCount, page, pageSize);
        }

        public async Task<BasePaginatedList<TripHistoryModel>> GetStudentTripHistory(Guid studentId, DateOnly? from, DateOnly? to, int page = 0, int pageSize = 10)
        {
            var query = _unitOfWork.GetRepository<Attendance>().Entities
                .Include(a => a.Trip)
                    .ThenInclude(t => t.Schedule)
                        .ThenInclude(s => s.Route)
                .Include(a => a.Trip)
                    .ThenInclude(t => t.Schedule)
                        .ThenInclude(s => s.SchoolShift)
                .Include(a => a.Trip)
                    .ThenInclude(t => t.Schedule)
                        .ThenInclude(s => s.Driver)
                .Include(a => a.Trip)
                    .ThenInclude(t => t.Schedule)
                        .ThenInclude(s => s.Shuttle)
                .Include(a => a.HistoryTicket)
                    .ThenInclude(ht => ht.User)
                .Where(a => a.HistoryTicket.UserId == studentId && !a.DeletedTime.HasValue)
                .Where(a => !from.HasValue || a.Trip.TripDate >= from)
                .Where(a => !to.HasValue || a.Trip.TripDate <= to)
                .OrderByDescending(a => a.Trip.TripDate);

            var totalCount = await query.CountAsync();

            var attendances = await query
                .Skip(page * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var result = attendances.Select(a => new TripHistoryModel
            {
                TripId = a.Trip.Id,
                TripDate = a.Trip.TripDate,
                StartTime = a.Trip.StartTime,
                EndTime = a.Trip.EndTime,
                RouteName = a.Trip.Schedule.Route?.RouteName,
                RouteCode = a.Trip.Schedule.Route?.RouteCode,
                Shift = a.Trip.Schedule.SchoolShift?.ShiftType.ToString(),
                Session = a.Trip.Schedule.SchoolShift?.SessionType.ToString(),
                DriverName = a.Trip.Schedule.Driver?.FullName,
                ShuttleName = a.Trip.Schedule.Shuttle?.Name,
                Status = a.Trip.Status.ToString(),
                MyAttendance = new StudentAttendanceInfo
                {
                    StudentId = a.HistoryTicket.User.Id,
                    StudentName = a.HistoryTicket.User.FullName,
                    CheckInLocation = a.StopCheckInLocation?.Name,
                    CheckInTime = a.CheckInTime,
                    CheckOutLocation = a.StopCheckOutLocation?.Name,
                    CheckOutTime = a.CheckOutTime,
                    AttendanceStatus = a.Status.ToString()
                }
            }).ToList();

            return new BasePaginatedList<TripHistoryModel>(result, totalCount, page, pageSize);
        }

        public async Task<BasePaginatedList<TripHistoryModel>> GetParentTripHistory(Guid parentId, Guid studentId, DateOnly? from, DateOnly? to, int page = 0, int pageSize = 10)
        {
            // Kiểm tra parentId có phải là parent của studentId không
            var student = await _unitOfWork.GetRepository<User>().Entities
                .FirstOrDefaultAsync(u => u.Id == studentId && !u.DeletedTime.HasValue);

            if (student == null)
                throw new ErrorException(StatusCodes.Status404NotFound, ResponseCodeConstants.NOT_FOUND, "Học sinh không tồn tại.");

            if (student.ParentId == null || student.ParentId != parentId)
                throw new ErrorException(StatusCodes.Status403Forbidden, ResponseCodeConstants.FORBIDDEN, "Bạn không có quyền xem lịch sử chuyến đi của học sinh này.");

            // Nếu đúng, trả về lịch sử chuyến đi của học sinh (có phân trang)
            return await GetStudentTripHistory(studentId, from, to, page, pageSize);
        }
    }
}
