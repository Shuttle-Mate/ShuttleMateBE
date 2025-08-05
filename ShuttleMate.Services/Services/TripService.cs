using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Org.BouncyCastle.Pkix;
using ShuttleMate.Contract.Repositories.Entities;
using ShuttleMate.Contract.Repositories.IUOW;
using ShuttleMate.Contract.Services.Interfaces;
using ShuttleMate.Core.Bases;
using ShuttleMate.Core.Constants;
using ShuttleMate.Core.Utils;
using ShuttleMate.ModelViews.AttendanceModelViews;
using ShuttleMate.ModelViews.RouteModelViews;
using ShuttleMate.ModelViews.TripModelViews;
using ShuttleMate.Services.Services.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
        private readonly IFirebaseService _firebaseService;

        public TripService(IUnitOfWork unitOfWork, IMapper mapper, IHttpContextAccessor contextAccessor, IAttendanceService attendanceService, INotificationService notificationService, IFirebaseService firebaseService)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _contextAccessor = contextAccessor;
            _attendanceService = attendanceService;
            _notificationService = notificationService;
            _firebaseService = firebaseService;
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
                // Có bản ghi override, kiểm tra xem tài xế hiện tại có phải là tài xế được thay thế không
                if (actualDriverId != overrideRecord.OverrideUserId)
                {
                    throw new ErrorException(StatusCodes.Status400BadRequest, ResponseCodeConstants.BADREQUEST, "Bạn không phải là tài xế được phép cho chuyến này theo lịch thay thế.");
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

        public Task<ResponseTripModel> GetById(Guid tripId)
        {
            throw new NotImplementedException();
        }

        public Task UpdateTrip(UpdateTripModel model)
        {
            throw new NotImplementedException();
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

            // 1. Lấy danh sách absent
            var absentQuery = new GetAbsentQuery
            {
                tripId = tripId,
                routeId = routeId,
                schoolShiftId = schoolShiftId,
                page = 0,
                pageSize = int.MaxValue // lấy tất cả
            };
            var absentList = await _attendanceService.ListAbsentStudent(absentQuery);

            // 2. Lấy danh sách userId và parentId
            var userIds = absentList.Items.Select(s => s.Id).ToList();

            // Lấy thông tin user để lấy parentId
            var users = await _unitOfWork.GetRepository<User>()
                .Entities
                .Where(u => userIds.Contains(u.Id))
                .Select(u => new { u.Id, u.FullName, u.ParentId })
                .ToListAsync();

            // 3. Chuẩn bị danh sách người nhận
            //var recipientIds = new List<Guid>();
            //foreach (var user in users)
            //{
            //    if (user.ParentId != null && user.ParentId != Guid.Empty)
            //        recipientIds.Add(user.ParentId.Value);
            //    else
            //        recipientIds.Add(user.Id);
            //}

            // 4. Gửi thông báo
            var createdBy = "system";

            // Tùy chỉnh metadata nếu dùng template
            foreach (var user in users)
            {
                var recipientId = (user.ParentId != null && user.ParentId != Guid.Empty) ? user.ParentId.Value : user.Id;

                var metadata = new Dictionary<string, string>
                {
                    { "StudentName", user.FullName },
                    { "RouteName", query.Schedule.Route.RouteName}
                    // Thêm các biến khác nếu cần
                };

                await _notificationService.SendNotificationFromTemplateAsync(
                    templateType: "AbsentNotification", // tên template bạn định nghĩa
                    recipientIds: new List<Guid> { recipientId },
                    metadata: metadata,
                    createdBy: createdBy
                );
            }
        }
    }
}
