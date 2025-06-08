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
using ShuttleMate.ModelViews.RouteModelViews;
using ShuttleMate.ModelViews.ShuttleModelViews;

namespace ShuttleMate.Services.Services
{
    public class ShuttleService : IShuttleService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public ShuttleService(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task CreateShuttle(ShuttleModel model)
        {
            Shuttle shuttle = await _unitOfWork.GetRepository<Shuttle>().Entities.FirstOrDefaultAsync(x => x.LicensePlate == model.LicensePlate);
            if (shuttle != null)
            {
                throw new ErrorException(StatusCodes.Status400BadRequest, ErrorCode.BadRequest, "Biển số xe này đã tồn tại!!");
            }
            var newShuttle = _mapper.Map<Shuttle>(model);
            await _unitOfWork.GetRepository<Shuttle>().InsertAsync(newShuttle);
            await _unitOfWork.SaveAsync();
        }

        public async Task DeleteShuttle(Guid shuttleId)
        {
            var shuttle = await _unitOfWork.GetRepository<Shuttle>().Entities.FirstOrDefaultAsync(x => x.Id == shuttleId && !x.DeletedTime.HasValue) ?? throw new ErrorException(StatusCodes.Status404NotFound, ErrorCode.NotFound, "Không tìm thấy xe!");
            shuttle.DeletedTime = DateTime.Now;
            await _unitOfWork.GetRepository<Shuttle>().UpdateAsync(shuttle);
            await _unitOfWork.SaveAsync();
        }

        public async Task<List<ResponseShuttleModel>> GetAll()
        {
            var shuttles = await _unitOfWork.GetRepository<Shuttle>().Entities.Where(x => !x.DeletedTime.HasValue).OrderBy(x => x.LicensePlate).ToListAsync();
            if (!shuttles.Any())
            {
                throw new ErrorException(StatusCodes.Status404NotFound, ErrorCode.NotFound, "Không có xe nào tồn tại!");
            }
            return _mapper.Map<List<ResponseShuttleModel>>(shuttles);
        }

        public async Task<ResponseShuttleModel> GetById(Guid shuttleId)
        {
            var shuttle = await _unitOfWork.GetRepository<Shuttle>().Entities.FirstOrDefaultAsync(x => x.Id == shuttleId && !x.DeletedTime.HasValue) ?? throw new ErrorException(StatusCodes.Status404NotFound, ErrorCode.NotFound, "Không tìm thấy tuyến!");

            return _mapper.Map<ResponseShuttleModel>(shuttle);
        }

        public async Task UpdateShuttle(UpdateShuttleModel model)
        {
            if (string.IsNullOrWhiteSpace(model.LicensePlate))
            {
                throw new ErrorException(StatusCodes.Status404NotFound, ErrorCode.NotFound, "Biển số xe không được để trống!");
            }
            var shutle = await _unitOfWork.GetRepository<Shuttle>().Entities.FirstOrDefaultAsync(x => x.Id == model.Id && !x.DeletedTime.HasValue) ?? throw new ErrorException(StatusCodes.Status404NotFound, ErrorCode.NotFound, "Không tìm thấy tuyến!");

            //route = _mapper.Map<Route>(model);
            _mapper.Map(model, shutle);
            await _unitOfWork.GetRepository<Shuttle>().UpdateAsync(shutle);
            await _unitOfWork.SaveAsync();
        }
    }
}
