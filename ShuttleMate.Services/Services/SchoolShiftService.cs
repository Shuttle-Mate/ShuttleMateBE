using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using ShuttleMate.Contract.Repositories.Entities;
using ShuttleMate.Contract.Repositories.IUOW;
using ShuttleMate.Contract.Services.Interfaces;
using ShuttleMate.Core.Bases;
using ShuttleMate.Core.Constants;
using ShuttleMate.ModelViews.RoleModelViews;
using ShuttleMate.ModelViews.SchoolShiftModelViews;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static ShuttleMate.Contract.Repositories.Enum.GeneralEnum;

namespace ShuttleMate.Services.Services
{
    public class SchoolShiftService : ISchoolShiftService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IConfiguration _configuration;
        private readonly IHttpContextAccessor _contextAccessor;
        private readonly IEmailService _emailService;

        public SchoolShiftService(IUnitOfWork unitOfWork, IMapper mapper, IConfiguration configuration, IHttpContextAccessor contextAccessor, IEmailService emailService)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _configuration = configuration;
            _contextAccessor = contextAccessor;
            _emailService = emailService;
        }

        public async Task<List<ResponseSchoolShiftListByTicketIdMode>> GetSchoolShiftListByTicketId(Guid ticketId)
        {
            var ticket = await _unitOfWork.GetRepository<Ticket>().Entities.FirstOrDefaultAsync(x => x.Id == ticketId && !x.DeletedTime.HasValue) ?? throw new ErrorException(StatusCodes.Status404NotFound, ResponseCodeConstants.NOT_FOUND, "Không tìm thấy trường!");
            var schoolShift = await _unitOfWork.GetRepository<SchoolShift>().Entities.Where(x=>x.SchoolId == ticket.Route.SchoolId).ToListAsync() ?? throw new ErrorException(StatusCodes.Status404NotFound, ResponseCodeConstants.NOT_FOUND, "Không tìm thấy trường!");
            var list = schoolShift.Select(x => new ResponseSchoolShiftListByTicketIdMode
            {
                Id = x.Id,
                SchoolId = x.SchoolId,
                SchoolName = x.School.Name,
                SessionType = x.SessionType,
                ShiftType = x.ShiftType,
                Time = x.Time,
                
            }).ToList();
            return list;
        }

        public async Task CreateSchoolShift(CreateSchoolShiftModel model)
        {
            var school = await _unitOfWork.GetRepository<School>().Entities.FirstOrDefaultAsync(x => x.Id == model.SchoolId && !x.DeletedTime.HasValue) ?? throw new ErrorException(StatusCodes.Status404NotFound, ResponseCodeConstants.NOT_FOUND, "Không tìm thấy trường!");

            if (school.SchoolShifts.Count(x => x.ShiftType == model.ShiftType && x.SessionType == SessionTypeEnum.MORNING) > 0)
            {
                throw new ErrorException(StatusCodes.Status400BadRequest, ResponseCodeConstants.BADREQUEST, "Loại giờ này đã được tạo!");
            }
            if (school.SchoolShifts.Count(x => x.ShiftType == model.ShiftType && x.SessionType == SessionTypeEnum.AFTERNOON) > 0)
            {
                throw new ErrorException(StatusCodes.Status400BadRequest, ResponseCodeConstants.BADREQUEST, "Loại giờ này đã được tạo!");
            }
            if (school.SchoolShifts.Count(x => x.SessionType == model.SessionType && x.ShiftType == ShiftTypeEnum.START) > 0)
            {
                throw new ErrorException(StatusCodes.Status400BadRequest, ResponseCodeConstants.BADREQUEST, "Loại buổi vào học này đã được tạo!");
            }
            if (school.SchoolShifts.Count(x => x.SessionType == model.SessionType && x.ShiftType == ShiftTypeEnum.END) > 0)
            {
                throw new ErrorException(StatusCodes.Status400BadRequest, ResponseCodeConstants.BADREQUEST, "Loại buổi vào học này đã được tạo!");
            }

            var schoolShift = new SchoolShift
            {
                Id = Guid.NewGuid(),
                SchoolId = school.Id,
                ShiftType = model.ShiftType,
                Time = model.Time,
                SessionType = model.SessionType,
                CreatedTime = DateTime.Now,
                LastUpdatedTime = DateTime.Now,
            };

            await _unitOfWork.GetRepository<SchoolShift>().InsertAsync(schoolShift);
            await _unitOfWork.SaveAsync();

        }
        public async Task UpdateSchoolShift(UpdateSchoolShiftModel model)
        {
            var school = await _unitOfWork.GetRepository<School>().Entities.FirstOrDefaultAsync(x => x.Id == model.SchoolId && !x.DeletedTime.HasValue) ?? throw new ErrorException(StatusCodes.Status404NotFound, ResponseCodeConstants.NOT_FOUND, "Không tìm thấy trường!");
            var schoolShift = await _unitOfWork.GetRepository<SchoolShift>().Entities.FirstOrDefaultAsync(x => x.Id == model.Id && !x.DeletedTime.HasValue) ?? throw new ErrorException(StatusCodes.Status404NotFound, ResponseCodeConstants.NOT_FOUND, "Không tìm thấy ca học!");

            if (schoolShift.ShiftType == model.ShiftType
                && schoolShift.Time == model.Time
                && schoolShift.SchoolId == model.SchoolId
                && schoolShift.SessionType == model.SessionType)
            {
                //bỏ qua cập nhật
            }
            else//tiếp tục cập nhật
            {
                if (school.SchoolShifts.Count(x => x.ShiftType == model.ShiftType && x.SessionType == SessionTypeEnum.MORNING) > 0)
                {
                    throw new ErrorException(StatusCodes.Status400BadRequest, ResponseCodeConstants.BADREQUEST, "Loại giờ này đã được tạo!");
                }
                if (school.SchoolShifts.Count(x => x.ShiftType == model.ShiftType && x.SessionType == SessionTypeEnum.AFTERNOON) > 0)
                {
                    throw new ErrorException(StatusCodes.Status400BadRequest, ResponseCodeConstants.BADREQUEST, "Loại giờ này đã được tạo!");
                }
                if (school.SchoolShifts.Count(x => x.SessionType == model.SessionType && x.ShiftType == ShiftTypeEnum.START) > 0)
                {
                    throw new ErrorException(StatusCodes.Status400BadRequest, ResponseCodeConstants.BADREQUEST, "Loại buổi vào học này đã được tạo!");
                }
                if (school.SchoolShifts.Count(x => x.SessionType == model.SessionType && x.ShiftType == ShiftTypeEnum.END) > 0)
                {
                    throw new ErrorException(StatusCodes.Status400BadRequest, ResponseCodeConstants.BADREQUEST, "Loại buổi vào học này đã được tạo!");
                }

                schoolShift.SchoolId = school.Id;
                schoolShift.ShiftType = model.ShiftType;
                schoolShift.Time = model.Time;
                schoolShift.SessionType = model.SessionType;
                schoolShift.LastUpdatedTime = DateTime.Now;

                await _unitOfWork.GetRepository<SchoolShift>().UpdateAsync(schoolShift);
                await _unitOfWork.SaveAsync();
            }
        }
        public async Task DeleteSchoolShift(DeleteSchoolShiftModel model)
        {
            var schoolShift = await _unitOfWork.GetRepository<SchoolShift>().Entities.FirstOrDefaultAsync(x => x.Id == model.Id && !x.DeletedTime.HasValue) ?? throw new ErrorException(StatusCodes.Status404NotFound, ResponseCodeConstants.NOT_FOUND, "Không tìm thấy ca học!");

            schoolShift.DeletedTime = DateTime.Now;

            await _unitOfWork.GetRepository<SchoolShift>().UpdateAsync(schoolShift);
            await _unitOfWork.SaveAsync();
        }


    }
}

