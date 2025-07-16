using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
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
        private readonly IMapper _mapper;
        private readonly IConfiguration _configuration;
        private readonly IHttpContextAccessor _contextAccessor;
        private readonly IEmailService _emailService;

        public SchoolService(IUnitOfWork unitOfWork, IMapper mapper, IConfiguration configuration, IHttpContextAccessor contextAccessor, IEmailService emailService)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _configuration = configuration;
            _contextAccessor = contextAccessor;
            _emailService = emailService;
        }
        public async Task CreateSchool(CreateSchoolModel model)
        {
            if (model.StartSemOne > model.EndSemOne)
            {
                throw new ErrorException(StatusCodes.Status400BadRequest, ResponseCodeConstants.BADREQUEST, "Thời gian bắt đầu kì 1 không được bé hơn thời gian kết thúc kì 1!");
            }
            if (model.StartSemTwo > model.EndSemTwo)
            {
                throw new ErrorException(StatusCodes.Status400BadRequest, ResponseCodeConstants.BADREQUEST, "Thời gian bắt đầu kì 2 không được bé hơn thời gian kết thúc kì 2!!");
            }
            if (model.EndSemOne > model.StartSemTwo)
            {
                throw new ErrorException(StatusCodes.Status400BadRequest, ResponseCodeConstants.BADREQUEST, "Thời gian kết thúc kì 1 không được bé hơn thời gian bắt đầu kì 2!!");
            }
            var school = new School
            {
                Id = Guid.NewGuid(),
                StartSemTwo = model.StartSemTwo,
                EndSemOne = model.EndSemOne,
                EndSemTwo = model.EndSemTwo,
                StartSemOne = model.StartSemOne,
                Address = model.Address,
                Email = model.Email,    
                IsActive = true,
                Name = model.Name,
                PhoneNumber = model.PhoneNumber,
                SchoolTime = model.SchoolTime,
                CreatedTime = DateTime.Now,
                LastUpdatedTime = DateTime.Now,
            };
            await _unitOfWork.GetRepository<School>().InsertAsync(school);
            await _unitOfWork.SaveAsync();
        }
    }
}
