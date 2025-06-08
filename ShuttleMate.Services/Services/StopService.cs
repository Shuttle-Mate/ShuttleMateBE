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

namespace ShuttleMate.Services.Services
{
    public class StopService : IStopService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public StopService(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task CreateStop(StopModel model)
        {
            Stop stop = await _unitOfWork.GetRepository<Stop>().Entities.FirstOrDefaultAsync(x => x.Name == model.Name && x.Ward == model.Ward);
            if (stop != null)
            {
                throw new ErrorException(StatusCodes.Status400BadRequest, ErrorCode.BadRequest, "Trùng tên/quận hoặc trạm dừng này đã tồn tại!!");
            }
            var newStop = _mapper.Map<Stop>(model);
            await _unitOfWork.GetRepository<Stop>().InsertAsync(newStop);
            await _unitOfWork.SaveAsync();
        }

        public async Task DeleteStop(Guid stopId)
        {
            var stop = await _unitOfWork.GetRepository<Stop>().Entities.FirstOrDefaultAsync(x => x.Id == stopId && !x.DeletedTime.HasValue) ?? throw new ErrorException(StatusCodes.Status404NotFound, ErrorCode.NotFound, "Không tìm thấy trạm!");
            stop.DeletedTime = DateTime.Now;
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
            if (string.IsNullOrWhiteSpace(model.Name) && string.IsNullOrWhiteSpace(model.Ward) && model.Lat == 0 && model.Lng == 0)
            {
                throw new ErrorException(StatusCodes.Status404NotFound, ErrorCode.NotFound, "Tên trạm, quận và tọa độ của trạm không được để trống!");
            }
            var stop = await _unitOfWork.GetRepository<Stop>().Entities.FirstOrDefaultAsync(x => x.Id == model.Id && !x.DeletedTime.HasValue) ?? throw new ErrorException(StatusCodes.Status404NotFound, ErrorCode.NotFound, "Không tìm thấy trạm!");

            _mapper.Map(model, stop);
            await _unitOfWork.GetRepository<Stop>().UpdateAsync(stop);
            await _unitOfWork.SaveAsync();
        }
    }
}
