using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using ShuttleMate.Contract.Repositories.Entities;
using ShuttleMate.Contract.Repositories.IUOW;
using ShuttleMate.Contract.Services.Interfaces;
using ShuttleMate.Core.Bases;
using ShuttleMate.Core.Constants;
using ShuttleMate.ModelViews.HistoryTicketModelView;
using ShuttleMate.ModelViews.SchoolModelView;
using ShuttleMate.ModelViews.SchoolShiftModelViews;
using ShuttleMate.ModelViews.StopModelViews;
using ShuttleMate.Services.Services.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
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

        public async Task<BasePaginatedList<SchoolResponseModel>> GetAllAsync(int page = 0, int pageSize = 10, string? search = null, bool? isActive = null, bool sortAsc = false)
        {
            var query = _unitOfWork.GetRepository<School>()
                .GetQueryable()
                .Include(x => x.Routes)
                .Include(x => x.Students)
                .Where(x => !x.DeletedTime.HasValue);

            if (!string.IsNullOrWhiteSpace(search))
            {
                var lowered = search.Trim().ToLower();
                query = query.Where(x =>
                    x.Name.ToLower().Contains(lowered) ||
                    x.Address.ToLower().Contains(lowered)||
                    x.PhoneNumber.ToLower().Contains(lowered)||
                    x.Email.ToLower().Contains(lowered));
            }

            if (isActive != null)
            {
                query = query.Where(x => x.IsActive == isActive);
            }

            query = sortAsc
                ? query.OrderBy(x => x.CreatedTime)
                : query.OrderByDescending(x => x.CreatedTime);

            var totalCount = await query.CountAsync();

            var pagedItems = await query
                .Skip(page * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var result = _mapper.Map<List<SchoolResponseModel>>(pagedItems);

            return new BasePaginatedList<SchoolResponseModel>(result, totalCount, page, pageSize);
        }
        public async Task<BasePaginatedList<ListStudentInSchoolResponse>> GetAllStudentInSchool(int page = 0, int pageSize = 10, string? search = null,  bool sortAsc = false, Guid? schoolShiftId = null)
        {
            // Lấy userId từ HttpContext
            string userId = Authentication.GetUserIdFromHttpContextAccessor(_contextAccessor);

            Guid.TryParse(userId, out Guid cb);

            var user = await _unitOfWork.GetRepository<User>().Entities.FirstOrDefaultAsync(x => x.Id == cb && !x.DeletedTime.HasValue);

            if (user == null || user.School == null || user.SchoolId == null)
            {
                var emptyList = Enumerable.Empty<ListStudentInSchoolResponse>();
                var paginatedList = new BasePaginatedList<ListStudentInSchoolResponse>(
                    emptyList.ToList(), // Danh sách rỗng
                    0,                  // Total count = 0
                    page,
                    pageSize
                );
                return paginatedList;
            }

            var query = _unitOfWork.GetRepository<User>()
                .GetQueryable()
                .Include(x => x.School)
                .Where(x =>  x.UserRoles.FirstOrDefault()!.Role.Name.ToUpper() == "STUDENT" 
                            && x.SchoolId == user.SchoolId
                            &&!x.DeletedTime.HasValue);

            if (!string.IsNullOrWhiteSpace(search))
            {
                var lowered = search.Trim().ToLower();
                query = query.Where(x =>
                    x.FullName.ToLower().Contains(lowered) ||
                    x.Address.ToLower().Contains(lowered) ||
                    x.PhoneNumber.ToLower().Contains(lowered) ||
                    x.Email.ToLower().Contains(lowered));
            }

            if (schoolShiftId != null)
            {
                query = query.Where(x =>
                    x.UserSchoolShifts.Any(x => x.SchoolShiftId == schoolShiftId) && x.HistoryTickets.Any(x =>x.ValidUntil >= DateOnly.FromDateTime(DateTime.Now)));
            }

            //if (routeId != null)
            //{
            //    query = query.Where(x =>
            //        x.HistoryTickets.Any(x=>x.Ticket.RouteId == routeId && x.ValidUntil >= DateOnly.FromDateTime(DateTime.Now)));
            //}

            query = sortAsc
                ? query.OrderBy(x => x.CreatedTime)
                : query.OrderByDescending(x => x.CreatedTime);

            var totalCount = await query.CountAsync();

            var pagedItems = await query
                .Skip(page * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var result = _mapper.Map<List<ListStudentInSchoolResponse>>(pagedItems);

            return new BasePaginatedList<ListStudentInSchoolResponse>(result, totalCount, page, pageSize);
        }

        public async Task<BasePaginatedList<RouteToSchoolResponseModel>> GetAllRouteToSchool(int page = 0, int pageSize = 10, string? search = null, bool? isActive = null, bool sortAsc = false)
        {
            var query = _unitOfWork.GetRepository<Route>()
                .GetQueryable()
                .Include(x => x.School)
                .Where(x => x.IsActive == true && !x.DeletedTime.HasValue);

            if (!string.IsNullOrWhiteSpace(search))
            {
                var lowered = search.Trim().ToLower();
                query = query.Where(x =>
                    x.RouteCode.ToLower().Contains(lowered) ||
                    x.RouteName.ToLower().Contains(lowered) ||
                    x.InBound.ToLower().Contains(lowered) ||
                    x.OutBound.ToLower().Contains(lowered));
            }

            if (isActive != null)
            {
                query = query.Where(x => x.IsActive == isActive);
            }

            query = sortAsc
                ? query.OrderBy(x => x.CreatedTime)
                : query.OrderByDescending(x => x.CreatedTime);

            var totalCount = await query.CountAsync();

            var pagedItems = await query
                .Skip(page * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var routes = await query
                .Select(u => new RouteToSchoolResponseModel
                {
                    OutBound = u.OutBound,
                    RouteName = u.RouteName,
                    RouteCode = u.RouteCode,
                    InBound = u.InBound,
                    AmountOfTrip = u.AmountOfTrip,
                    Description = u.Description,
                    OperatingTime = u.OperatingTime,
                    Price = u.Price,
                    RunningTime = u.RunningTime,
                    SchoolId = u.SchoolId,
                    SchoolName = u.School.Name,
                    TotalDistance = u.TotalDistance,                 
                })
                .ToListAsync();

            return new BasePaginatedList<RouteToSchoolResponseModel>(routes, totalCount, page, pageSize);
        }
        public async Task AssignSchoolForManager(AssignSchoolForManagerModel model)
        {
            var school = await _unitOfWork.GetRepository<School>().Entities.FirstOrDefaultAsync(x => x.Id == model.SchoolId && !x.DeletedTime.HasValue) ?? throw new ErrorException(StatusCodes.Status404NotFound, ResponseCodeConstants.NOT_FOUND, "Không tìm thấy trường!");
            var user = await _unitOfWork.GetRepository<User>().Entities.FirstOrDefaultAsync(x => x.Id == model.UserId && !x.DeletedTime.HasValue) ?? throw new ErrorException(StatusCodes.Status404NotFound, ResponseCodeConstants.NOT_FOUND, "Không tìm thấy người dùng!");
            if (user.UserRoles.FirstOrDefault()!.Role.Name.ToUpper() != "SCHOOL") {
                throw new ErrorException(StatusCodes.Status404NotFound, ResponseCodeConstants.NOT_FOUND, "Người dùng không đúng role school!");
            }

            user.SchoolId = school.Id;

            await _unitOfWork.GetRepository<User>().UpdateAsync(user);
            await _unitOfWork.SaveAsync();

        }
        public async Task<SchoolResponseModel> GetById(Guid id)
        {
            var school = await _unitOfWork.GetRepository<School>().Entities.FirstOrDefaultAsync(x => x.Id == id && !x.DeletedTime.HasValue) ?? throw new ErrorException(StatusCodes.Status404NotFound, ResponseCodeConstants.NOT_FOUND, "Không tìm thấy trường!");
            if (school.IsActive == false)
            {
                throw new ErrorException(StatusCodes.Status400BadRequest, ResponseCodeConstants.BADREQUEST, "Trường đã bị khóa!");
            }
            return _mapper.Map<SchoolResponseModel>(school);
        }

        public async Task CreateSchool(CreateSchoolModel model)
        {
            if (model.StartSemOne > model.EndSemOne)
            {
                throw new ErrorException(StatusCodes.Status400BadRequest, ResponseCodeConstants.BADREQUEST, "Thời gian bắt đầu kì 1 không được bé hơn thời gian kết thúc kì 1!");
            }
            if (model.StartSemTwo > model.EndSemTwo)
            {
                throw new ErrorException(StatusCodes.Status400BadRequest, ResponseCodeConstants.BADREQUEST, "Thời gian bắt đầu kì 2 không được bé hơn thời gian kết thúc kì 2!");
            }
            if (model.EndSemOne > model.StartSemTwo)
            {
                throw new ErrorException(StatusCodes.Status400BadRequest, ResponseCodeConstants.BADREQUEST, "Thời gian kết thúc kì 1 không được bé hơn thời gian bắt đầu kì 2!");
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
                CreatedTime = DateTime.Now,
                LastUpdatedTime = DateTime.Now,
            };
            await _unitOfWork.GetRepository<School>().InsertAsync(school);
            await _unitOfWork.SaveAsync();
        }
        public async Task UpdateSchool(UpdateSchoolModel model)
        {
            if (model.StartSemOne > model.EndSemOne)
            {
                throw new ErrorException(StatusCodes.Status400BadRequest, ResponseCodeConstants.BADREQUEST, "Thời gian bắt đầu kì 1 không được bé hơn thời gian kết thúc kì 1!");
            }
            if (model.StartSemTwo > model.EndSemTwo)
            {
                throw new ErrorException(StatusCodes.Status400BadRequest, ResponseCodeConstants.BADREQUEST, "Thời gian bắt đầu kì 2 không được bé hơn thời gian kết thúc kì 2!");
            }
            if (model.EndSemOne > model.StartSemTwo)
            {
                throw new ErrorException(StatusCodes.Status400BadRequest, ResponseCodeConstants.BADREQUEST, "Thời gian kết thúc kì 1 không được bé hơn thời gian bắt đầu kì 2!");
            }
            var school = await _unitOfWork.GetRepository<School>().Entities.FirstOrDefaultAsync(x=>x.Id == model.Id && !x.DeletedTime.HasValue) ?? throw new ErrorException(StatusCodes.Status404NotFound, ResponseCodeConstants.NOT_FOUND, "Không tìm thấy trường!");
            if (school.IsActive == false)
            {
                throw new ErrorException(StatusCodes.Status400BadRequest, ResponseCodeConstants.BADREQUEST, "Trường đã bị khóa!");
            }

            school.Name = model.Name;
            school.Address = model.Address;
            school.PhoneNumber = model.PhoneNumber;
            school.Email = model.Email;
            school.Email =  model.Email;
            school.StartSemOne = model.StartSemOne;
            school.EndSemOne = model.EndSemOne;
            school.StartSemTwo = model.StartSemTwo;
            school.EndSemTwo = model.EndSemTwo;
            school.LastUpdatedTime = DateTime.Now;

            await _unitOfWork.GetRepository<School>().UpdateAsync(school);
            await _unitOfWork.SaveAsync();
        }
        public async Task DeleteSchool (DeleteSchoolModel model)
        {
            var school = await _unitOfWork.GetRepository<School>().Entities.FirstOrDefaultAsync(x => x.Id == model.Id && !x.DeletedTime.HasValue) ?? throw new ErrorException(StatusCodes.Status404NotFound, ResponseCodeConstants.NOT_FOUND, "Không tìm thấy trường!");

            school.DeletedTime = DateTime.Now;
            school.LastUpdatedTime = DateTime.Now;

            await _unitOfWork.GetRepository<School>().UpdateAsync(school);
            await _unitOfWork.SaveAsync();
        }
    }
}
