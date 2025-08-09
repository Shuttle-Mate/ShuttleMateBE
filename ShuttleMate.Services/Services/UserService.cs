using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using ShuttleMate.Contract.Repositories.Entities;
using ShuttleMate.Contract.Repositories.IUOW;
using ShuttleMate.Contract.Services.Interfaces;
using ShuttleMate.Core.Bases;
using ShuttleMate.Core.Constants;
using ShuttleMate.Core.Utils;
using ShuttleMate.ModelViews.AuthModelViews;
using ShuttleMate.ModelViews.SchoolModelView;
using ShuttleMate.ModelViews.SchoolShiftModelViews;
using ShuttleMate.ModelViews.UserModelViews;
using ShuttleMate.Services.Services.Infrastructure;
using System.Numerics;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;
using static ShuttleMate.Contract.Repositories.Enum.GeneralEnum;
using static System.Net.WebRequestMethods;

namespace ShuttleMate.Services.Services
{
    public class UserService : IUserService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IConfiguration _configuration;
        private readonly IHttpContextAccessor _contextAccessor;
        private readonly string _apiKey;
        private readonly IEmailService _emailService;
        private readonly ISupabaseService _supabaseService;

        public UserService(IUnitOfWork unitOfWork, IMapper mapper, IConfiguration configuration, IHttpContextAccessor contextAccessor, IEmailService emailService, ISupabaseService supabaseService)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _configuration = configuration;
            _contextAccessor = contextAccessor;
            _apiKey = configuration["VietMap:ApiKey"] ?? throw new Exception("API key is missing from configuration.");
            _emailService = emailService;
            _supabaseService = supabaseService;
        }

        public async Task<IEnumerable<ReponseYourChild>> GetYourChild(Guid Id)
        {
            var user = _unitOfWork.GetRepository<User>();
            var query = user.Entities.Where(x => x.ParentId == Id && !x.DeletedTime.HasValue).AsQueryable();
            var users = await query
            .Select(u => new ReponseYourChild
            {
                Id = u.Id,
                Gender = u.Gender,
                FullName = u.FullName,
            })
            .ToListAsync();
            return users;
        }
        public async Task RemoveParent()
        {
            // Lấy userId từ HttpContext
            string userId = Authentication.GetUserIdFromHttpContextAccessor(_contextAccessor);

            Guid.TryParse(userId, out Guid cb);

            User user = await _unitOfWork.GetRepository<User>().Entities.FirstOrDefaultAsync(x => x.Id == cb && !x.DeletedTime.HasValue) ?? throw new ErrorException(StatusCodes.Status404NotFound, ErrorCode.NotFound, "Không tìm thấy học sinh!");
            string email = user.Parent.Email;
            user.ParentId = null;

            await _unitOfWork.GetRepository<User>().UpdateAsync(user);
            await _unitOfWork.SaveAsync();
            if (email != null)
            {
                await _emailService.SendEmailAsync(email, "Thông báo từ ShuttleMate", $"Học sinh {user.FullName} đã xóa bạn khỏi vai trò phụ huynh!</div>");
            }
        }
        public async Task RemoveStudent(RemoveStudentModel model)
        {
            //// Lấy userId từ HttpContext
            //string userId = Authentication.GetUserIdFromHttpContextAccessor(_contextAccessor);

            //Guid.TryParse(userId, out Guid cb);

            User user = await _unitOfWork.GetRepository<User>().Entities.FirstOrDefaultAsync(x => x.Id == model.StudentId && !x.DeletedTime.HasValue) ?? throw new ErrorException(StatusCodes.Status404NotFound, ErrorCode.NotFound, "Không tìm thấy học sinh!");
            string email = user.Parent.Email;
            user.ParentId = null;

            await _unitOfWork.GetRepository<User>().UpdateAsync(user);
            await _unitOfWork.SaveAsync();
            if (email != null)
            {
                await _emailService.SendEmailAsync(user.Email, "Thông báo từ ShuttleMate", $"Phụ huynh đã xóa bạn khỏi khỏi danh sách học sinh!</div>");
            }
        }
        public async Task CreateUserAdmin(CreateUserAdminModel model)
        {
            // Kiểm tra user co tồn tại
            var user = await _unitOfWork.GetRepository<User>().Entities
                .FirstOrDefaultAsync(x => x.Email == model.Email && !x.DeletedTime.HasValue);
            var vnTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Ho_Chi_Minh");
            var vietnamNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, vnTimeZone);
            // Kiểm tra xác nhận mật khẩu
            if (user != null)
            {
                throw new ErrorException(StatusCodes.Status400BadRequest, ErrorCode.BadRequest, "Tài khoản đã tồn tại!");
            }
            if (model.Password != model.ConfirmPassword)
            {
                throw new ErrorException(StatusCodes.Status400BadRequest, ErrorCode.BadRequest, "Xác nhận mật khẩu không đúng!");
            }

            // Sử dụng PasswordHasher để băm mật khẩu
            var passwordHasher = new FixedSaltPasswordHasher<User>(Options.Create(new PasswordHasherOptions()));
            User newUser = new User();
            newUser.Id = Guid.NewGuid();
            newUser.FullName = model.Name;
            newUser.Email = model.Email;
            newUser.UserName = model.Email;
            newUser.PhoneNumber = model.PhoneNumber;
            newUser.PasswordHash = passwordHasher.HashPassword(null, model.Password); // Băm mật khẩu tại đây
            newUser.EmailVerified = true;

            switch (model.RoleName)
            {
                case "STUDENT":
                    var role = await _unitOfWork.GetRepository<Role>().Entities
                    .FirstOrDefaultAsync(x => x.Name == "STUDENT");
                    if (role == null)
                    {
                        throw new ErrorException(StatusCodes.Status404NotFound, ErrorCode.NotFound, "Vai trò không tồn tại!");
                    }
                    // Thêm người dùng  vào cơ sở dữ liệu
                    await _unitOfWork.GetRepository<User>().InsertAsync(newUser);

                    UserRole userRole = new UserRole()
                    {
                        UserId = newUser.Id,
                        RoleId = role.Id,
                    };
                    await _unitOfWork.GetRepository<UserRole>().InsertAsync(userRole);
                    break;
                case "PARENT":
                    var roleStudent = await _unitOfWork.GetRepository<Role>().Entities
                    .FirstOrDefaultAsync(x => x.Name == "PARENT");
                    if (roleStudent == null)
                    {
                        throw new ErrorException(StatusCodes.Status404NotFound, ErrorCode.NotFound, "Vai trò không tồn tại!");
                    }
                    // Thêm người dùng  vào cơ sở dữ liệu
                    await _unitOfWork.GetRepository<User>().InsertAsync(newUser);

                    UserRole userRoleStudent = new UserRole()
                    {
                        UserId = newUser.Id,
                        RoleId = roleStudent.Id,
                    };
                    await _unitOfWork.GetRepository<UserRole>().InsertAsync(userRoleStudent);
                    break;
                case "OPERATOR":
                    var roleOperator = await _unitOfWork.GetRepository<Role>().Entities
                    .FirstOrDefaultAsync(x => x.Name == "OPERATOR");
                    if (roleOperator == null)
                    {
                        throw new ErrorException(StatusCodes.Status404NotFound, ErrorCode.NotFound, "Vai trò không tồn tại!");
                    }
                    // Thêm người dùng  vào cơ sở dữ liệu
                    await _unitOfWork.GetRepository<User>().InsertAsync(newUser);

                    UserRole userRoleOperator = new UserRole()
                    {
                        UserId = newUser.Id,
                        RoleId = roleOperator.Id,
                    };
                    await _unitOfWork.GetRepository<UserRole>().InsertAsync(userRoleOperator);
                    break;
                case "DRIVER":
                    var roleDriver = await _unitOfWork.GetRepository<Role>().Entities
                    .FirstOrDefaultAsync(x => x.Name == "DRIVER");
                    if (roleDriver == null)
                    {
                        throw new ErrorException(StatusCodes.Status404NotFound, ErrorCode.NotFound, "Vai trò không tồn tại!");
                    }
                    // Thêm người dùng  vào cơ sở dữ liệu
                    await _unitOfWork.GetRepository<User>().InsertAsync(newUser);

                    UserRole userRoleDriver = new UserRole()
                    {
                        UserId = newUser.Id,
                        RoleId = roleDriver.Id,
                    };
                    await _unitOfWork.GetRepository<UserRole>().InsertAsync(userRoleDriver);
                    break;
                case "SCHOOL":
                    var roleSchool = await _unitOfWork.GetRepository<Role>().Entities
                    .FirstOrDefaultAsync(x => x.Name == "SCHOOL");
                    if (roleSchool == null)
                    {
                        throw new ErrorException(StatusCodes.Status404NotFound, ErrorCode.NotFound, "Vai trò không tồn tại!");
                    }
                    var school = new School
                    {
                        Id = Guid.NewGuid(),
                        Email = model.Email,
                        IsActive = true,
                        Name = model.Name,
                        PhoneNumber = model.PhoneNumber,
                        CreatedTime = vietnamNow,
                        LastUpdatedTime = vietnamNow,
                    };
                    newUser.SchoolId = school.Id;
                    // Thêm người dùng  vào cơ sở dữ liệu
                    await _unitOfWork.GetRepository<School>().InsertAsync(school);
                    await _unitOfWork.GetRepository<User>().InsertAsync(newUser);

                    UserRole userRoleSchool = new UserRole()
                    {
                        UserId = newUser.Id,
                        RoleId = roleSchool.Id,
                    };
                    await _unitOfWork.GetRepository<UserRole>().InsertAsync(userRoleSchool);

                    break;
                default:
                    throw new ErrorException(StatusCodes.Status400BadRequest, ErrorCode.BadRequest, "Vui lòng chọn đúng vai trò!!");

            }

            await _unitOfWork.SaveAsync();

        }
        public async Task AssignParent(Guid studentId, AssignParentModel model)
        {
            var user = await _unitOfWork.GetRepository<User>()
                .Entities.FirstOrDefaultAsync(x => x.Id == studentId && !x.DeletedTime.HasValue)
                ?? throw new ErrorException(StatusCodes.Status404NotFound, ResponseCodeConstants.NOT_FOUND, "Người dùng không tồn tại!");
            var parent = await _unitOfWork.GetRepository<User>()
                .Entities.FirstOrDefaultAsync(x => x.Id == model.ParentId && !x.DeletedTime.HasValue);
            if (parent == null)
            {
                parent = await _unitOfWork.GetRepository<User>()
                .Entities.FirstOrDefaultAsync(x => x.Id == user.ParentId && !x.DeletedTime.HasValue);
                if (parent.UserRoles.FirstOrDefault().Role.Name.ToUpper() != "PARENT")
                {
                    throw new ErrorException(StatusCodes.Status400BadRequest, ResponseCodeConstants.BADREQUEST, "Tài khoản này không phải vai trò phụ huynh!");
                }
                if (parent.UserRoles.FirstOrDefault().Role.Name.ToUpper() != "PARENT")
                {
                    throw new ErrorException(StatusCodes.Status400BadRequest, ResponseCodeConstants.BADREQUEST, "Tài khoản này không phải vai trò phụ huynh!");
                }
                user.ParentId = parent.Id;
                await _unitOfWork.GetRepository<User>().UpdateAsync(user);
                await _unitOfWork.SaveAsync();
            }
            if (parent.UserRoles.FirstOrDefault().Role.Name.ToUpper() != "PARENT")
            {
                throw new ErrorException(StatusCodes.Status400BadRequest, ResponseCodeConstants.BADREQUEST, "Tài khoản này không phải vai trò phụ huynh!");
            }
            user.ParentId = parent.Id;
            await _unitOfWork.GetRepository<User>().UpdateAsync(user);
            await _unitOfWork.SaveAsync();
        }
        public async Task<BasePaginatedList<ResponseStudentInRouteAndShiftModel>> GetStudentInRouteAndShift(int page = 0, int pageSize = 10, Guid? routeId = null, Guid? schoolShiftId = null, string? search = null)
        {
            // Lấy userId từ HttpContext
            string userId = Authentication.GetUserIdFromHttpContextAccessor(_contextAccessor);

            Guid.TryParse(userId, out Guid cb);
            var userRepo = _unitOfWork.GetRepository<User>();

            var vnTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Ho_Chi_Minh");
            var vietnamNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, vnTimeZone);
            var todayVN = DateOnly.FromDateTime(vietnamNow);

            var query = userRepo.Entities
            .Include(u => u.UserSchoolShifts)
                .ThenInclude(uss => uss.SchoolShift)
            .Include(u => u.HistoryTickets)
                .ThenInclude(ht => ht.Attendances)
            .Include(u => u.HistoryTickets)
                .ThenInclude(ht => ht.Ticket)
                    .ThenInclude(t => t.Route)
            .Include(u => u.Parent)
            .Include(u => u.School)
            .Where(u => !u.DeletedTime.HasValue)
            .AsQueryable();


            if (routeId == null)
            {
                throw new ErrorException(StatusCodes.Status404NotFound, ResponseCodeConstants.NOT_FOUND, "Không tìm thấy tuyến!");
            }
            if (schoolShiftId == null)
            {
                throw new ErrorException(StatusCodes.Status404NotFound, ResponseCodeConstants.NOT_FOUND, "Không tìm thấy ca học!");
            }
            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(u => u.FullName.Contains(search));
            }
            //điều kiện hs trong cùng 1 ca học
            query = query.Where(x => x.UserSchoolShifts.Any(x => x.SchoolShiftId == schoolShiftId
            && !x.DeletedTime.HasValue));
            //điều kiện học sinh có vé tuyến đường này và vé còn thời gian hiệu lực
            query = query.Where(x => x.HistoryTickets.Any(y => y.Ticket.RouteId == routeId
            && y.Ticket.Route.IsActive == true
            && y.ValidUntil >= todayVN
            && y.Status == HistoryTicketStatus.PAID
            && !y.DeletedTime.HasValue));

            var totalCount = await query.CountAsync();

            var pagedItems = await query
                .Select(u => new ResponseStudentInRouteAndShiftModel
                {
                    Id = u.Id,
                    FullName = u.FullName,
                    Gender = u.Gender,
                    DateOfBirth = u.DateOfBirth,
                    ProfileImageUrl = _supabaseService.GetPublicUrl(u.ProfileImageUrl),
                    Address = u.Address,
                    Email = u.Email,
                    ParentName = u.Parent.FullName,
                    PhoneNumber = u.PhoneNumber,
                    SchoolName = u.School.Name,
                    HistoryTicketId = u.HistoryTickets.
                    FirstOrDefault(x => x.ValidUntil >= todayVN
                    && x.Ticket.RouteId == routeId
                    && x.Ticket.Route.IsActive == true
                    && x.Status == HistoryTicketStatus.PAID
                    && !x.DeletedTime.HasValue)!.Id,
                    TripId = u.HistoryTickets
                        .SelectMany(h => h.Attendances)
                        .Where(a => DateOnly.FromDateTime(a.CheckInTime) == todayVN)
                        .OrderByDescending(a => a.CheckInTime)
                        .Select(a => (Guid?)a.TripId)
                        .FirstOrDefault(),
                    IsCheckIn = u.HistoryTickets.Any(h => h.Attendances.Any(a => DateOnly.FromDateTime(a.CheckInTime) == todayVN)),
                    IsCheckOut = u.HistoryTickets.Any(h => h.Attendances.Any(a => DateOnly.FromDateTime(a.CheckOutTime) == todayVN)),
                })
                .Skip(page * pageSize)
                .Take(pageSize)
                .ToListAsync();
            return new BasePaginatedList<ResponseStudentInRouteAndShiftModel>(pagedItems, totalCount, page, pageSize);
        }
        public async Task<BasePaginatedList<AdminResponseUserModel>> GetAllAsync(int page = 0, int pageSize = 10, string? name = null, bool? gender = null, string? roleName = null, bool? Violate = null, string? email = null, string? phone = null, Guid? schoolId = null, Guid? parentId = null)
        {
            var userRepo = _unitOfWork.GetRepository<User>();

            var query = userRepo.Entities
        .Include(u => u.UserRoles)
        .ThenInclude(ur => ur.Role)
        .AsQueryable();

            if (!string.IsNullOrWhiteSpace(roleName))
            {
                query = query.Where(u => u.UserRoles.Any(ur => ur.Role.Name.ToUpper().Contains(roleName)));
            }
            if (!string.IsNullOrWhiteSpace(name))
            {
                query = query.Where(u => u.FullName.Contains(name));
            }
            if (gender != null)
            {
                query = query.Where(u => u.Gender == gender);
            }
            if (Violate != null)
            {
                query = query.Where(u => u.Violate == Violate);
            }
            if (!string.IsNullOrWhiteSpace(email))
            {
                query = query.Where(u => u.Email.Contains(email));
            }
            if (!string.IsNullOrWhiteSpace(phone))
            {
                query = query.Where(u => u.PhoneNumber.Contains(phone));
            }
            if (schoolId != null)
            {
                query = query.Where(u => u.SchoolId == schoolId);
            }
            if (parentId != null)
            {
                query = query.Where(u => u.ParentId == parentId);
            }

            var totalCount = await query.CountAsync();

            var pagedItems = await query
                .Select(u => new AdminResponseUserModel
                {
                    Id = u.Id,
                    RoleName = u.UserRoles.Select(ur => ur.Role.Name).FirstOrDefault()!.ToUpper(),
                    FullName = u.FullName,
                    Gender = u.Gender,
                    DateOfBirth = u.DateOfBirth,
                    ProfileImageUrl = _supabaseService.GetPublicUrl(u.ProfileImageUrl),
                    Address = u.Address,
                    EmailVerified = u.EmailVerified,
                    Violate = u.Violate,
                    DeletedTime = u.DeletedTime,
                    Email = u.Email,
                    ParentName = u.Parent.FullName,
                    SchoolName = u.School.Name,
                    PhoneNumber = u.PhoneNumber,
                })
                .Skip(page * pageSize)
                .Take(pageSize)
                .ToListAsync();
            return new BasePaginatedList<AdminResponseUserModel>(pagedItems, totalCount, page, pageSize);
        }
        public async Task UpdateSchoolForUser(Guid? id = null, UpdateSchoolForUserModel? model = null)
        {
            // Lấy userId từ HttpContext
            string userId = Authentication.GetUserIdFromHttpContextAccessor(_contextAccessor);

            Guid.TryParse(userId, out Guid cb);
            var vnTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Ho_Chi_Minh");
            var vietnamNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, vnTimeZone);

            // Check if current time is within allowed period (7PM Sat to 5PM Sun)
            var dayOfWeek = vietnamNow.DayOfWeek;
            var currentTime = vietnamNow.TimeOfDay;

            User admin = await _unitOfWork.GetRepository<User>()
                .Entities.FirstOrDefaultAsync(x => x.Id == cb && !x.DeletedTime.HasValue) ?? throw new ErrorException(StatusCodes.Status404NotFound, ErrorCode.NotFound, "Tài khoản không tồn tại!");

            bool isAllowedTime = false;

            // Saturday case
            if (dayOfWeek == DayOfWeek.Saturday && currentTime >= new TimeSpan(19, 0, 0))
            {
                isAllowedTime = true;
            }
            // Sunday case
            else if (dayOfWeek == DayOfWeek.Sunday && currentTime <= new TimeSpan(17, 0, 0))
            {
                isAllowedTime = true;
            }
            if (!admin.UserRoles.Any(x => x.Role.Name == "ADMIN" || x.Role.Name == "OPERATOR"))
            {
                if (!isAllowedTime)
                {
                    throw new ErrorException(StatusCodes.Status400BadRequest, ErrorCode.BadRequest,
                        "Chỉ có thể cập nhật ca học từ 19h tối thứ 7 đến 17h chiều Chủ nhật hàng tuần");
                }
            }

            if (id != null)
            {
                User user = await _unitOfWork.GetRepository<User>()
                    .Entities.FirstOrDefaultAsync(x => x.Id == id && !x.DeletedTime.HasValue) ?? throw new ErrorException(StatusCodes.Status404NotFound, ErrorCode.NotFound, "Tài khoản không tồn tại!");
                School school = await _unitOfWork.GetRepository<School>().Entities.FirstOrDefaultAsync(x => x.Id == model!.SchoolId && !x.DeletedTime.HasValue) ?? throw new ErrorException(StatusCodes.Status404NotFound, ErrorCode.NotFound, "Không tìm thấy trường!");

                user.SchoolId = school.Id;
                user.LastUpdatedTime = vietnamNow;

                await _unitOfWork.GetRepository<User>().UpdateAsync(user);

                List<Guid> listSchoolShift = new List<Guid>();
                //Check xem đúng hết chưa 
                foreach (var schoolShiftId in model.SchoolShifts!)
                {
                    var schoolShift = await _unitOfWork.GetRepository<SchoolShift>().Entities.FirstOrDefaultAsync(x => x.Id == schoolShiftId && !x.DeletedTime.HasValue) ?? throw new ErrorException(StatusCodes.Status404NotFound, ResponseCodeConstants.NOT_FOUND, "Ca học không tồn tại!");
                    listSchoolShift.Add(schoolShift.Id);
                }
                //xóa userShift
                var userShift = await _unitOfWork.GetRepository<UserSchoolShift>().Entities.Where(x => x.StudentId == user.Id && !x.DeletedTime.HasValue).ToListAsync();
                if (userShift != null)
                {
                    foreach (var del in userShift)
                    {
                        await _unitOfWork.GetRepository<UserSchoolShift>().DeleteAsync(del);
                    }
                    await _unitOfWork.SaveAsync();
                }
                //Add vào 
                foreach (var schoolShiftId in listSchoolShift)
                {
                    var userShiftNew = new UserSchoolShift
                    {
                        Id = Guid.NewGuid(),
                        SchoolShiftId = schoolShiftId,
                        StudentId = user.Id,
                        CreatedTime = vietnamNow,
                        LastUpdatedTime = vietnamNow
                    };
                    await _unitOfWork.GetRepository<UserSchoolShift>().InsertAsync(userShiftNew);
                }
            }
            else
            {
                User user = await _unitOfWork.GetRepository<User>()
                    .Entities.FirstOrDefaultAsync(x => x.Id == cb && !x.DeletedTime.HasValue) ?? throw new ErrorException(StatusCodes.Status404NotFound, ErrorCode.NotFound, "Tài khoản không tồn tại!");
                School school = await _unitOfWork.GetRepository<School>().Entities.FirstOrDefaultAsync(x => x.Id == model!.SchoolId && !x.DeletedTime.HasValue) ?? throw new ErrorException(StatusCodes.Status404NotFound, ErrorCode.NotFound, "Không tìm thấy trường!");

                user.SchoolId = school.Id;
                user.LastUpdatedTime = vietnamNow;

                await _unitOfWork.GetRepository<User>().UpdateAsync(user);

                List<Guid> listSchoolShift = new List<Guid>();
                //Check xem đúng hết chưa 
                foreach (var schoolShiftId in model!.SchoolShifts!)
                {
                    var schoolShift = await _unitOfWork.GetRepository<SchoolShift>().Entities.FirstOrDefaultAsync(x => x.Id == schoolShiftId && !x.DeletedTime.HasValue) ?? throw new ErrorException(StatusCodes.Status404NotFound, ResponseCodeConstants.NOT_FOUND, "Ca học không tồn tại!");
                    listSchoolShift.Add(schoolShift.Id);
                }
                //xóa userShift
                var userShift = await _unitOfWork.GetRepository<UserSchoolShift>().Entities.Where(x => x.StudentId == user.Id && !x.DeletedTime.HasValue).ToListAsync();
                if (userShift != null)
                {
                    foreach (var del in userShift)
                    {
                        await _unitOfWork.GetRepository<UserSchoolShift>().DeleteAsync(del);
                    }
                    await _unitOfWork.SaveAsync();
                }
                //Add vào 
                foreach (var schoolShiftId in listSchoolShift)
                {
                    var userShiftNew = new UserSchoolShift
                    {
                        Id = Guid.NewGuid(),
                        SchoolShiftId = schoolShiftId,
                        StudentId = user.Id,
                        CreatedTime = vietnamNow,
                        LastUpdatedTime = vietnamNow
                    };
                    await _unitOfWork.GetRepository<UserSchoolShift>().InsertAsync(userShiftNew);
                }
            }
            await _unitOfWork.SaveAsync();
        }
        public async Task AssignSchoolForManager(AssignSchoolForManagerModel model)
        {
            var school = await _unitOfWork.GetRepository<School>().Entities.FirstOrDefaultAsync(x => x.Id == model.SchoolId && !x.DeletedTime.HasValue) ?? throw new ErrorException(StatusCodes.Status404NotFound, ResponseCodeConstants.NOT_FOUND, "Không tìm thấy trường!");
            var user = await _unitOfWork.GetRepository<User>().Entities.FirstOrDefaultAsync(x => x.Id == model.UserId && !x.DeletedTime.HasValue) ?? throw new ErrorException(StatusCodes.Status404NotFound, ResponseCodeConstants.NOT_FOUND, "Không tìm thấy người dùng!");
            if (user.UserRoles.FirstOrDefault()!.Role.Name.ToUpper() != "SCHOOL")
            {
                throw new ErrorException(StatusCodes.Status404NotFound, ResponseCodeConstants.NOT_FOUND, "Người dùng không đúng role school!");
            }

            user.SchoolId = school.Id;

            await _unitOfWork.GetRepository<User>().UpdateAsync(user);
            await _unitOfWork.SaveAsync();

        }

        public async Task<UserInforModel> GetInfor()
        {
            // Lấy userId từ HttpContext
            string userId = Authentication.GetUserIdFromHttpContextAccessor(_contextAccessor);

            Guid.TryParse(userId, out Guid cb);

            // Lấy thông tin người dùng
            User? user = await _unitOfWork.GetRepository<User>()
                .Entities.FirstOrDefaultAsync(x => x.Id == cb);

            // Lấy thông tin trường học (nếu có)
            SchoolResponse? schoolResponse = null;

            var school = await _unitOfWork.GetRepository<School>()
                .Entities.Include(x => x.SchoolShifts)
                .FirstOrDefaultAsync(x => x.Id == user.SchoolId);

            if (school != null)
            {
                schoolResponse = new SchoolResponse
                {
                    Id = school.Id,
                    Name = school.Name,
                    Address = school.Address,
                    PhoneNumber = school.PhoneNumber,
                    Email = school.Email,
                    schoolShiftResponses = school.SchoolShifts?.Select(x => new SchoolShiftResponse
                    {
                        Id = x.Id,
                        Time = x.Time,
                        ShiftType = x.ShiftType.ToString().ToUpper(),
                        SessionType = x.SessionType.ToString().ToUpper()
                    }).ToList()!
                };
            }

            // Lấy thông tin parent (nếu có)
            ParentResponse? parentResponse = null;
            if (user!.ParentId != null)
            {
                var parent = await _unitOfWork.GetRepository<User>()
                    .Entities.FirstOrDefaultAsync(x => x.Id == user.ParentId);
                if (parent != null)
                {
                    parentResponse = new ParentResponse
                    {
                        Id = parent.Id,
                        Address = parent.Address,
                        DateOfBirth = parent.DateOfBirth,
                        Email = parent.Email,
                        FullName = parent.FullName,
                        Gender = parent.Gender,
                        PhoneNumber = parent.PhoneNumber,
                        ProfileImageUrl = parent.ProfileImageUrl!
                    };
                }
            }
            // Lấy danh sách children (nếu có)
            var children = new List<ChildResponse>();

            var childUsers = await _unitOfWork.GetRepository<User>()
                .Entities.Where(x => x.ParentId == user.Id).ToListAsync();
            if (childUsers != null)
            {
                foreach (var child in childUsers)
                {

                    // Lấy thông tin trường học (nếu có)
                    SchoolResponse? childSchoolResponse = null;

                    var childSchool = await _unitOfWork.GetRepository<School>()
                        .Entities.Include(x => x.SchoolShifts)
                        .FirstOrDefaultAsync(x => x.Id == child.SchoolId);

                    if (childSchool != null)
                    {
                        childSchoolResponse = new SchoolResponse
                        {
                            Id = childSchool.Id,
                            Name = childSchool.Name,
                            Address = childSchool.Address,
                            PhoneNumber = childSchool.PhoneNumber,
                            Email = childSchool.Email,
                            schoolShiftResponses = childSchool.SchoolShifts?.Select(x => new SchoolShiftResponse
                            {
                                Id = x.Id,
                                Time = x.Time,
                                ShiftType = x.ShiftType.ToString().ToUpper(),
                                SessionType = x.SessionType.ToString().ToUpper()
                            }).ToList()!
                        };
                    }
                    children.Add(new ChildResponse
                    {
                        Id = child.Id,
                        Address = child.Address,
                        DateOfBirth = child.DateOfBirth,
                        Email = child.Email,
                        FullName = child.FullName,
                        Gender = child.Gender,
                        PhoneNumber = child.PhoneNumber,
                        ProfileImageUrl = child.ProfileImageUrl!,
                        School = childSchoolResponse
                    });
                }
            }


            UserInforModel inforModel = new UserInforModel
            {
                Id = user.Id,
                Address = user.Address,
                DateOfBirth = user.DateOfBirth,
                Email = user.Email,
                FullName = user.FullName,
                Gender = user.Gender,
                PhoneNumber = user.PhoneNumber,
                ProfileImageUrl = user.ProfileImageUrl!,
                Parent = parentResponse,
                Childs = children,
                School = schoolResponse!,
                RoleName = user.UserRoles.FirstOrDefault(x => !x.Role.DeletedTime.HasValue)!.Role.Name.ToUpper(),
            };
            return inforModel;
        }
        public async Task<UserInforModel> GetById(Guid userId)
        {

            // Lấy thông tin người dùng
            User user = await _unitOfWork.GetRepository<User>()
                .Entities.FirstOrDefaultAsync(x => x.Id == userId) ?? throw new ErrorException(StatusCodes.Status404NotFound, ResponseCodeConstants.NOT_FOUND, "Người dùng không tồn tại!");

            // Lấy thông tin trường học (nếu có)
            SchoolResponse? schoolResponse = null;

            var school = await _unitOfWork.GetRepository<School>()
                .Entities.Include(x => x.SchoolShifts)
                .FirstOrDefaultAsync(x => x.Id == user.SchoolId);

            if (school != null)
            {
                schoolResponse = new SchoolResponse
                {
                    Id = school.Id,
                    Name = school.Name,
                    Address = school.Address,
                    PhoneNumber = school.PhoneNumber,
                    Email = school.Email,
                    schoolShiftResponses = school.SchoolShifts?.Select(x => new SchoolShiftResponse
                    {
                        Id = x.Id,
                        Time = x.Time,
                        ShiftType = x.ShiftType.ToString().ToUpper(),
                        SessionType = x.SessionType.ToString().ToUpper()
                    }).ToList()!
                };
            }

            // Lấy thông tin parent (nếu có)
            ParentResponse? parentResponse = null;
            if (user!.ParentId != null)
            {
                var parent = await _unitOfWork.GetRepository<User>()
                    .Entities.FirstOrDefaultAsync(x => x.Id == user.ParentId);
                if (parent != null)
                {
                    parentResponse = new ParentResponse
                    {
                        Id = parent.Id,
                        Address = parent.Address,
                        DateOfBirth = parent.DateOfBirth,
                        Email = parent.Email,
                        FullName = parent.FullName,
                        Gender = parent.Gender,
                        PhoneNumber = parent.PhoneNumber,
                        ProfileImageUrl = parent.ProfileImageUrl!
                    };
                }
            }
            // Lấy danh sách children (nếu có)
            var children = new List<ChildResponse>();

            var childUsers = await _unitOfWork.GetRepository<User>()
                .Entities.Where(x => x.ParentId == user.Id).ToListAsync();
            if (childUsers != null)
            {
                foreach (var child in childUsers)
                {

                    // Lấy thông tin trường học (nếu có)
                    SchoolResponse? childSchoolResponse = null;

                    var childSchool = await _unitOfWork.GetRepository<School>()
                        .Entities.Include(x => x.SchoolShifts)
                        .FirstOrDefaultAsync(x => x.Id == child.SchoolId);

                    if (childSchool != null)
                    {
                        childSchoolResponse = new SchoolResponse
                        {
                            Id = childSchool.Id,
                            Name = childSchool.Name,
                            Address = childSchool.Address,
                            PhoneNumber = childSchool.PhoneNumber,
                            Email = childSchool.Email,
                            schoolShiftResponses = childSchool.SchoolShifts?.Select(x => new SchoolShiftResponse
                            {
                                Id = x.Id,
                                Time = x.Time,
                                ShiftType = x.ShiftType.ToString().ToUpper(),
                                SessionType = x.SessionType.ToString().ToUpper()
                            }).ToList()!
                        };
                    }
                    children.Add(new ChildResponse
                    {
                        Id = child.Id,
                        Address = child.Address,
                        DateOfBirth = child.DateOfBirth,
                        Email = child.Email,
                        FullName = child.FullName,
                        Gender = child.Gender,
                        PhoneNumber = child.PhoneNumber,
                        ProfileImageUrl = child.ProfileImageUrl!,
                        School = childSchoolResponse
                    });
                }
            }


            UserInforModel inforModel = new UserInforModel
            {
                Id = user.Id,
                Address = user.Address,
                DateOfBirth = user.DateOfBirth,
                Email = user.Email,
                FullName = user.FullName,
                Gender = user.Gender,
                PhoneNumber = user.PhoneNumber,
                ProfileImageUrl = user.ProfileImageUrl!,
                Parent = parentResponse,
                Childs = children,
                School = schoolResponse!,
                RoleName = user.UserRoles.FirstOrDefault(x => !x.Role.DeletedTime.HasValue)!.Role.Name.ToUpper(),
            };
            return inforModel;
        }
        public async Task AssignUserToRoleAsync(Guid userId, Guid roleId)
        {
            var user = await _unitOfWork.GetRepository<User>()
                .Entities.FirstOrDefaultAsync(x => x.Id == userId && !x.DeletedTime.HasValue)
                ?? throw new ErrorException(StatusCodes.Status404NotFound, ResponseCodeConstants.NOT_FOUND, "Người dùng không tồn tại!");

            var role = await _unitOfWork.GetRepository<Role>()
                .Entities.FirstOrDefaultAsync(x => x.Id == roleId)
                ?? throw new ErrorException(StatusCodes.Status404NotFound, ResponseCodeConstants.NOT_FOUND, "Vai trò không tồn tại!");

            var userRoleRepo = _unitOfWork.GetRepository<UserRole>();

            var existingUserRole = await userRoleRepo.Entities
                .FirstOrDefaultAsync(ur => ur.UserId == userId && ur.RoleId == roleId);

            if (existingUserRole != null)
            {
                throw new ErrorException(StatusCodes.Status400BadRequest, ResponseCodeConstants.BADREQUEST, "Người dùng đã được gán vai trò này!");
            }

            var rolesToDelete = await userRoleRepo.Entities
                .Where(ur => ur.UserId == userId)
                .ToListAsync();

            if (rolesToDelete.Any())
            {
                foreach (var roleToDelete in rolesToDelete)
                {
                    userRoleRepo.Delete(roleToDelete);
                }
                await _unitOfWork.SaveAsync();
            }

            var newUserRole = new UserRole
            {
                UserId = userId,
                RoleId = roleId
            };

            await userRoleRepo.InsertAsync(newUserRole);
            await _unitOfWork.SaveAsync();
        }
        public async Task RemoveUserToRoleAsync(Guid userId)
        {
            var user = await _unitOfWork.GetRepository<User>()
                .Entities.FirstOrDefaultAsync(x => x.Id == userId && !x.DeletedTime.HasValue)
                ?? throw new ErrorException(StatusCodes.Status404NotFound, ResponseCodeConstants.NOT_FOUND, "Người dùng không tồn tại!");

            var userRoleRepo = _unitOfWork.GetRepository<UserRole>();

            var existingUserRole = await userRoleRepo.Entities
                .FirstOrDefaultAsync(ur => ur.UserId == userId);

            if (existingUserRole != null)
            {
                throw new ErrorException(StatusCodes.Status400BadRequest, ResponseCodeConstants.BADREQUEST, "Người dùng đã được gán vai trò này!");
            }

            var rolesToDelete = await userRoleRepo.Entities
                .Where(ur => ur.UserId == userId)
                .ToListAsync();

            if (rolesToDelete.Any())
            {
                foreach (var roleToDelete in rolesToDelete)
                {
                    userRoleRepo.Delete(roleToDelete);
                }
            }

            await _unitOfWork.SaveAsync();
        }
        public async Task UpdateProfiel(Guid? id = null, UpdateProfileModel? model = null)
        {
            // Lấy userId từ HttpContext
            string userId = Authentication.GetUserIdFromHttpContextAccessor(_contextAccessor);

            Guid.TryParse(userId, out Guid cb);
            if (id != null)
            {
                User user = await _unitOfWork.GetRepository<User>()
                    .Entities.FirstOrDefaultAsync(x => x.Id == id && !x.DeletedTime.HasValue) ?? throw new ErrorException(StatusCodes.Status404NotFound, ErrorCode.NotFound, "Tài khoản không tồn tại!");
                user.FullName = model.FullName;
                user.PhoneNumber = model.PhoneNumber;
                user.Gender = model.Gender;
                user.Address = model.Address;
                user.DateOfBirth = model.DateOfBirth;
                user.ProfileImageUrl = model.ProfileImageUrl;
                await _unitOfWork.GetRepository<User>().UpdateAsync(user);
            }
            else
            {
                User user = await _unitOfWork.GetRepository<User>()
                    .Entities.FirstOrDefaultAsync(x => x.Id == cb && !x.DeletedTime.HasValue) ?? throw new ErrorException(StatusCodes.Status404NotFound, ErrorCode.NotFound, "Tài khoản không tồn tại!");
                user.FullName = model.FullName;
                user.PhoneNumber = model.PhoneNumber;
                user.Gender = model.Gender;
                user.Address = model.Address;
                user.DateOfBirth = model.DateOfBirth;
                user.ProfileImageUrl = model.ProfileImageUrl;
                await _unitOfWork.GetRepository<User>().UpdateAsync(user);
            }
            await _unitOfWork.SaveAsync();

            //if (model.FullName.Length < 8)
            //{
            //    throw new ErrorException(StatusCodes.Status400BadRequest, ErrorCode.BadRequest, "Tên phải chứa ít nhất 8 kí tự!!");
            //}

            //if (string.IsNullOrEmpty(model.PhoneNumber) || model.PhoneNumber.Length < 10)
            //{
            //    throw new ErrorException(StatusCodes.Status400BadRequest, ErrorCode.BadRequest, "Số điện thoại phải có ít nhất 10 chữ số!!");
            //}

            //if (model.Gender == null)
            //{
            //    throw new ErrorException(StatusCodes.Status400BadRequest, ErrorCode.BadRequest, "Giới tính không được để trống!!");
            //}

            //if (string.IsNullOrEmpty(model.Address))
            //{
            //    throw new ErrorException(StatusCodes.Status400BadRequest, ErrorCode.BadRequest, "Địa chỉ không được để trống!!");
            //}
        }
        public async Task<string> BlockUserForAdmin(Guid userId)
        {
            var user = await _unitOfWork.GetRepository<User>().Entities.FirstOrDefaultAsync(x => x.Id == userId && !x.DeletedTime.HasValue) ?? throw new ErrorException(StatusCodes.Status404NotFound, ErrorCode.NotFound, "Không tìm thấy người dùng!");
            user.Violate = true;
            await _unitOfWork.GetRepository<User>().UpdateAsync(user);
            await _unitOfWork.SaveAsync();
            // Gửi email thông báo cho user
            await SendBlockUserEmail(user);
            return "Khóa người dùng thành công!";
        }
        public async Task<string> UnBlockUserForAdmin(Guid userId)
        {
            var user = await _unitOfWork.GetRepository<User>().Entities.FirstOrDefaultAsync(x => x.Id == userId && !x.DeletedTime.HasValue) ?? throw new ErrorException(StatusCodes.Status404NotFound, ErrorCode.NotFound, "Không tìm thấy người dùng!");
            user.Violate = false;
            await _unitOfWork.GetRepository<User>().UpdateAsync(user);
            await _unitOfWork.SaveAsync();
            // Gửi email thông báo cho user
            await SendUnBlockUserEmail(user);
            return "Mở khóa người dùng thành công!";
        }
        private async Task SendBlockUserEmail(User guide)
        {
            await _emailService.SendEmailAsync(
                guide.Email,
                "Thông Báo khóa tài khoản",
                $@"
            <html>
            <body>
                <h2>THÔNG BÁO KHÓA TÀI KHOẢN</h2>
                <p>Xin chào {guide.FullName},</p>
                <p>Chúng tôi xin thông báo rằng tài khoản của bạn đã bị khóa do vi phạm vi định của app.</p>
                <p><strong>Trạng thái tài khoản:</strong> Đã khóa</p>
                <p>Nếu có bất kỳ thắc mắc nào, vui lòng liên hệ với chúng tôi.</p>
                <p>Cảm ơn bạn đã sử dụng dịch vụ của chúng tôi!</p>
            </body>
            </html>"
            );
        }
        private async Task SendUnBlockUserEmail(User guide)
        {
            await _emailService.SendEmailAsync(
                guide.Email,
                "Thông Báo mở khóa tài khoản",
                $@"
            <html>
            <body>
                <h2>THÔNG BÁO MỞ KHÓA TÀI KHOẢN</h2>
                <p>Xin chào {guide.FullName},</p>
                <p>Chúng tôi xin thông báo rằng tài khoản của bạn đã được mở khóa.</p>
                <p><strong>Trạng thái tài khoản:</strong> Mở khóa</p>
                <p>Nếu có bất kỳ thắc mắc nào, vui lòng liên hệ với chúng tôi.</p>
                <p>Cảm ơn bạn đã sử dụng dịch vụ của chúng tôi!</p>
            </body>
            </html>"
            );
        }
    }
}
