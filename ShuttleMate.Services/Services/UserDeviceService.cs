using AutoMapper;
using Microsoft.AspNetCore.Http;
using ShuttleMate.Contract.Repositories.Entities;
using ShuttleMate.Contract.Repositories.IUOW;
using ShuttleMate.Contract.Services.Interfaces;
using ShuttleMate.ModelViews.UserDeviceModelView;
using ShuttleMate.Services.Services.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShuttleMate.Services.Services
{
    public class UserDeviceService : IUserDeviceService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IHttpContextAccessor _contextAccessor;


        public UserDeviceService(IUnitOfWork unitOfWork, IMapper mapper, IHttpContextAccessor contextAccessor)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _contextAccessor = contextAccessor;
        }

        public async Task RegisterDeviceToken(UserDeviceModel model)
        {
            var userIdString = Authentication.GetUserIdFromHttpContextAccessor(_contextAccessor);
            Guid.TryParse(userIdString, out Guid userId);

            var existing = await _unitOfWork.GetRepository<UserDevice>().FindAsync(x => x.UserId == userId && x.PushToken == model.PushToken);

            if (existing == null)
            {
                var newDevice = _mapper.Map<UserDevice>(model);

                newDevice.UserId = userId;
                newDevice.IsValid = true;
                newDevice.CreatedBy = userIdString;
                newDevice.LastUpdatedBy = userIdString;

                await _unitOfWork.GetRepository<UserDevice>().InsertAsync(newDevice);
                await _unitOfWork.SaveAsync();
            }
        }
    }
}
