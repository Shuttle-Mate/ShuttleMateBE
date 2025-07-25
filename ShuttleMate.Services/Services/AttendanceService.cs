using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShuttleMate.Contract.Repositories.Entities;
using ShuttleMate.Contract.Repositories.IUOW;
using ShuttleMate.Contract.Services.Interfaces;
using ShuttleMate.Core.Bases;
using ShuttleMate.Core.Constants;
using ShuttleMate.ModelViews.AttendanceModelViews;
using ShuttleMate.ModelViews.ShuttleModelViews;
using ShuttleMate.Services.Services.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;
using static ShuttleMate.Contract.Repositories.Enum.GeneralEnum;

namespace ShuttleMate.Services.Services
{
    public class AttendanceService : IAttendanceService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IHttpContextAccessor _contextAccessor;

        public AttendanceService(IUnitOfWork unitOfWork, IMapper mapper, IHttpContextAccessor contextAccessor)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _contextAccessor = contextAccessor;
        }

        public async Task CheckIn(CheckInModel model)
        {
            string userId = Authentication.GetUserIdFromHttpContextAccessor(_contextAccessor);

            Attendance attendance = await _unitOfWork.GetRepository<Attendance>().Entities.FirstOrDefaultAsync(x => x.Status == AttendanceStatusEnum.CHECKED_IN);
            if (attendance != null)
            {
                throw new ErrorException(StatusCodes.Status400BadRequest, ErrorCode.BadRequest, "Vé này đã CheckIn nhưng chưa được CheckOut!!");
            }
            
            var checkin = _mapper.Map<Attendance>(model);
            checkin.CheckInTime = DateTime.UtcNow;
            checkin.Status = AttendanceStatusEnum.CHECKED_IN;
            checkin.CreatedBy = userId;
            checkin.LastUpdatedBy = userId;
            //checkin.CheckOutTime = null;
            await _unitOfWork.GetRepository<Attendance>().InsertAsync(checkin);
            await _unitOfWork.SaveAsync();
        }

        public async Task CheckOut(CheckOutModel model)
        {
            string userId = Authentication.GetUserIdFromHttpContextAccessor(_contextAccessor);

            if (string.IsNullOrWhiteSpace(model.CheckOutLocation))
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
            checkout.CheckOutTime = DateTime.UtcNow;
            checkout.LastUpdatedBy = userId;
            checkout.LastUpdatedTime = DateTime.UtcNow;
            await _unitOfWork.GetRepository<Attendance>().UpdateAsync(checkout);
            await _unitOfWork.SaveAsync();
        }

        public async Task DeleteAttendance(Guid attendanceId)
        {
            string userId = Authentication.GetUserIdFromHttpContextAccessor(_contextAccessor);

            var attendance = await _unitOfWork.GetRepository<Attendance>().Entities.FirstOrDefaultAsync(x => x.Id == attendanceId && !x.DeletedTime.HasValue) ?? throw new ErrorException(StatusCodes.Status404NotFound, ErrorCode.NotFound, "Không tìm thấy thông tin điểm danh!");
            attendance.DeletedTime = DateTime.Now;
            attendance.DeletedBy = userId;
            await _unitOfWork.GetRepository<Attendance>().UpdateAsync(attendance);
            await _unitOfWork.SaveAsync();
        }

        public async Task<BasePaginatedList<ResponseAttendanceModel>> GetAll(GetAttendanceQuery req)
        {
            var page = req.page > 0 ? req.page : 0;
            var pageSize = req.pageSize > 0 ? req.pageSize : 10;

            var query = _unitOfWork.GetRepository<Attendance>().Entities
                .Where(x => !x.DeletedTime.HasValue);
            //.OrderBy(x => x.Status);

            // Thêm filter theo tripId nếu có truyền vào
            if (req.tripId.HasValue && req.tripId.Value != Guid.Empty)
            {
                query = query.Where(x => x.TripId == req.tripId.Value);
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

        public async Task BulkCheckOutByTrip(Guid tripId, string checkOutLocation, string? notes = null)
        {
            string userId = Authentication.GetUserIdFromHttpContextAccessor(_contextAccessor);

            // lấy tất cả attendance của trip chưa checkout và chưa bị xóa
            var attendances = await _unitOfWork.GetRepository<Attendance>().Entities
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
                attendance.CheckOutTime = DateTime.UtcNow;
                attendance.CheckOutLocation = checkOutLocation;
                attendance.Notes = notes;
                attendance.LastUpdatedBy = userId;
                attendance.LastUpdatedTime = DateTime.UtcNow;
            }

            await _unitOfWork.GetRepository<Attendance>().UpdateRangeAsync(attendances);
            await _unitOfWork.SaveAsync();
        }

        //public Task UpdateAttendance(UpdateAttendanceModel model)
        //{
        //    throw new NotImplementedException();
        //}

        //static string ConvertAttendanceStatusToString(AttendanceStatusEnum status)
        //{
        //    return status switch
        //    {
        //        AttendanceStatusEnum.NotCheckedIn => "Chưa Check In",
        //        AttendanceStatusEnum.CheckedIn => "Đã Check In",
        //        AttendanceStatusEnum.CheckedOut => "Đã Check Out",
        //        _ => "Không xác định"
        //    };
        //}
    }
}
