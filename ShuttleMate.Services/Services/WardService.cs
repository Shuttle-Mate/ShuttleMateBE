using AutoMapper;
using Microsoft.AspNetCore.Http;
using ShuttleMate.Contract.Repositories.Entities;
using ShuttleMate.Contract.Repositories.IUOW;
using ShuttleMate.Contract.Services.Interfaces;
using ShuttleMate.Core.Bases;
using ShuttleMate.Core.Constants;
using ShuttleMate.ModelViews.WardModelViews;

namespace ShuttleMate.Services.Services
{
    public class WardService : IWardService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public WardService(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<IEnumerable<ResponseWardModel>> GetAllAsync()
        {
            var wards = await _unitOfWork.GetRepository<Ward>().FindAllAsync(a => !a.DeletedTime.HasValue);

            if (!wards.Any())
            {
                throw new ErrorException(StatusCodes.Status404NotFound, ErrorCode.NotFound, "Không có phường nào.");
            }

            return _mapper.Map<IEnumerable<ResponseWardModel>>(wards);
        }
    }
}
