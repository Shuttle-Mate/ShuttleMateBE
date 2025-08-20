using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using ShuttleMate.Contract.Repositories.Entities;
using ShuttleMate.Contract.Repositories.IUOW;
using ShuttleMate.Contract.Services.Interfaces;
using ShuttleMate.Core.Bases;
using ShuttleMate.Core.Constants;
using ShuttleMate.Core.Utils;
using ShuttleMate.ModelViews.ScheduleOverrideModelView;
using ShuttleMate.Services.Services.Infrastructure;
using static ShuttleMate.Contract.Repositories.Enum.GeneralEnum;

namespace ShuttleMate.Services.Services
{
    public class ScheduleOverrideService : IScheduleOverrideService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IHttpContextAccessor _contextAccessor;
        private readonly IGenericRepository<ScheduleOverride> _scheduleOverrideRepo;
        private readonly IGenericRepository<Schedule> _scheduleRepo;
        private readonly IGenericRepository<SchoolShift> _schoolShiftRepo;
        private readonly IGenericRepository<Shuttle> _shuttleRepo;
        private readonly IGenericRepository<User> _userRepo;
        private readonly INotificationService _notificationService;

        public ScheduleOverrideService(IUnitOfWork unitOfWork, IMapper mapper, IHttpContextAccessor contextAccessor, INotificationService notificationService)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _contextAccessor = contextAccessor;
            _scheduleOverrideRepo = _unitOfWork.GetRepository<ScheduleOverride>();
            _scheduleRepo = _unitOfWork.GetRepository<Schedule>();
            _schoolShiftRepo = _unitOfWork.GetRepository<SchoolShift>();
            _shuttleRepo = _unitOfWork.GetRepository<Shuttle>();
            _userRepo = _unitOfWork.GetRepository<User>();
            _notificationService = notificationService;
        }

        public async Task CreateAsync(CreateScheduleOverrideModel model)
        {
            var userId = Authentication.GetUserIdFromHttpContextAccessor(_contextAccessor);
            model.TrimAllStrings();

            if (model.OverrideShuttleId == null && model.OverrideUserId == null)
                throw new ErrorException(StatusCodes.Status400BadRequest, ResponseCodeConstants.BADREQUEST, "Phải chỉ định ít nhất một thay đổi (xe hoặc tài xế).");

            var originalSchedule = await _scheduleRepo.GetByIdAsync(model.ScheduleId)
                ?? throw new ErrorException(StatusCodes.Status404NotFound, ResponseCodeConstants.NOT_FOUND, "Lịch trình gốc không tồn tại.");

            if (model.Date < originalSchedule.From || model.Date > originalSchedule.To)
                throw new ErrorException(StatusCodes.Status400BadRequest, ResponseCodeConstants.BADREQUEST, $"Ngày thay thế {model.Date:dd/MM/yyyy} phải nằm trong khoảng từ {originalSchedule.From:dd/MM/yyyy} đến {originalSchedule.To:dd/MM/yyyy}.");

            // Lịch trình thay thế cho ngày này
            var existingOverride = _scheduleOverrideRepo.Entities
                .Include(x => x.OriginalUser)
                .Include(x => x.OverrideUser)
                .FirstOrDefault(x =>
                x.ScheduleId == model.ScheduleId &&
                x.Date == model.Date &&
                !x.DeletedTime.HasValue);

            // Nếu đã có lịch trình thay thế cho ngày này
            if (existingOverride != null)
            {
                // Thêm xe thay thế (đã có tài xế thay thế)
                if (existingOverride.OverrideUserId != null &&
                    existingOverride.OverrideShuttleId == null &&
                    model.OverrideShuttleId != null &&
                    model.OverrideUserId == null)
                {
                    existingOverride.OverrideShuttleId = model.OverrideShuttleId;
                    existingOverride.ShuttleReason = model.ShuttleReason ?? existingOverride.ShuttleReason;
                    existingOverride.LastUpdatedBy = userId;
                    existingOverride.LastUpdatedTime = CoreHelper.SystemTimeNow;

                    await _scheduleOverrideRepo.UpdateAsync(existingOverride);
                    await _unitOfWork.SaveAsync();

                    // Noti tài xế
                    var metadata = new Dictionary<string, string>
                    {
                        { "DriverName", existingOverride.OverrideUser.FullName }
                    };

                    await _notificationService.SendNotificationFromTemplateAsync(
                        templateType: "UpdateSchedule",
                        recipientIds: new List<Guid> { (Guid)existingOverride.OverrideUserId },
                        metadata: metadata,
                        createdBy: "system",
                        notiCategory: "SCHEDULE"
                    );
                    return;
                }
                
                // Thêm tài xế thay thế (đã có xe thay thế)
                if (existingOverride.OverrideShuttleId != null &&
                    existingOverride.OverrideUserId == null &&
                    model.OverrideUserId != null &&
                    model.OverrideShuttleId == null)
                {
                    existingOverride.OverrideUserId = model.OverrideUserId;
                    existingOverride.DriverReason = model.DriverReason ?? existingOverride.DriverReason;
                    existingOverride.LastUpdatedBy = userId;
                    existingOverride.LastUpdatedTime = CoreHelper.SystemTimeNow;

                    await _scheduleOverrideRepo.UpdateAsync(existingOverride);
                    await _unitOfWork.SaveAsync();
                    
                    // Noti tài xế
                    var metadata01 = new Dictionary<string, string>
                    {
                        { "DriverName", existingOverride.OriginalUser.FullName }
                    };

                    await _notificationService.SendNotificationFromTemplateAsync(
                        templateType: "UpdateSchedule",
                        recipientIds: new List<Guid> { existingOverride.OriginalUserId },
                        metadata: metadata01,
                        createdBy: "system",
                        notiCategory: "SCHEDULE"
                    );

                    var metadata02 = new Dictionary<string, string>
                    {
                        { "DriverName", existingOverride.OverrideUser.FullName }
                    };

                    await _notificationService.SendNotificationFromTemplateAsync(
                        templateType: "UpdateSchedule",
                        recipientIds: new List<Guid> { existingOverride.OverrideUser.Id },
                        metadata: metadata02,
                        createdBy: "system",
                        notiCategory: "SCHEDULE"
                    );
                    return;
                }

                throw new ErrorException(StatusCodes.Status400BadRequest, ResponseCodeConstants.BADREQUEST,
                    $"Đã tồn tại lịch trình thay thế cho lịch này vào ngày {model.Date:dd/MM/yyyy}. Vui lòng cập nhật bản ghi hiện có thay vì tạo mới.");
            }
            
            var schoolShift = await _schoolShiftRepo.GetByIdAsync(originalSchedule.SchoolShiftId)
                ?? throw new ErrorException(StatusCodes.Status404NotFound, ResponseCodeConstants.NOT_FOUND, "Ca học không tồn tại.");

            var dayOfWeek = model.Date.DayOfWeek.ToString().ToUpper();
            var dayIndex = ConvertDayOfWeekToIndex(dayOfWeek);

            if (originalSchedule.DayOfWeek[dayIndex] != '1')
                throw new ErrorException(StatusCodes.Status400BadRequest, ResponseCodeConstants.BADREQUEST, $"Ngày {model.Date:dd/MM/yyyy} không nằm trong lịch trình gốc.");

            // Kiểm tra tài xế thay thế hợp lệ
            if (model.OverrideUserId.HasValue)
            {
                var overrideDriver = await _userRepo.GetByIdAsync(model.OverrideUserId.Value)
                    ?? throw new ErrorException(StatusCodes.Status404NotFound, ResponseCodeConstants.NOT_FOUND, "Tài xế override không tồn tại.");

                if (overrideDriver.DeletedTime.HasValue)
                    throw new ErrorException(StatusCodes.Status400BadRequest, ResponseCodeConstants.BADREQUEST, $"Tài xế {overrideDriver.FullName} đã bị xóa.");

                if (overrideDriver.Violate == true)
                    throw new ErrorException(StatusCodes.Status400BadRequest, ResponseCodeConstants.BADREQUEST, $"Tài xế {overrideDriver.FullName} đã bị khóa.");

                // Lịch trình hiện tại của tài xế thay thế cùng một loại ca học
                var existingDriverSchedules = await _scheduleRepo.FindAllAsync(x =>
                    x.DriverId == model.OverrideUserId.Value &&
                    !x.DeletedTime.HasValue &&
                    x.From <= model.Date &&
                    x.To >= model.Date &&
                    x.SchoolShift.ShiftType == schoolShift.ShiftType &&
                    x.SchoolShift.SessionType == schoolShift.SessionType);

                // Kiểm tra lịch trình tài xế thay thế có trùng vào ngày này không
                foreach (var existingSchedule in existingDriverSchedules)
                {
                    if (existingSchedule.DayOfWeek[dayIndex] == '1')
                        throw new ErrorException(StatusCodes.Status400BadRequest, ResponseCodeConstants.BADREQUEST,
                            $"Tài xế thay thế đã có lịch trình cho ca {GetSchoolShiftDescription(schoolShift)} vào ngày {model.Date:dd/MM/yyyy} lúc {existingSchedule.DepartureTime}.");
                }

                // Lịch trình thay thế hiện tại của tài xế thay thế cùng một loại ca học
                var existingDriverOverrides = await _scheduleOverrideRepo.FindAllAsync(x =>
                    (x.OverrideUserId == model.OverrideUserId.Value ||
                     (x.Schedule.DriverId == model.OverrideUserId.Value && x.OverrideUserId == null)) &&
                    x.Date == model.Date &&
                    !x.DeletedTime.HasValue &&
                    x.Schedule.SchoolShift.ShiftType == schoolShift.ShiftType &&
                    x.Schedule.SchoolShift.SessionType == schoolShift.SessionType);

                // Kiểm tra lịch trình thay thế của tài xế thay thế có trùng vào ngày này không
                foreach (var existingDriverOverride in existingDriverOverrides)
                {
                    var overrideDayIndex = ConvertDayOfWeekToIndex(model.Date.DayOfWeek.ToString().ToUpper());
                    if (existingDriverOverride.Schedule.DayOfWeek[overrideDayIndex] == '1')
                        throw new ErrorException(StatusCodes.Status400BadRequest, ResponseCodeConstants.BADREQUEST, $"Tài xế thay thế đã có lịch trình thay thế cho ca {GetSchoolShiftDescription(schoolShift)} vào ngày {model.Date:dd/MM/yyyy} lúc {existingDriverOverride.Schedule.DepartureTime}.");
                }
            }

            // Kiểm tra xe thay thế hợp lệ
            if (model.OverrideShuttleId.HasValue)
            {
                var overrideShuttle = await _shuttleRepo.GetByIdAsync(model.OverrideShuttleId.Value)
                    ?? throw new ErrorException(StatusCodes.Status404NotFound, ResponseCodeConstants.NOT_FOUND, "Xe thay thế không tồn tại.");

                if (overrideShuttle.DeletedTime.HasValue)
                    throw new ErrorException(StatusCodes.Status400BadRequest, ResponseCodeConstants.BADREQUEST, $"Xe {overrideShuttle.Name} đã bị xóa.");

                if (!overrideShuttle.IsActive)
                    throw new ErrorException(StatusCodes.Status400BadRequest, ResponseCodeConstants.BADREQUEST, $"Xe {overrideShuttle.Name} trong trạng thái không hoạt động.");

                // Lịch trình hiện tại của xe thay thế cùng một loại ca học
                var existingShuttleSchedules = await _scheduleRepo.FindAllAsync(x =>
                    x.ShuttleId == model.OverrideShuttleId.Value &&
                    !x.DeletedTime.HasValue &&
                    x.From <= model.Date &&
                    x.To >= model.Date &&
                    x.SchoolShift.ShiftType == schoolShift.ShiftType &&
                    x.SchoolShift.SessionType == schoolShift.SessionType);

                // Kiểm tra lịch trình xe thay thế có trùng vào ngày này không
                foreach (var existingSchedule in existingShuttleSchedules)
                {
                    if (existingSchedule.DayOfWeek[dayIndex] == '1')
                        throw new ErrorException(StatusCodes.Status400BadRequest, ResponseCodeConstants.BADREQUEST,
                            $"Xe thay thế đã có lịch trình cho ca {GetSchoolShiftDescription(schoolShift)} vào ngày {model.Date:dd/MM/yyyy} lúc {existingSchedule.DepartureTime}.");
                }

                // Lịch trình thay thế hiện tại của xe thay thế cùng một loại ca học
                var existingShuttleOverrides = await _scheduleOverrideRepo.FindAllAsync(x =>
                    (x.OverrideShuttleId == model.OverrideShuttleId.Value ||
                     (x.Schedule.ShuttleId == model.OverrideShuttleId.Value && x.OverrideShuttleId == null)) &&
                    x.Date == model.Date &&
                    !x.DeletedTime.HasValue &&
                    x.Schedule.SchoolShift.ShiftType == schoolShift.ShiftType &&
                    x.Schedule.SchoolShift.SessionType == schoolShift.SessionType);

                // Kiểm tra lịch trình thay thế của xe thay thế có trùng vào ngày này không
                foreach (var existingShuttleOverride in existingShuttleOverrides)
                {
                    var overrideDayIndex = ConvertDayOfWeekToIndex(model.Date.DayOfWeek.ToString().ToUpper());
                    if (existingShuttleOverride.Schedule.DayOfWeek[overrideDayIndex] == '1')
                        throw new ErrorException(StatusCodes.Status400BadRequest, ResponseCodeConstants.BADREQUEST,
                            $"Xe thay thế đã có lịch thay thế cho ca {GetSchoolShiftDescription(schoolShift)} vào ngày {model.Date:dd/MM/yyyy} lúc {existingShuttleOverride.Schedule.DepartureTime}.");
                }
            }

            var overrideSchedule = _mapper.Map<ScheduleOverride>(model);

            overrideSchedule.OriginalShuttleId = originalSchedule.ShuttleId;
            overrideSchedule.OriginalUserId = originalSchedule.DriverId;
            overrideSchedule.CreatedBy = userId;
            overrideSchedule.LastUpdatedBy = userId;

            await _scheduleOverrideRepo.InsertAsync(overrideSchedule);
            await _unitOfWork.SaveAsync();

            var ids = new[] { overrideSchedule.OriginalUserId, overrideSchedule.OverrideUserId };

            var users = await _unitOfWork.GetRepository<User>()
                .Entities
                .Where(u => ids.Contains(u.Id))
                .ToListAsync();

            // Noti
            foreach (var user in users)
            {
                var metadata1 = new Dictionary<string, string>
                {
                    { "DriverName", user.FullName }
                };

                await _notificationService.SendNotificationFromTemplateAsync(
                    templateType: "UpdateSchedule",
                    recipientIds: new List<Guid> { user.Id },
                    metadata: metadata1,
                    createdBy: "system",
                    notiCategory: "SCHEDULE"
                );
            }
        }

        public async Task UpdateAsync(Guid scheduleOverrideId, UpdateScheduleOverrideModel model)
        {
            var userId = Authentication.GetUserIdFromHttpContextAccessor(_contextAccessor);
            model.TrimAllStrings();

            var existingOverride = await _scheduleOverrideRepo.GetByIdAsync(scheduleOverrideId)
                ?? throw new ErrorException(StatusCodes.Status404NotFound, ResponseCodeConstants.NOT_FOUND, "Lịch trình thay thế không tồn tại.");

            var originalSchedule = await _scheduleRepo.GetByIdAsync(existingOverride.ScheduleId)
                ?? throw new ErrorException(StatusCodes.Status404NotFound, ResponseCodeConstants.NOT_FOUND, "Lịch trình gốc không tồn tại.");

            if (model.OverrideShuttleId == null && model.OverrideUserId == null &&
                model.ShuttleReason == null && model.DriverReason == null)
                throw new ErrorException(StatusCodes.Status400BadRequest, ResponseCodeConstants.BADREQUEST,
                    "Phải chỉ định ít nhất một thay đổi (xe, tài xế hoặc lý do).");

            var schoolShift = await _schoolShiftRepo.GetByIdAsync(originalSchedule.SchoolShiftId)
                ?? throw new ErrorException(StatusCodes.Status404NotFound, ResponseCodeConstants.NOT_FOUND, "Ca học không tồn tại.");

            var dayOfWeek = existingOverride.Date.DayOfWeek.ToString().ToUpper();
            var dayIndex = ConvertDayOfWeekToIndex(dayOfWeek);

            // Kiểm tra tài xế thay thế hợp lệ
            if (model.OverrideUserId.HasValue && model.OverrideUserId != existingOverride.OverrideUserId)
            {
                var overrideDriver = await _userRepo.GetByIdAsync(model.OverrideUserId.Value)
                        ?? throw new ErrorException(StatusCodes.Status404NotFound, ResponseCodeConstants.NOT_FOUND, "Tài xế override không tồn tại.");

                if (overrideDriver.DeletedTime.HasValue)
                    throw new ErrorException(StatusCodes.Status400BadRequest, ResponseCodeConstants.BADREQUEST, $"Tài xế {overrideDriver.FullName} đã bị xóa.");

                if (overrideDriver.Violate == true)
                    throw new ErrorException(StatusCodes.Status400BadRequest, ResponseCodeConstants.BADREQUEST, $"Tài xế {overrideDriver.FullName} đã bị khóa.");

                // Lịch trình hiện tại của tài xế thay thế cùng một loại ca học
                var existingDriverSchedules = await _scheduleRepo.FindAllAsync(x =>
                    x.DriverId == model.OverrideUserId.Value &&
                    !x.DeletedTime.HasValue &&
                    x.From <= existingOverride.Date &&
                    x.To >= existingOverride.Date &&
                    x.SchoolShift.ShiftType == schoolShift.ShiftType &&
                    x.SchoolShift.SessionType == schoolShift.SessionType);

                // Kiểm tra lịch trình tài xế thay thế có trùng vào ngày này không
                foreach (var existingSchedule in existingDriverSchedules)
                {
                    if (existingSchedule.DayOfWeek[dayIndex] == '1')
                        throw new ErrorException(StatusCodes.Status400BadRequest, ResponseCodeConstants.BADREQUEST,
                            $"Tài xế thay thế đã có lịch trình cho ca {GetSchoolShiftDescription(schoolShift)} vào ngày {existingOverride.Date:dd/MM/yyyy} lúc {existingSchedule.DepartureTime}.");
                }

                // Lịch trình thay thế hiện tại của tài xế thay thế cùng một loại ca học
                var existingDriverOverrides = await _unitOfWork.GetRepository<ScheduleOverride>().FindAllAsync(x =>
                    x.Id != scheduleOverrideId &&
                    (x.OverrideUserId == model.OverrideUserId.Value ||
                     (x.Schedule.DriverId == model.OverrideUserId.Value && x.OverrideUserId == null)) &&
                    x.Date == existingOverride.Date &&
                    !x.DeletedTime.HasValue &&
                    x.Schedule.SchoolShift.ShiftType == schoolShift.ShiftType &&
                    x.Schedule.SchoolShift.SessionType == schoolShift.SessionType);

                // Kiểm tra lịch trình thay thế của tài xế thay thế có trùng vào ngày này không
                foreach (var existingDriverOverride in existingDriverOverrides)
                {
                    var overrideDayIndex = ConvertDayOfWeekToIndex(existingOverride.Date.DayOfWeek.ToString().ToUpper());
                    if (existingDriverOverride.Schedule.DayOfWeek[overrideDayIndex] == '1')
                        throw new ErrorException(StatusCodes.Status400BadRequest, ResponseCodeConstants.BADREQUEST,
                            $"Tài xế thay thế đã có lịch trình thay thế cho ca {GetSchoolShiftDescription(schoolShift)} vào ngày {existingOverride.Date:dd/MM/yyyy} lúc {existingDriverOverride.Schedule.DepartureTime}.");
                }

                existingOverride.OverrideUserId = model.OverrideUserId;
            }

            // Kiểm tra xe thay thế hợp lệ
            if (model.OverrideShuttleId.HasValue && model.OverrideShuttleId != existingOverride.OverrideShuttleId)
            {
                var overrideShuttle = await _shuttleRepo.GetByIdAsync(model.OverrideShuttleId.Value)
                    ?? throw new ErrorException(StatusCodes.Status404NotFound, ResponseCodeConstants.NOT_FOUND, "Xe thay thế không tồn tại.");

                if (overrideShuttle.DeletedTime.HasValue)
                    throw new ErrorException(StatusCodes.Status400BadRequest, ResponseCodeConstants.BADREQUEST, $"Xe {overrideShuttle.Name} đã bị xóa.");

                if (!overrideShuttle.IsActive)
                    throw new ErrorException(StatusCodes.Status400BadRequest, ResponseCodeConstants.BADREQUEST, $"Xe {overrideShuttle.Name} trong trạng thái không hoạt động.");

                // Lịch trình hiện tại của xe thay thế cùng một loại ca học
                var existingShuttleSchedules = await _scheduleRepo.FindAllAsync(x =>
                    x.ShuttleId == model.OverrideShuttleId.Value &&
                    !x.DeletedTime.HasValue &&
                    x.From <= existingOverride.Date &&
                    x.To >= existingOverride.Date &&
                    x.SchoolShift.ShiftType == schoolShift.ShiftType &&
                    x.SchoolShift.SessionType == schoolShift.SessionType);

                // Kiểm tra lịch trình xe thay thế có trùng vào ngày này không
                foreach (var existingSchedule in existingShuttleSchedules)
                {
                    if (existingSchedule.DayOfWeek[dayIndex] == '1')
                        throw new ErrorException(StatusCodes.Status400BadRequest, ResponseCodeConstants.BADREQUEST,
                            $"Xe thay thế đã có lịch trình cho ca {GetSchoolShiftDescription(schoolShift)} vào ngày {existingOverride.Date:dd/MM/yyyy} lúc {existingSchedule.DepartureTime}.");
                }

                // Lịch trình thay thế hiện tại của xe thay thế cùng một loại ca học
                var existingShuttleOverrides = await _scheduleOverrideRepo.FindAllAsync(x =>
                    x.Id != scheduleOverrideId &&
                    (x.OverrideShuttleId == model.OverrideShuttleId.Value ||
                     (x.Schedule.ShuttleId == model.OverrideShuttleId.Value && x.OverrideShuttleId == null)) &&
                    x.Date == existingOverride.Date &&
                    !x.DeletedTime.HasValue &&
                    x.Schedule.SchoolShift.ShiftType == schoolShift.ShiftType &&
                    x.Schedule.SchoolShift.SessionType == schoolShift.SessionType);

                // Kiểm tra lịch trình thay thế của xe thay thế có trùng vào ngày này không
                foreach (var existingShuttleOverride in existingShuttleOverrides)
                {
                    var overrideDayIndex = ConvertDayOfWeekToIndex(existingOverride.Date.DayOfWeek.ToString().ToUpper());
                    if (existingShuttleOverride.Schedule.DayOfWeek[overrideDayIndex] == '1')
                        throw new ErrorException(StatusCodes.Status400BadRequest, ResponseCodeConstants.BADREQUEST,
                            $"Xe thay thế đã có lịch thay thế cho ca {GetSchoolShiftDescription(schoolShift)} vào ngày {existingOverride.Date:dd/MM/yyyy} lúc {existingShuttleOverride.Schedule.DepartureTime}.");
                }

                existingOverride.OverrideShuttleId = model.OverrideShuttleId;
            }

            if (model.ShuttleReason != null)
                existingOverride.ShuttleReason = model.ShuttleReason;

            if (model.DriverReason != null)
                existingOverride.DriverReason = model.DriverReason;

            existingOverride.LastUpdatedBy = userId;
            existingOverride.LastUpdatedTime = CoreHelper.SystemTimeNow;

            _scheduleOverrideRepo.Update(existingOverride);
            await _unitOfWork.SaveAsync();
            
            // Gửi thông báo cho tài xế phù hợp
            // Nếu vừa thay tài xế (OverrideUserId thay đổi), gửi cho cả 2 tài xế
            if (model.OverrideUserId.HasValue && model.OverrideUserId != existingOverride.OriginalUserId)
            {
                // Lấy lại thông tin user nếu cần
                var overrideUser = existingOverride.OverrideUser;
                var originalUser = existingOverride.OriginalUser;

                if (originalUser != null)
                {
                    var metadataOriginal = new Dictionary<string, string>
                    {
                        { "DriverName", originalUser.FullName }
                    };
                    await _notificationService.SendNotificationFromTemplateAsync(
                        templateType: "UpdateSchedule",
                        recipientIds: new List<Guid> { originalUser.Id },
                        metadata: metadataOriginal,
                        createdBy: "system",
                        notiCategory: "SCHEDULE"
                    );
                }

                if (overrideUser != null)
                {
                    var metadataOverride = new Dictionary<string, string>
                    {
                        { "DriverName", overrideUser.FullName }
                    };
                    await _notificationService.SendNotificationFromTemplateAsync(
                        templateType: "UpdateSchedule",
                        recipientIds: new List<Guid> { overrideUser.Id },
                        metadata: metadataOverride,
                        createdBy: "system",
                        notiCategory: "SCHEDULE"
                    );
                }
            }
            else if (existingOverride.OverrideUserId != null && existingOverride.OverrideUser != null)
            {
                var metadata = new Dictionary<string, string>
                {
                    { "DriverName", existingOverride.OverrideUser.FullName }
                };

                await _notificationService.SendNotificationFromTemplateAsync(
                    templateType: "UpdateSchedule",
                    recipientIds: new List<Guid> { existingOverride.OverrideUser.Id },
                    metadata: metadata,
                    createdBy: "system",
                    notiCategory: "SCHEDULE"
                );
            }
            else if (existingOverride.OriginalUser != null)
            {
                var metadata = new Dictionary<string, string>
                {
                    { "DriverName", existingOverride.OriginalUser.FullName }
                };

                await _notificationService.SendNotificationFromTemplateAsync(
                    templateType: "UpdateSchedule",
                    recipientIds: new List<Guid> { existingOverride.OriginalUser.Id },
                    metadata: metadata,
                    createdBy: "system",
                    notiCategory: "SCHEDULE"
                );
            }
        }

        public async Task DeleteAsync(Guid scheduleOverrideId, DeleteScheduleOverrideModel model)
        {
            var userId = Authentication.GetUserIdFromHttpContextAccessor(_contextAccessor);
            model.TrimAllStrings();

            var scheduleOverride = await _scheduleOverrideRepo.Entities
                .Include(x => x.OriginalUser)
                .Include(x => x.OverrideUser)
                .FirstOrDefaultAsync(x => x.Id == scheduleOverrideId)
                ?? throw new ErrorException(StatusCodes.Status404NotFound, ResponseCodeConstants.NOT_FOUND, "Lịch trình thay thế không tồn tại.");

            // Lưu lại thông tin tài xế thay thế trước khi xóa
            var overrideUser = scheduleOverride.OverrideUser;
            var originalUser = scheduleOverride.OriginalUser;

            bool notifyBothDrivers = false;
            bool notifyOverrideDriver = false;
            bool notifyOriginalDriver = false;

            if (model.OverrideShuttleId == null && model.OverrideUserId == null)
            {
                scheduleOverride.LastUpdatedBy = userId;
                scheduleOverride.LastUpdatedTime = CoreHelper.SystemTimeNow;
                scheduleOverride.DeletedBy = userId;
                scheduleOverride.DeletedTime = CoreHelper.SystemTimeNow;

                _scheduleOverrideRepo.Update(scheduleOverride);
                await _unitOfWork.SaveAsync();

                notifyBothDrivers = true;
            }
            else
            {
                if (model.OverrideShuttleId != null && scheduleOverride.OverrideShuttleId != model.OverrideShuttleId)
                {
                    throw new ErrorException(StatusCodes.Status400BadRequest, ResponseCodeConstants.BADREQUEST,
                        $"ID xe thay thế không khớp.");
                }

                if (model.OverrideUserId != null && scheduleOverride.OverrideUserId != model.OverrideUserId)
                {
                    throw new ErrorException(StatusCodes.Status400BadRequest, ResponseCodeConstants.BADREQUEST,
                        $"ID tài xế thay thế không khớp.");
                }

                if (model.OverrideShuttleId != null)
                {
                    scheduleOverride.OverrideShuttleId = null;
                    scheduleOverride.ShuttleReason = null;
                    // Gỡ xe: thông báo cho tài xế thay thế, nếu không có thì cho tài xế gốc
                    notifyOverrideDriver = true;
                }

                if (model.OverrideUserId != null)
                {
                    scheduleOverride.OverrideUserId = null;
                    scheduleOverride.DriverReason = null;
                    // Gỡ người: thông báo cho cả 2 tài xế
                    notifyBothDrivers = true;
                }

                bool shouldDeleteWholeRecordFlag = scheduleOverride.OverrideShuttleId == null
                                                   && scheduleOverride.OverrideUserId == null;

                if (shouldDeleteWholeRecordFlag)
                {
                    scheduleOverride.LastUpdatedBy = userId;
                    scheduleOverride.LastUpdatedTime = CoreHelper.SystemTimeNow;
                    scheduleOverride.DeletedBy = userId;
                    scheduleOverride.DeletedTime = CoreHelper.SystemTimeNow;
                }
                else
                {
                    scheduleOverride.LastUpdatedBy = userId;
                    scheduleOverride.LastUpdatedTime = CoreHelper.SystemTimeNow;
                }

                _scheduleOverrideRepo.Update(scheduleOverride);
                await _unitOfWork.SaveAsync();
            }

            // Gửi thông báo
            if (notifyBothDrivers)
            {
                // Gửi cho cả tài xế gốc và tài xế thay thế (nếu có)
                if (originalUser != null)
                {
                    var metadata = new Dictionary<string, string>
                    {
                        { "DriverName", originalUser.FullName }
                    };

                    await _notificationService.SendNotificationFromTemplateAsync(
                        templateType: "UpdateSchedule",
                        recipientIds: new List<Guid> { originalUser.Id },
                        metadata: metadata,
                        createdBy: "system",
                        notiCategory: "SCHEDULE"
                    );
                }
                if (overrideUser != null)
                {
                    var metadata = new Dictionary<string, string>
                    {
                        { "DriverName", overrideUser.FullName }
                    };

                    await _notificationService.SendNotificationFromTemplateAsync(
                        templateType: "UpdateSchedule",
                        recipientIds: new List<Guid> { overrideUser.Id },
                        metadata: metadata,
                        createdBy: "system",
                        notiCategory: "SCHEDULE"
                    );
                }
            }
            else if (notifyOverrideDriver)
            {
                // Gỡ xe: gửi cho tài xế thay thế, nếu không có thì gửi cho tài xế gốc
                if (overrideUser != null)
                {
                    var metadata = new Dictionary<string, string>
                    {
                        { "DriverName", overrideUser.FullName }
                    };

                    await _notificationService.SendNotificationFromTemplateAsync(
                        templateType: "UpdateSchedule",
                        recipientIds: new List<Guid> { overrideUser.Id },
                        metadata: metadata,
                        createdBy: "system",
                        notiCategory: "SCHEDULE"
                    );
                }
                else if (originalUser != null)
                {
                    var metadata = new Dictionary<string, string>
                    {
                        { "DriverName", originalUser.FullName }
                    };

                    await _notificationService.SendNotificationFromTemplateAsync(
                        templateType: "UpdateSchedule",
                        recipientIds: new List<Guid> { originalUser.Id },
                        metadata: metadata,
                        createdBy: "system",
                        notiCategory: "SCHEDULE"
                    );
                }
            }
        }

        #region Private Methods
        private int ConvertDayOfWeekToIndex(string day)
        {
            return day.ToLower() switch
            {
                "monday" => 0,
                "tuesday" => 1,
                "wednesday" => 2,
                "thursday" => 3,
                "friday" => 4,
                "saturday" => 5,
                "sunday" => 6,
                _ => throw new ErrorException(StatusCodes.Status400BadRequest, ResponseCodeConstants.BADREQUEST, $"Thứ không hợp lệ: {day}")
            };
        }

        private string GetSchoolShiftDescription(SchoolShift schoolShift)
        {
            string shiftType = schoolShift.ShiftType switch
            {
                ShiftTypeEnum.START => "vào học",
                ShiftTypeEnum.END => "tan học",
                _ => "Không xác định"
            };

            string sessionType = schoolShift.SessionType switch
            {
                SessionTypeEnum.MORNING => "sáng",
                SessionTypeEnum.AFTERNOON => "chiều",
                _ => "Không xác định"
            };

            return $"{shiftType} buổi {sessionType}";
        }

        #endregion
    }
}
