using AutoMapper;
using Microsoft.AspNetCore.Http;
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

        public ScheduleOverrideService(IUnitOfWork unitOfWork, IMapper mapper, IHttpContextAccessor contextAccessor)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _contextAccessor = contextAccessor;
            _scheduleOverrideRepo = _unitOfWork.GetRepository<ScheduleOverride>();
            _scheduleRepo = _unitOfWork.GetRepository<Schedule>();
            _schoolShiftRepo = _unitOfWork.GetRepository<SchoolShift>();
        }

        public async Task CreateAsync(CreateScheduleOverrideModel model)
        {
            var userId = Authentication.GetUserIdFromHttpContextAccessor(_contextAccessor);
            model.TrimAllStrings();

            var originalSchedule = await _scheduleRepo.GetByIdAsync(model.ScheduleId)
                ?? throw new ErrorException(StatusCodes.Status404NotFound, ResponseCodeConstants.NOT_FOUND, "Lịch trình gốc không tồn tại.");

            if (model.Date < originalSchedule.From || model.Date > originalSchedule.To)
                throw new ErrorException(StatusCodes.Status400BadRequest, ResponseCodeConstants.BADREQUEST, $"Ngày thay thế {model.Date:dd/MM/yyyy} phải nằm trong khoảng từ {originalSchedule.From:dd/MM/yyyy} đến {originalSchedule.To:dd/MM/yyyy}.");

            var existingOverride = await _scheduleOverrideRepo.FindAsync(x =>
                x.ScheduleId == model.ScheduleId &&
                x.Date == model.Date &&
                !x.DeletedTime.HasValue);

            if (existingOverride != null)
            {
                if (existingOverride.OverrideUserId != null &&
                    existingOverride.OverrideShuttleId == null &&
                    model.OverrideShuttleId != null &&
                    model.OverrideUserId == null)
                {
                    existingOverride.OverrideShuttleId = model.OverrideShuttleId;
                    existingOverride.ShuttleReason = model.ShuttleReason ?? existingOverride.ShuttleReason;
                    existingOverride.LastUpdatedBy = userId;

                    await _unitOfWork.GetRepository<ScheduleOverride>().UpdateAsync(existingOverride);
                    await _unitOfWork.SaveAsync();
                    return;
                }

                if (existingOverride.OverrideShuttleId != null &&
                    existingOverride.OverrideUserId == null &&
                    model.OverrideUserId != null &&
                    model.OverrideShuttleId == null)
                {
                    existingOverride.OverrideUserId = model.OverrideUserId;
                    existingOverride.DriverReason = model.DriverReason ?? existingOverride.DriverReason;
                    existingOverride.LastUpdatedBy = userId;

                    await _unitOfWork.GetRepository<ScheduleOverride>().UpdateAsync(existingOverride);
                    await _unitOfWork.SaveAsync();
                    return;
                }

                throw new ErrorException(StatusCodes.Status400BadRequest, ResponseCodeConstants.BADREQUEST,
                    $"Đã tồn tại lịch trình thay thế cho lịch này vào ngày {model.Date:dd/MM/yyyy}. Vui lòng cập nhật bản ghi hiện có thay vì tạo mới.");
            }

            if (model.OverrideShuttleId == null && model.OverrideUserId == null)
                throw new ErrorException(StatusCodes.Status400BadRequest, ResponseCodeConstants.BADREQUEST, "Phải chỉ định ít nhất một thay đổi (xe hoặc tài xế).");

            var schoolShift = await _schoolShiftRepo.GetByIdAsync(originalSchedule.SchoolShiftId)
                ?? throw new ErrorException(StatusCodes.Status404NotFound, ResponseCodeConstants.NOT_FOUND, "Ca học không tồn tại.");

            if (model.OverrideShuttleId.HasValue)
            {
                var overrideShuttle = await _unitOfWork.GetRepository<Shuttle>().GetByIdAsync(model.OverrideShuttleId.Value)
                    ?? throw new ErrorException(StatusCodes.Status404NotFound, ResponseCodeConstants.NOT_FOUND, "Xe thay thế không tồn tại.");

                if (overrideShuttle.DeletedTime.HasValue)
                    throw new ErrorException(StatusCodes.Status400BadRequest, ResponseCodeConstants.BADREQUEST, $"Xe {overrideShuttle.Name} đã bị xóa.");

                if (!overrideShuttle.IsActive)
                    throw new ErrorException(StatusCodes.Status400BadRequest, ResponseCodeConstants.BADREQUEST, $"Xe {overrideShuttle.Name} trong trạng thái không hoạt động.");
            }

            if (model.OverrideUserId.HasValue)
            {
                var overrideDriver = await _unitOfWork.GetRepository<User>().GetByIdAsync(model.OverrideUserId.Value)
                    ?? throw new ErrorException(StatusCodes.Status404NotFound, ResponseCodeConstants.NOT_FOUND, "Tài xế override không tồn tại.");

                if (overrideDriver.DeletedTime.HasValue)
                    throw new ErrorException(StatusCodes.Status400BadRequest, ResponseCodeConstants.BADREQUEST, $"Tài xế {overrideDriver.FullName} đã bị xóa.");

                if (overrideDriver.Violate == true)
                    throw new ErrorException(StatusCodes.Status400BadRequest, ResponseCodeConstants.BADREQUEST, $"Tài xế {overrideDriver.FullName} đã bị khóa.");
            }

            var dayOfWeek = model.Date.DayOfWeek.ToString().ToUpper();
            var dayIndex = ConvertDayOfWeekToIndex(dayOfWeek);

            if (originalSchedule.DayOfWeek[dayIndex] != '1')
                throw new ErrorException(StatusCodes.Status400BadRequest, ResponseCodeConstants.BADREQUEST, $"Ngày {model.Date:dd/MM/yyyy} không nằm trong lịch trình gốc.");

            if (model.OverrideUserId.HasValue)
            {
                var existingDriverSchedules = await _unitOfWork.GetRepository<Schedule>().FindAllAsync(x =>
                    x.DriverId == model.OverrideUserId.Value &&
                    !x.DeletedTime.HasValue &&
                    x.SchoolShiftId == originalSchedule.SchoolShiftId &&
                    x.Direction == originalSchedule.Direction &&
                    x.From <= model.Date &&
                    x.To >= model.Date);

                foreach (var existingSchedule in existingDriverSchedules)
                {
                    if (existingSchedule.DayOfWeek[dayIndex] == '1')
                    {
                        throw new ErrorException(StatusCodes.Status400BadRequest, ResponseCodeConstants.BADREQUEST,
                            $"Tài xế thay thế đã có ca {GetSchoolShiftDescription(schoolShift)} vào ngày {model.Date:dd/MM/yyyy} lúc {existingSchedule.DepartureTime}.");
                    }
                }

                var existingDriverOverrides = await _unitOfWork.GetRepository<ScheduleOverride>().FindAllAsync(x =>
                    (x.OverrideUserId == model.OverrideUserId.Value ||
                     (x.Schedule.DriverId == model.OverrideUserId.Value && x.OverrideUserId == null)) &&
                    x.Date == model.Date &&
                    !x.DeletedTime.HasValue &&
                    x.Schedule.SchoolShiftId == originalSchedule.SchoolShiftId &&
                    x.Schedule.Direction == originalSchedule.Direction);

                foreach (var existingDriverOverride in existingDriverOverrides)
                {
                    var overrideDayIndex = ConvertDayOfWeekToIndex(model.Date.DayOfWeek.ToString().ToUpper());
                    if (existingDriverOverride.Schedule.DayOfWeek[overrideDayIndex] == '1')
                        throw new ErrorException(StatusCodes.Status400BadRequest, ResponseCodeConstants.BADREQUEST, $"Tài xế thay thế đã có lịch override vào ngày {model.Date:dd/MM/yyyy} lúc {existingDriverOverride.Schedule.DepartureTime}.");
                }
            }

            if (model.OverrideShuttleId.HasValue)
            {
                var existingShuttleSchedules = await _unitOfWork.GetRepository<Schedule>().FindAllAsync(x =>
                    x.ShuttleId == model.OverrideShuttleId.Value &&
                    !x.DeletedTime.HasValue &&
                    x.SchoolShiftId == originalSchedule.SchoolShiftId &&
                    x.Direction == originalSchedule.Direction &&
                    x.From <= model.Date &&
                    x.To >= model.Date);

                foreach (var existingSchedule in existingShuttleSchedules)
                {
                    if (existingSchedule.DayOfWeek[dayIndex] == '1')
                    {
                        throw new ErrorException(StatusCodes.Status400BadRequest, ResponseCodeConstants.BADREQUEST,
                            $"Xe thay thế đã có ca {GetSchoolShiftDescription(schoolShift)} vào ngày {model.Date:dd/MM/yyyy} lúc {existingSchedule.DepartureTime}.");
                    }
                }

                var existingShuttleOverrides = await _unitOfWork.GetRepository<ScheduleOverride>().FindAllAsync(x =>
                    (x.OverrideShuttleId == model.OverrideShuttleId.Value ||
                     (x.Schedule.ShuttleId == model.OverrideShuttleId.Value && x.OverrideShuttleId == null)) &&
                    x.Date == model.Date &&
                    !x.DeletedTime.HasValue &&
                    x.Schedule.SchoolShiftId == originalSchedule.SchoolShiftId &&
                    x.Schedule.Direction == originalSchedule.Direction);

                foreach (var existingShuttleOverride in existingShuttleOverrides)
                {
                    var overrideDayIndex = ConvertDayOfWeekToIndex(model.Date.DayOfWeek.ToString().ToUpper());
                    if (existingShuttleOverride.Schedule.DayOfWeek[overrideDayIndex] == '1')
                    {
                        throw new ErrorException(StatusCodes.Status400BadRequest, ResponseCodeConstants.BADREQUEST,
                            $"Xe thay thế đã có lịch override vào ngày {model.Date:dd/MM/yyyy} lúc {existingShuttleOverride.Schedule.DepartureTime}.");
                    }
                }
            }

            var overrideSchedule = new ScheduleOverride
            {
                ScheduleId = model.ScheduleId,
                Date = model.Date,
                ShuttleReason = model.ShuttleReason,
                DriverReason = model.DriverReason,
                OriginalShuttleId = originalSchedule.ShuttleId,
                OverrideShuttleId = model.OverrideShuttleId,
                OriginalUserId = originalSchedule.DriverId,
                OverrideUserId = model.OverrideUserId,
                CreatedBy = userId,
                LastUpdatedBy = userId
            };

            await _unitOfWork.GetRepository<ScheduleOverride>().InsertAsync(overrideSchedule);
            await _unitOfWork.SaveAsync();
        }

        public async Task UpdateAsync(Guid scheduleOverrideId, UpdateScheduleOverrideModel model)
        {
            var userId = Authentication.GetUserIdFromHttpContextAccessor(_contextAccessor);
            model.TrimAllStrings();

            var existingOverride = await _unitOfWork.GetRepository<ScheduleOverride>().GetByIdAsync(scheduleOverrideId)
                ?? throw new ErrorException(StatusCodes.Status404NotFound, ResponseCodeConstants.NOT_FOUND, "Lịch trình thay thế không tồn tại.");

            var originalSchedule = await _unitOfWork.GetRepository<Schedule>().GetByIdAsync(existingOverride.ScheduleId)
                ?? throw new ErrorException(StatusCodes.Status404NotFound, ResponseCodeConstants.NOT_FOUND, "Lịch trình gốc không tồn tại.");

            if (model.OverrideShuttleId == null && model.OverrideUserId == null &&
                model.ShuttleReason == null && model.DriverReason == null)
                throw new ErrorException(StatusCodes.Status400BadRequest, ResponseCodeConstants.BADREQUEST,
                    "Phải chỉ định ít nhất một thay đổi (xe, tài xế hoặc lý do).");

            var schoolShift = await _unitOfWork.GetRepository<SchoolShift>().GetByIdAsync(originalSchedule.SchoolShiftId)
                ?? throw new ErrorException(StatusCodes.Status404NotFound, ResponseCodeConstants.NOT_FOUND, "Ca học không tồn tại.");

            if (model.OverrideShuttleId.HasValue)
            {
                if (model.OverrideShuttleId != existingOverride.OverrideShuttleId)
                {
                    var overrideShuttle = await _unitOfWork.GetRepository<Shuttle>().GetByIdAsync(model.OverrideShuttleId.Value)
                        ?? throw new ErrorException(StatusCodes.Status404NotFound, ResponseCodeConstants.NOT_FOUND, "Xe thay thế không tồn tại.");

                    if (overrideShuttle.DeletedTime.HasValue)
                        throw new ErrorException(StatusCodes.Status400BadRequest, ResponseCodeConstants.BADREQUEST, $"Xe {overrideShuttle.Name} đã bị xóa.");

                    if (!overrideShuttle.IsActive)
                        throw new ErrorException(StatusCodes.Status400BadRequest, ResponseCodeConstants.BADREQUEST, $"Xe {overrideShuttle.Name} trong trạng thái không hoạt động.");

                    var existingShuttleSchedules = await _unitOfWork.GetRepository<Schedule>().FindAllAsync(x =>
                        x.ShuttleId == model.OverrideShuttleId.Value &&
                        !x.DeletedTime.HasValue &&
                        x.SchoolShiftId == originalSchedule.SchoolShiftId &&
                        x.Direction == originalSchedule.Direction &&
                        x.From <= existingOverride.Date &&
                        x.To >= existingOverride.Date);

                    var dayOfWeek = existingOverride.Date.DayOfWeek.ToString().ToUpper();
                    var dayIndex = ConvertDayOfWeekToIndex(dayOfWeek);

                    foreach (var existingSchedule in existingShuttleSchedules)
                    {
                        if (existingSchedule.DayOfWeek[dayIndex] == '1')
                        {
                            throw new ErrorException(StatusCodes.Status400BadRequest, ResponseCodeConstants.BADREQUEST,
                                $"Xe thay thế đã có ca {GetSchoolShiftDescription(schoolShift)} vào ngày {existingOverride.Date:dd/MM/yyyy} lúc {existingSchedule.DepartureTime}.");
                        }
                    }

                    var existingShuttleOverrides = await _unitOfWork.GetRepository<ScheduleOverride>().FindAllAsync(x =>
                        x.Id != scheduleOverrideId &&
                        (x.OverrideShuttleId == model.OverrideShuttleId.Value ||
                         (x.Schedule.ShuttleId == model.OverrideShuttleId.Value && x.OverrideShuttleId == null)) &&
                        x.Date == existingOverride.Date &&
                        !x.DeletedTime.HasValue &&
                        x.Schedule.SchoolShiftId == originalSchedule.SchoolShiftId &&
                        x.Schedule.Direction == originalSchedule.Direction);

                    foreach (var existingShuttleOverride in existingShuttleOverrides)
                    {
                        var overrideDayIndex = ConvertDayOfWeekToIndex(existingOverride.Date.DayOfWeek.ToString().ToUpper());
                        if (existingShuttleOverride.Schedule.DayOfWeek[overrideDayIndex] == '1')
                        {
                            throw new ErrorException(StatusCodes.Status400BadRequest, ResponseCodeConstants.BADREQUEST,
                                $"Xe thay thế đã có lịch override vào ngày {existingOverride.Date:dd/MM/yyyy} lúc {existingShuttleOverride.Schedule.DepartureTime}.");
                        }
                    }
                }

                if (existingOverride.OverrideShuttleId != model.OverrideShuttleId)
                {
                    existingOverride.OverrideShuttleId = model.OverrideShuttleId;
                }
            }

            if (model.OverrideUserId.HasValue)
            {
                if (model.OverrideUserId != existingOverride.OverrideUserId)
                {
                    var overrideDriver = await _unitOfWork.GetRepository<User>().GetByIdAsync(model.OverrideUserId.Value)
                        ?? throw new ErrorException(StatusCodes.Status404NotFound, ResponseCodeConstants.NOT_FOUND, "Tài xế override không tồn tại.");

                    if (overrideDriver.DeletedTime.HasValue)
                        throw new ErrorException(StatusCodes.Status400BadRequest, ResponseCodeConstants.BADREQUEST, $"Tài xế {overrideDriver.FullName} đã bị xóa.");

                    if (overrideDriver.Violate == true)
                        throw new ErrorException(StatusCodes.Status400BadRequest, ResponseCodeConstants.BADREQUEST, $"Tài xế {overrideDriver.FullName} đã bị khóa.");

                    var existingDriverSchedules = await _unitOfWork.GetRepository<Schedule>().FindAllAsync(x =>
                        x.DriverId == model.OverrideUserId.Value &&
                        !x.DeletedTime.HasValue &&
                        x.SchoolShiftId == originalSchedule.SchoolShiftId &&
                        x.Direction == originalSchedule.Direction &&
                        x.From <= existingOverride.Date &&
                        x.To >= existingOverride.Date);

                    var dayOfWeek = existingOverride.Date.DayOfWeek.ToString().ToUpper();
                    var dayIndex = ConvertDayOfWeekToIndex(dayOfWeek);

                    foreach (var existingSchedule in existingDriverSchedules)
                    {
                        if (existingSchedule.DayOfWeek[dayIndex] == '1')
                        {
                            throw new ErrorException(StatusCodes.Status400BadRequest, ResponseCodeConstants.BADREQUEST,
                                $"Tài xế thay thế đã có ca {GetSchoolShiftDescription(schoolShift)} vào ngày {existingOverride.Date:dd/MM/yyyy} lúc {existingSchedule.DepartureTime}.");
                        }
                    }

                    var existingDriverOverrides = await _unitOfWork.GetRepository<ScheduleOverride>().FindAllAsync(x =>
                        x.Id != scheduleOverrideId &&
                        (x.OverrideUserId == model.OverrideUserId.Value ||
                         (x.Schedule.DriverId == model.OverrideUserId.Value && x.OverrideUserId == null)) &&
                        x.Date == existingOverride.Date &&
                        !x.DeletedTime.HasValue &&
                        x.Schedule.SchoolShiftId == originalSchedule.SchoolShiftId &&
                        x.Schedule.Direction == originalSchedule.Direction);

                    foreach (var existingDriverOverride in existingDriverOverrides)
                    {
                        var overrideDayIndex = ConvertDayOfWeekToIndex(existingOverride.Date.DayOfWeek.ToString().ToUpper());
                        if (existingDriverOverride.Schedule.DayOfWeek[overrideDayIndex] == '1')
                        {
                            throw new ErrorException(StatusCodes.Status400BadRequest, ResponseCodeConstants.BADREQUEST,
                                $"Tài xế thay thế đã có lịch override vào ngày {existingOverride.Date:dd/MM/yyyy} lúc {existingDriverOverride.Schedule.DepartureTime}.");
                        }
                    }
                }

                if (existingOverride.OverrideUserId != model.OverrideUserId)
                {
                    existingOverride.OverrideUserId = model.OverrideUserId;
                }
            }

            if (model.ShuttleReason != null)
            {
                existingOverride.ShuttleReason = model.ShuttleReason;
            }

            if (model.DriverReason != null)
            {
                existingOverride.DriverReason = model.DriverReason;
            }

            existingOverride.LastUpdatedBy = userId;
            existingOverride.LastUpdatedTime = CoreHelper.SystemTimeNow;

            _unitOfWork.GetRepository<ScheduleOverride>().Update(existingOverride);
            await _unitOfWork.SaveAsync();
        }

        public async Task DeleteAsync(Guid scheduleOverrideId, DeleteScheduleOverrideModel model)
        {
            var userId = Authentication.GetUserIdFromHttpContextAccessor(_contextAccessor);
            model.TrimAllStrings();

            var scheduleOverride = await _unitOfWork.GetRepository<ScheduleOverride>().GetByIdAsync(scheduleOverrideId)
                ?? throw new ErrorException(StatusCodes.Status404NotFound, ResponseCodeConstants.NOT_FOUND, "Lịch trình thay thế không tồn tại.");

            if (model.OverrideShuttleId == null && model.OverrideUserId == null)
            {
                scheduleOverride.LastUpdatedBy = userId;
                scheduleOverride.LastUpdatedTime = CoreHelper.SystemTimeNow;
                scheduleOverride.DeletedBy = userId;
                scheduleOverride.DeletedTime = CoreHelper.SystemTimeNow;

                _unitOfWork.GetRepository<ScheduleOverride>().Update(scheduleOverride);
                await _unitOfWork.SaveAsync();
                return;
            }

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
            }

            if (model.OverrideUserId != null)
            {
                scheduleOverride.OverrideUserId = null;
                scheduleOverride.DriverReason = null;
            }

            bool shouldDeleteWholeRecord = scheduleOverride.OverrideShuttleId == null
                               && scheduleOverride.OverrideUserId == null;

            if (shouldDeleteWholeRecord)
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

            _unitOfWork.GetRepository<ScheduleOverride>().Update(scheduleOverride);
            await _unitOfWork.SaveAsync();
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
