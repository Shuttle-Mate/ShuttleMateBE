using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using ShuttleMate.Contract.Repositories.Entities;
using ShuttleMate.Contract.Repositories.IUOW;
using ShuttleMate.Contract.Services.Interfaces;
using ShuttleMate.Core.Bases;
using ShuttleMate.Core.Constants;
using ShuttleMate.ModelViews.SchoolModelView;
using ShuttleMate.ModelViews.TransactionModelView;
using ShuttleMate.Services.Services.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static ShuttleMate.Contract.Repositories.Enum.GeneralEnum;

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
        public async Task<IEnumerable<SchoolResponseModel>> GetAllAsync(string? name = null, string? adress = null, TimeOnly? schoolTime = null)
        {

            var school = _unitOfWork.GetRepository<School>();

            var query = school.Entities.Where(x => !x.DeletedTime.HasValue)
                .AsQueryable();
            if (!string.IsNullOrWhiteSpace(name))
            {
                query = query.Where(u => u.Name.Contains(name));
            }
            if (schoolTime.HasValue)
            {
                query = query.Where(u => u.SchoolTime >= schoolTime);
            }
            if (!string.IsNullOrWhiteSpace(adress))
            {
                query = query.Where(u => u.Address.Contains(adress));
            }

            var schools = await query
                .Select(u => new SchoolResponseModel
                {
                    Id = u.Id,
                    Address = u.Address,
                    SchoolTime = u.SchoolTime,
                    Name = u.Name,
                })
                .ToListAsync();

            return schools;
        }
        public async Task<SchoolResponseModel> GetById(Guid id)
        {

            var school = await _unitOfWork.GetRepository<School>().Entities.FirstOrDefaultAsync(x => x.Id == id && !x.DeletedTime.HasValue) ?? throw new ErrorException(StatusCodes.Status404NotFound, ErrorCode.NotFound, "Không tìm thấy trường!");

            var responseSchool = new SchoolResponseModel
            {
                Id = id,
                Address = school.Address,
                Name = school.Name,
                SchoolTime = school.SchoolTime  
            };

            return responseSchool;
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
            var school = await _unitOfWork.GetRepository<School>().Entities.FirstOrDefaultAsync(x => x.Id == model.Id && !x.DeletedTime.HasValue) ?? throw new ErrorException(StatusCodes.Status404NotFound, ErrorCode.NotFound, "Không tìm thấy trường!");

            school.Name = model.Name;
            school.Address = model.Address;
            school.SchoolTime = model.SchoolTime;

            await _unitOfWork.GetRepository<School>().UpdateAsync(school);
            await _unitOfWork.SaveAsync();
        }
        public async Task DeleteSchool(DeleteSchoolModel model)
        {

            var school = await _unitOfWork.GetRepository<School>().Entities.FirstOrDefaultAsync(x => x.Id == model.Id && !x.DeletedTime.HasValue) ?? throw new ErrorException(StatusCodes.Status404NotFound, ErrorCode.NotFound, "Không tìm thấy trường!");

            school.DeletedTime = DateTime.Now;

            await _unitOfWork.GetRepository<School>().UpdateAsync(school);
            await _unitOfWork.SaveAsync();
        }
    }
}
