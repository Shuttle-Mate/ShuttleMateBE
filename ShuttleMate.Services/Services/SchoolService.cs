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
        public async Task<BasePaginatedList<ListStudentInSchoolResponse>> GetAllStudentInSchoolForAdmin(int page = 0, int pageSize = 10, string? search = null, bool sortAsc = false, Guid? schoolShiftId = null, Guid? schoolId = null)
        {

            if (schoolId == null)
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
                .Where(x => x.UserRoles.FirstOrDefault()!.Role.Name.ToUpper() == "STUDENT"
                            && x.SchoolId == schoolId
                            && !x.DeletedTime.HasValue);

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
                    x.UserSchoolShifts.Any(x => x.SchoolShiftId == schoolShiftId) && x.HistoryTickets.Any(x => x.ValidUntil >= DateOnly.FromDateTime(DateTime.Now)));
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

        public async Task<BasePaginatedList<RouteToSchoolResponseModel>> GetAllRouteToSchool(int page = 0, int pageSize = 10, string? search = null,  bool sortAsc = false, Guid ? schoolId = null)
        {
            var query = _unitOfWork.GetRepository<Route>()
                .GetQueryable()
                .Include(x => x.School)
                .Where(x => x.IsActive == true 
                        && x.SchoolId == schoolId
                        && !x.DeletedTime.HasValue);

            if (!string.IsNullOrWhiteSpace(search))
            {
                var lowered = search.Trim().ToLower();
                query = query.Where(x =>
                    x.RouteCode.ToLower().Contains(lowered) ||
                    x.RouteName.ToLower().Contains(lowered) ||
                    x.InBound.ToLower().Contains(lowered) ||
                    x.OutBound.ToLower().Contains(lowered));
            }

            query = sortAsc
                ? query.OrderBy(x => x.CreatedTime)
                : query.OrderByDescending(x => x.CreatedTime);

            var totalCount = await query.CountAsync();

            var pagedItems = await query
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
                .Skip(page * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new BasePaginatedList<RouteToSchoolResponseModel>(pagedItems, totalCount, page, pageSize);
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
        public async Task UpdateSchool(Guid id, UpdateSchoolModel model)
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
            var school = await _unitOfWork.GetRepository<School>().Entities.FirstOrDefaultAsync(x=>x.Id == id && !x.DeletedTime.HasValue) ?? throw new ErrorException(StatusCodes.Status404NotFound, ResponseCodeConstants.NOT_FOUND, "Không tìm thấy trường!");
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
        public async Task DeleteSchool (Guid schoolId)
        {
            var school = await _unitOfWork.GetRepository<School>().Entities.FirstOrDefaultAsync(x => x.Id == schoolId && !x.DeletedTime.HasValue) ?? throw new ErrorException(StatusCodes.Status404NotFound, ResponseCodeConstants.NOT_FOUND, "Không tìm thấy trường!");

            school.DeletedTime = DateTime.Now;
            school.LastUpdatedTime = DateTime.Now;

            await _unitOfWork.GetRepository<School>().UpdateAsync(school);
            await _unitOfWork.SaveAsync();
        }
        public async Task SendEmailToSchool(SendEmailToSchoolModel model)
        {
            var school = await _unitOfWork.GetRepository<School>().Entities.FirstOrDefaultAsync(x=>x.Id == model.SchoolId) ?? throw new ErrorException(StatusCodes.Status404NotFound, ResponseCodeConstants.NOT_FOUND, "Không tìm thấy trường!");
            switch (model.Type)
            {
                case "SCHOOL_SHIFT":
                    await SendCreateStudentSessionsEmail(school.Email);
                    break;
                case "SCHOOL_INFOR":
                    await SendSchoolTermUpdateEmail(school.Email);
                    break;
                default:
                    throw new ErrorException(StatusCodes.Status404NotFound, ResponseCodeConstants.NOT_FOUND, "Vui lòng chọn đúng loại!");
            }
        }
        private async Task SendSchoolTermUpdateEmail(string schoolEmail)
        {
            await _emailService.SendEmailAsync(
                schoolEmail,
                "Yêu Cầu Cập Nhật Thời Gian Học Kỳ",
                $@"
        <html>
        <head>
            <style>
                body, p, h1, h2, h3, ul {{
                    margin: 0;
                    padding: 0;
                }}
                
                body {{
                    font-family: 'Arial', sans-serif;
                    line-height: 1.6;
                    color: #333333;
                    background-color: #FAF9F7;
                    padding: 20px 0;
                }}
                
                .email-container {{
                    max-width: 600px;
                    margin: 0 auto;
                    background: #ffffff;
                    border-radius: 8px;
                    overflow: hidden;
                    box-shadow: 0 4px 12px rgba(0, 0, 0, 0.1);
                }}
                
                .email-header {{
                    background-color: #124DA3;
                    padding: 25px 30px;
                    color: white;
                    text-align: center;
                }}
                
                .email-header h2 {{
                    font-size: 22px;
                    font-weight: 600;
                    margin-bottom: 10px;
                }}
                
                .email-content {{
                    padding: 30px;
                }}
                
                .greeting {{
                    margin-bottom: 20px;
                    font-size: 16px;
                }}
                
                .section {{
                    margin-bottom: 25px;
                }}
                
                .section h3 {{
                    color: #124DA3;
                    font-size: 18px;
                    margin-bottom: 15px;
                    padding-bottom: 8px;
                    border-bottom: 1px solid #eaeaea;
                }}
                
                .section p {{
                    margin-bottom: 15px;
                    font-size: 15px;
                }}
                
                .section ul {{
                    margin: 15px 0 15px 20px;
                }}
                
                .section li {{
                    margin-bottom: 8px;
                    font-size: 15px;
                }}
                
                .cta-button {{
                    display: inline-block;
                    background-color: #F37022;
                    color: white !important;
                    padding: 12px 25px;
                    text-decoration: none;
                    border-radius: 4px;
                    font-weight: 500;
                    margin: 15px 0;
                    text-align: center;
                }}
                
                .email-footer {{
                    background-color: #FAF9F7;
                    padding: 20px 30px;
                    text-align: center;
                    font-size: 14px;
                    color: #666666;
                }}
                
                .contact-info {{
                    margin-top: 15px;
                }}
                
                .contact-info p {{
                    margin-bottom: 5px;
                }}
                
                .logo {{
                    font-weight: bold;
                    color: #124DA3;
                    font-size: 16px;
                    margin-bottom: 10px;
                }}
                
                a {{
                    color: #124DA3;
                    text-decoration: underline;
                }}
                
                @media only screen and (max-width: 600px) {{
                    .email-container {{
                        width: 100%;
                        border-radius: 0;
                    }}
                    
                    .email-header, .email-content, .email-footer {{
                        padding: 20px;
                    }}
                }}
            </style>
        </head>
        <body>
            <div class='email-container'>
                <div class='email-header'>
                    <h2>YÊU CẦU CẬP NHẬT THỜI GIAN HỌC KỲ</h2>
                </div>
                
                <div class='email-content'>
                    <div class='greeting'>
                        <p>Kính gửi Ban Giám hiệu Nhà trường,</p>
                    </div>
                    
                    <div class='section'>
                        <p>Hệ thống ShuttleMate trân trọng gửi đến Quý trường yêu cầu cập nhật thông tin thời gian học kỳ để phục vụ công tác quản lý và vận hành tuyến xe.</p>
                    </div>
                    
                    <div class='section'>
                        <h3>NỘI DUNG YÊU CẦU</h3>
                        <p>Quý trường vui lòng cập nhật đầy đủ thông tin về:</p>
                        <ul>
                            <li>Thời gian bắt đầu và kết thúc Học kỳ 1</li>
                            <li>Thời gian bắt đầu và kết thúc Học kỳ 2</li>
                        </ul>
                        <p>Thông tin này sẽ giúp chúng tôi lên kế hoạch vận hành tuyến xe phù hợp với lịch học của nhà trường.</p>
                    </div>
                    
                    <div class='section'>
                        <h3>HƯỚNG DẪN THỰC HIỆN</h3>
                        <p>Vui lòng đăng nhập vào hệ thống quản lý và cập nhật thông tin tại mục <strong>Cài đặt học kỳ</strong>:</p>
                        <a href='https://admin.shuttlemate.fun/login' class='cta-button'>TRUY CẬP HỆ THỐNG</a>
                        <p>Hoặc truy cập đường dẫn: <a href='https://admin.shuttlemate.fun/login'>https://admin.shuttlemate.fun/login</a></p>
                    </div>
                </div>
                
                <div class='email-footer'>
                    <div class='logo'>SHUTTLEMATE</div>
                    <p>Nếu Quý trường cần hỗ trợ, vui lòng liên hệ:</p>
                    <div class='contact-info'>
                        <p>Email: <a href='mailto:shuttlemate.service@gmail.com'>shuttlemate.service@gmail.com</a></p>
                    </div>
                    <p style='margin-top: 15px;'>Trân trọng cảm ơn sự hợp tác của Quý trường!</p>
                </div>
            </div>
        </body>
        </html>"
            );
        }

        private async Task SendCreateStudentSessionsEmail( string schoolEmail)
        {
            await _emailService.SendEmailAsync(
                schoolEmail,
                "Yêu Cầu Tạo Ca Học Cho Học Sinh",
                $@"
        <html>
        <head>
            <style>
                body, p, h1, h2, h3, ul {{
                    margin: 0;
                    padding: 0;
                }}
                
                body {{
                    font-family: 'Arial', sans-serif;
                    line-height: 1.6;
                    color: #333333;
                    background-color: #FAF9F7;
                    padding: 20px 0;
                }}
                
                .email-container {{
                    max-width: 600px;
                    margin: 0 auto;
                    background: #ffffff;
                    border-radius: 8px;
                    overflow: hidden;
                    box-shadow: 0 4px 12px rgba(0, 0, 0, 0.1);
                }}
                
                .email-header {{
                    background-color: #124DA3;
                    padding: 25px 30px;
                    color: white;
                    text-align: center;
                }}
                
                .email-header h2 {{
                    font-size: 22px;
                    font-weight: 600;
                    margin-bottom: 10px;
                }}
                
                .email-content {{
                    padding: 30px;
                }}
                
                .greeting {{
                    margin-bottom: 20px;
                    font-size: 16px;
                }}
                
                .section {{
                    margin-bottom: 25px;
                }}
                
                .section h3 {{
                    color: #124DA3;
                    font-size: 18px;
                    margin-bottom: 15px;
                    padding-bottom: 8px;
                    border-bottom: 1px solid #eaeaea;
                }}
                
                .section p {{
                    margin-bottom: 15px;
                    font-size: 15px;
                }}
                
                .section ul {{
                    margin: 15px 0 15px 20px;
                }}
                
                .section li {{
                    margin-bottom: 8px;
                    font-size: 15px;
                }}
                
                .cta-button {{
                    display: inline-block;
                    background-color: #F37022;
                    color: white !important;
                    padding: 12px 25px;
                    text-decoration: none;
                    border-radius: 4px;
                    font-weight: 500;
                    margin: 15px 0;
                    text-align: center;
                }}
                
                .email-footer {{
                    background-color: #FAF9F7;
                    padding: 20px 30px;
                    text-align: center;
                    font-size: 14px;
                    color: #666666;
                }}
                
                .contact-info {{
                    margin-top: 15px;
                }}
                
                .contact-info p {{
                    margin-bottom: 5px;
                }}
                
                .logo {{
                    font-weight: bold;
                    color: #124DA3;
                    font-size: 16px;
                    margin-bottom: 10px;
                }}
                
                a {{
                    color: #124DA3;
                    text-decoration: underline;
                }}
                
                @media only screen and (max-width: 600px) {{
                    .email-container {{
                        width: 100%;
                        border-radius: 0;
                    }}
                    
                    .email-header, .email-content, .email-footer {{
                        padding: 20px;
                    }}
                }}
            </style>
        </head>
        <body>
            <div class='email-container'>
                <div class='email-header'>
                    <h2>YÊU CẦU TẠO CA HỌC CHO HỌC SINH</h2>
                </div>
                
                <div class='email-content'>
                    <div class='greeting'>
                        <p>Kính gửi Ban Giám hiệu Nhà trường,</p>
                    </div>
                    
                    <div class='section'>
                        <p>Hệ thống ShuttleMate trân trọng gửi đến Quý trường yêu cầu tạo các ca học cho học sinh để phục vụ công tác quản lý và sắp xếp lịch trình xe đưa đón.</p>
                    </div>
                    
                    <div class='section'>
                        <h3>NỘI DUNG YÊU CẦU</h3>
                        <p>Quý trường vui lòng tạo đầy đủ các ca học cho học sinh bao gồm:</p>
                        <ul>
                            <li>Thời gian bắt đầu và kết thúc các ca học</li>
                        </ul>
                        <p>Thông tin này sẽ giúp chúng tôi sắp xếp lịch trình xe đưa đón phù hợp với nhu cầu của nhà trường và phụ huynh học sinh.</p>
                    </div>
                    
                    <div class='section'>
                        <h3>HƯỚNG DẪN THỰC HIỆN</h3>
                        <p>Vui lòng đăng nhập vào hệ thống quản lý và thực hiện tạo ca học tại mục <strong>Quản lý ca học</strong>:</p>
                        <a href='https://admin.shuttlemate.fun/login' class='cta-button'>TRUY CẬP HỆ THỐNG</a>
                        <p>Hoặc truy cập đường dẫn: <a href='https://admin.shuttlemate.fun/login'>https://admin.shuttlemate.fun/login</a></p>
                    </div>
                </div>
                
                <div class='email-footer'>
                    <div class='logo'>SHUTTLEMATE</div>
                    <p>Nếu Quý trường cần hỗ trợ, vui lòng liên hệ:</p>
                    <div class='contact-info'>
                        <p>Email: <a href='mailto:shuttlemate.service@gmail.com'>shuttlemate.service@gmail.com</a></p>
                    </div>
                    <p style='margin-top: 15px;'>Trân trọng cảm ơn sự hợp tác của Quý trường!</p>
                </div>
            </div>
        </body>
        </html>"
            );
        }
    }
}
