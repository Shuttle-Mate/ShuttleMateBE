using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Org.BouncyCastle.Pkix;
using ShuttleMate.Contract.Repositories.Entities;
using ShuttleMate.Contract.Repositories.IUOW;
using ShuttleMate.Contract.Services.Interfaces;
using ShuttleMate.Core.Bases;
using ShuttleMate.Core.Constants;
using ShuttleMate.Core.Utils;
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

        public TripService(IUnitOfWork unitOfWork, IMapper mapper, IHttpContextAccessor contextAccessor)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _contextAccessor = contextAccessor;
        }

        //public async Task StartTrip(TripModel model)
        //{
        //    string userId = Authentication.GetUserIdFromHttpContextAccessor(_contextAccessor);

        //    var newTrip = _mapper.Map<Trip>(model);
        //    newTrip.CreatedBy = userId;
        //    newTrip.LastUpdatedBy = userId;
        //    newTrip.EndTime = null; // Assuming EndTime is nullable and not provided in the model
        //    newTrip.Status = TripStatusEnum.IN_PROGESS;
        //    await _unitOfWork.GetRepository<Trip>().InsertAsync(newTrip);
        //    await _unitOfWork.SaveAsync();
        //}

        public async Task StartTrip(TripModel model)
        {
            string currentUserIdString = Authentication.GetUserIdFromHttpContextAccessor(_contextAccessor);

            if (!Guid.TryParse(currentUserIdString, out Guid actualDriverId))
            {
                throw new ErrorException(StatusCodes.Status401Unauthorized, ResponseCodeConstants.UNAUTHORIZED, "Tài xế không hợp lệ");
            }

            DateTime now = DateTime.Now;
            var tripDate = DateOnly.FromDateTime(now);

            // kiểm tra trong ScheduleOverrides
            var scheduleOverrideRepository = _unitOfWork.GetRepository<ScheduleOverride>();
            var overrideRecord = await scheduleOverrideRepository.FindAsync(
                predicate: so => so.ScheduleId == model.ScheduleId &&
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
                    predicate: s => s.Id == model.ScheduleId &&
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

            var newTrip = _mapper.Map<Trip>(model);

            newTrip.CreatedBy = currentUserIdString;
            newTrip.LastUpdatedBy = currentUserIdString;

            newTrip.TripDate = DateOnly.FromDateTime(now);
            newTrip.StartTime = TimeOnly.FromDateTime(now);
            newTrip.EndTime = null; // Assuming EndTime is nullable and not provided in the model
            newTrip.Status = TripStatusEnum.IN_PROGESS;

            // Nếu StartTime trong Trip entity là TimeOnly?, bạn cần đảm bảo TripModel.StartTime cũng là TimeOnly? hoặc bạn cần chuyển đổi.
            // newTrip.StartTime = TimeOnly.FromDateTime(model.StartTime); // Giả sử model.StartTime là DateTime

            await _unitOfWork.GetRepository<Trip>().InsertAsync(newTrip);
            await _unitOfWork.SaveAsync();
        }

        public Task<BasePaginatedList<ResponseTripModel>> GetAllPaging(GetTripQuery req)
        {
            throw new NotImplementedException();
        }

        public Task<ResponseTripModel> GetById(Guid tripId)
        {
            throw new NotImplementedException();
        }

        public Task UpdateTrip(UpdateTripModel model)
        {
            throw new NotImplementedException();
        }
    }
}
