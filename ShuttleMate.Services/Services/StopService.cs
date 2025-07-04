using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using ShuttleMate.Contract.Repositories.Entities;
using ShuttleMate.Contract.Repositories.IUOW;
using ShuttleMate.Contract.Services.Interfaces;
using ShuttleMate.Core.Bases;
using ShuttleMate.Core.Constants;
using ShuttleMate.ModelViews.StopModelViews;
using ShuttleMate.Services.Services.Infrastructure;

namespace ShuttleMate.Services.Services
{
    public class StopService : IStopService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IHttpContextAccessor _contextAccessor;

        public StopService(IUnitOfWork unitOfWork, IMapper mapper, IHttpContextAccessor contextAccessor)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _contextAccessor = contextAccessor;
        }

        public async Task CreateStop(StopModel model)
        {
            string userId = Authentication.GetUserIdFromHttpContextAccessor(_contextAccessor);

            Stop stop = await _unitOfWork.GetRepository<Stop>().Entities.FirstOrDefaultAsync(x => x.Name == model.Name);
            if (stop != null)
            {
                throw new ErrorException(StatusCodes.Status400BadRequest, ErrorCode.BadRequest, "Trùng tên/quận hoặc trạm dừng này đã tồn tại!!");
            }
            var newStop = _mapper.Map<Stop>(model);
            newStop.CreatedBy = userId;
            newStop.LastUpdatedBy = userId;
            await _unitOfWork.GetRepository<Stop>().InsertAsync(newStop);
            await _unitOfWork.SaveAsync();
        }

        public async Task DeleteStop(Guid stopId)
        {
            string userId = Authentication.GetUserIdFromHttpContextAccessor(_contextAccessor);

            var stop = await _unitOfWork.GetRepository<Stop>().Entities.FirstOrDefaultAsync(x => x.Id == stopId && !x.DeletedTime.HasValue) ?? throw new ErrorException(StatusCodes.Status404NotFound, ErrorCode.NotFound, "Không tìm thấy trạm!");
            stop.DeletedTime = DateTime.Now;
            stop.DeletedBy = userId;
            await _unitOfWork.GetRepository<Stop>().UpdateAsync(stop);
            await _unitOfWork.SaveAsync();
        }

        public async Task<List<ResponseStopModel>> GetAll()
        {
            var stops = await _unitOfWork.GetRepository<Stop>().Entities.Where(x => !x.DeletedTime.HasValue).OrderBy(x => x.Ward).ToListAsync();
            if (!stops.Any())
            {
                throw new ErrorException(StatusCodes.Status404NotFound, ErrorCode.NotFound, "Không có trạm dừng nào tồn tại!");
            }
            return _mapper.Map<List<ResponseStopModel>>(stops);
        }

        public async Task<ResponseStopModel> GetById(Guid stopId)
        {
            var stop = await _unitOfWork.GetRepository<Stop>().Entities.FirstOrDefaultAsync(x => x.Id == stopId && !x.DeletedTime.HasValue) ?? throw new ErrorException(StatusCodes.Status404NotFound, ErrorCode.NotFound, "Không tìm thấy trạm!");

            return _mapper.Map<ResponseStopModel>(stop);
        }

        public async Task UpdateStop(UpdateStopModel model)
        {
            string userId = Authentication.GetUserIdFromHttpContextAccessor(_contextAccessor);

            if (string.IsNullOrWhiteSpace(model.Name) && string.IsNullOrWhiteSpace(model.Ward) && model.Lat == 0 && model.Lng == 0)
            {
                throw new ErrorException(StatusCodes.Status404NotFound, ErrorCode.NotFound, "Tên trạm, quận và tọa độ của trạm không được để trống!");
            }
            var stop = await _unitOfWork.GetRepository<Stop>().Entities.FirstOrDefaultAsync(x => x.Id == model.Id && !x.DeletedTime.HasValue) ?? throw new ErrorException(StatusCodes.Status404NotFound, ErrorCode.NotFound, "Không tìm thấy trạm!");

            _mapper.Map(model, stop);
            stop.LastUpdatedBy = userId;
            stop.LastUpdatedTime = DateTime.Now;
            await _unitOfWork.GetRepository<Stop>().UpdateAsync(stop);
            await _unitOfWork.SaveAsync();
        }
    }
}
