using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using ShuttleMate.Contract.Repositories.Entities;
using ShuttleMate.Contract.Repositories.IUOW;
using ShuttleMate.Contract.Services.Interfaces;
using ShuttleMate.Core.Bases;
using ShuttleMate.Core.Constants;
using ShuttleMate.ModelViews.SchoolModelView;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShuttleMate.Services.Services
{
    public class SchoolService : ISchoolService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IEmailService _emailService;

        public SchoolService(IUnitOfWork unitOfWork, IEmailService emailService)
        {
            _unitOfWork = unitOfWork;
            _emailService = emailService;
        }
        public async Task CreateSchool(CreateSchoolModel model)
        {
            if (string.IsNullOrWhiteSpace(model.Name))
            {
                throw new ErrorException(StatusCodes.Status400BadRequest, ErrorCode.BadRequest, "Tên trường không được để trống!");
            }
            if (string.IsNullOrWhiteSpace(model.Address))
            {
                throw new ErrorException(StatusCodes.Status400BadRequest, ErrorCode.BadRequest, "Địa chỉ trường không được để trống!");
            }
            if (string.IsNullOrWhiteSpace(model.SchoolTime.ToString()))
            {
                throw new ErrorException(StatusCodes.Status400BadRequest, ErrorCode.BadRequest, "Thời gian trường không được để trống!");
            }
            School school = new School()
            {
                SchoolTime = model.SchoolTime,
                Address = model.Address,
                Name = model.Name,
                Id = Guid.NewGuid(),
                CreatedTime = DateTime.Now,
                LastUpdatedTime = DateTime.Now,
            };
            await _unitOfWork.GetRepository<School>().InsertAsync(school);
            await _unitOfWork.SaveAsync();
        }
        public async Task UpdateSchool(UpdateSchoolModel model)
        {
            if (string.IsNullOrWhiteSpace(model.Name))
            {
                throw new ErrorException(StatusCodes.Status400BadRequest, ErrorCode.BadRequest, "Tên trường không được để trống!");
            }
            if (string.IsNullOrWhiteSpace(model.Address))
            {
                throw new ErrorException(StatusCodes.Status400BadRequest, ErrorCode.BadRequest, "Địa chỉ trường không được để trống!");
            }
            if (string.IsNullOrWhiteSpace(model.SchoolTime.ToString()))
            {
                throw new ErrorException(StatusCodes.Status400BadRequest, ErrorCode.BadRequest, "Thời gian trường không được để trống!");
            }
            var school = await _unitOfWork.GetRepository<School>().Entities.FirstOrDefaultAsync(x => x.Id == model.Id) ?? throw new ErrorException(StatusCodes.Status404NotFound, ErrorCode.NotFound, "Không tìm thấy trường!");

            school.Name = model.Name;
            school.Address = model.Address;
            school.SchoolTime = model.SchoolTime;

            await _unitOfWork.GetRepository<School>().UpdateAsync(school);
            await _unitOfWork.SaveAsync();
        }
        public async Task DeleteSchool(DeleteSchoolModel model)
        {

            var school = await _unitOfWork.GetRepository<School>().Entities.FirstOrDefaultAsync(x => x.Id == model.Id) ?? throw new ErrorException(StatusCodes.Status404NotFound, ErrorCode.NotFound, "Không tìm thấy trường!");

            school.DeletedTime = DateTime.Now;

            await _unitOfWork.GetRepository<School>().UpdateAsync(school);
            await _unitOfWork.SaveAsync();
        }
    }
}
