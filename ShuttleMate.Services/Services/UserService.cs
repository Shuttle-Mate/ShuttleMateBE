using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using ShuttleMate.Contract.Repositories.Entities;
using ShuttleMate.Contract.Repositories.IUOW;
using ShuttleMate.Contract.Services.Interfaces;
using ShuttleMate.Core.Bases;
using ShuttleMate.Core.Constants;
using ShuttleMate.Core.Utils;
using ShuttleMate.ModelViews.AuthModelViews;
using ShuttleMate.ModelViews.UserModelViews;
using ShuttleMate.Services.Services.Infrastructure;
using static ShuttleMate.Contract.Repositories.Enum.GeneralEnum;

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

        public UserService(IUnitOfWork unitOfWork, IMapper mapper, IConfiguration configuration, IHttpContextAccessor contextAccessor, IEmailService emailService)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _configuration = configuration;
            _contextAccessor = contextAccessor;
            _apiKey = configuration["VietMap:ApiKey"] ?? throw new Exception("API key is missing from configuration.");
            _emailService = emailService;
        }

        public async Task CreateUserAdmin(CreateUserAdminModel model)
        {
            // Kiểm tra user co tồn tại
            var user = await _unitOfWork.GetRepository<User>().Entities
                .FirstOrDefaultAsync(x => x.Email == model.Email && !x.DeletedTime.HasValue);

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
                    // Thêm người dùng  vào cơ sở dữ liệu
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
        public async Task AssignParent(AssignParentModel model)
        {
            var user = await _unitOfWork.GetRepository<User>()
                .Entities.FirstOrDefaultAsync(x => x.Id == model.UserId && !x.DeletedTime.HasValue)
                ?? throw new ErrorException(StatusCodes.Status404NotFound, ResponseCodeConstants.NOT_FOUND, "Người dùng không tồn tại!");
            var parent = await _unitOfWork.GetRepository<User>()
                .Entities.FirstOrDefaultAsync(x => x.Id == model.ParentId && !x.DeletedTime.HasValue)
                ?? throw new ErrorException(StatusCodes.Status404NotFound, ResponseCodeConstants.NOT_FOUND, "Phụ huynh không tồn tại!");
            user.ParentId = parent.Id;
            await _unitOfWork.GetRepository<User>().UpdateAsync(user);
            await _unitOfWork.SaveAsync();
        }
        public async Task AssignParentForParent(AssignParentForStudentModel model)
        {
            // Lấy userId từ HttpContext
            string userId = Authentication.GetUserIdFromHttpContextAccessor(_contextAccessor);

            Guid.TryParse(userId, out Guid cb);

            var user = await _unitOfWork.GetRepository<User>()
                .Entities.FirstOrDefaultAsync(x => x.Id == cb && !x.DeletedTime.HasValue)
                ?? throw new ErrorException(StatusCodes.Status404NotFound, ResponseCodeConstants.NOT_FOUND, "Người dùng không tồn tại!");
            var parent = await _unitOfWork.GetRepository<User>()
                .Entities.FirstOrDefaultAsync(x => x.Id == model.ParentId && !x.DeletedTime.HasValue)
                ?? throw new ErrorException(StatusCodes.Status404NotFound, ResponseCodeConstants.NOT_FOUND, "Phụ huynh không tồn tại!");
            user.ParentId = parent.Id;
            await _unitOfWork.GetRepository<User>().UpdateAsync(user);
            await _unitOfWork.SaveAsync();
        }
        public async Task<IEnumerable<AdminResponseUserModel>> GetAllAsync(Guid? roleId = null, string? name = null, bool? gender = null)
        {
            var userRepo = _unitOfWork.GetRepository<User>();

            var query = userRepo.Entities
        .Include(u => u.UserRoles)
        .ThenInclude(ur => ur.Role)
        .AsQueryable();

            if (roleId.HasValue)
            {
                query = query.Where(u => u.UserRoles.Any(ur => ur.RoleId == roleId));
            }
            if (!string.IsNullOrWhiteSpace(name))
            {
                query = query.Where(u => u.FullName.Contains(name));
            }
            if (gender != null)
            {
                query = query.Where(u => u.Gender == gender);
            }

            var users = await query
                .Select(u => new AdminResponseUserModel
                {
                    Id = u.Id,
                    RoleName = u.UserRoles.Select(ur => ur.Role.Name).FirstOrDefault(),
                    FullName = u.FullName,
                    Gender = u.Gender,
                    DateOfBirth = u.DateOfBirth,
                    ProfileImageUrl = u.ProfileImageUrl,
                    Address = u.Address,
                    EmailVerified = u.EmailVerified,
                    IdentificationNumber = u.IdentificationNumber,
                    EmailCode = u.EmailCode,
                    CodeGeneratedTime = u.CodeGeneratedTime,
                    CreatedBy = u.CreatedBy,
                    Violate = u.Violate,
                    LastUpdatedBy = u.LastUpdatedBy,
                    DeletedBy = u.DeletedBy,
                    CreatedTime = u.CreatedTime,
                    LastUpdatedTime = u.LastUpdatedTime,
                    DeletedTime = u.DeletedTime
                })
                .ToListAsync();

            return users;
        }
        public async Task<UserInforModel> GetInfor()
        {
            // Lấy userId từ HttpContext
            string userId = Authentication.GetUserIdFromHttpContextAccessor(_contextAccessor);

            Guid.TryParse(userId, out Guid cb);


            // Lấy thông tin người dùng
            User user = await _unitOfWork.GetRepository<User>()
                .Entities.FirstOrDefaultAsync(x => x.Id == cb);
            UserInforModel inforModel = new UserInforModel
            {
                Id = user.Id,
                Address = user.Address,
                DateOfBirth = user.DateOfBirth,
                Email = user.Email,
                FullName = user.FullName,
                Gender = user.Gender,
                PhoneNumber = user.PhoneNumber,
                ProfileImageUrl = user.ProfileImageUrl
            };
            return inforModel;
        }
        public async Task<UserResponseModel> GetById(Guid userId)
        {

            // Lấy thông tin người dùng
            User user = await _unitOfWork.GetRepository<User>()
                .Entities.FirstOrDefaultAsync(x => x.Id == userId) ?? throw new ErrorException(StatusCodes.Status404NotFound, ResponseCodeConstants.NOT_FOUND, "Người dùng không tồn tại!");

            UserResponseModel inforModel = new UserResponseModel
            {
                Id = user.Id,
                Address = user.Address,
                DateOfBirth = user.DateOfBirth,
                Email = user.Email,
                FullName = user.FullName,
                Gender = user.Gender,
                PhoneNumber = user.PhoneNumber,
                ProfileImageUrl = user.ProfileImageUrl,
                RoleName = user.UserRoles.FirstOrDefault().Role.Name.ToUpper()
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
        public async Task UpdateProfiel(UpdateProfileModel model)
        {
            // Lấy userId từ HttpContext
            string userId = Authentication.GetUserIdFromHttpContextAccessor(_contextAccessor);

            Guid.TryParse(userId, out Guid cb);


            User user = await _unitOfWork.GetRepository<User>()
         .Entities.FirstOrDefaultAsync(x => x.Id == cb && !x.DeletedTime.HasValue) ?? throw new ErrorException(StatusCodes.Status404NotFound, ErrorCode.NotFound, "Tài khoản không tồn tại!");
            School school = await _unitOfWork.GetRepository<School>()
         .Entities.FirstOrDefaultAsync(x => x.Id == model.SchoolId && !x.DeletedTime.HasValue) ?? throw new ErrorException(StatusCodes.Status404NotFound, ErrorCode.NotFound, "Trường học không tồn tại!");

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

            user.FullName = model.FullName;
            user.PhoneNumber = model.PhoneNumber;
            user.Gender = model.Gender;
            user.Address = model.Address;
            user.DateOfBirth = model.DateOfBirth;
            user.ProfileImageUrl = model.ProfileImageUrl;
            user.SchoolId = model.SchoolId;
            await _unitOfWork.GetRepository<User>().UpdateAsync(user);
            await _unitOfWork.SaveAsync();
        }
        public async Task<string> BlockUserForAdmin(BlockUserForAdminModel model)
        {
            var user = await _unitOfWork.GetRepository<User>().Entities.FirstOrDefaultAsync(x => x.Id == model.UserId && !x.DeletedTime.HasValue) ?? throw new ErrorException(StatusCodes.Status404NotFound, ErrorCode.NotFound, "Không tìm thấy người dùng!");
            user.Violate = true;
            await _unitOfWork.GetRepository<User>().UpdateAsync(user);
            await _unitOfWork.SaveAsync();
            // Gửi email thông báo cho user
            await SendBlockUserEmail(user);
            return "Khóa người dùng thành công!";
        }
        public async Task<string> UnBlockUserForAdmin(UnBlockUserForAdminModel model)
        {
            var user = await _unitOfWork.GetRepository<User>().Entities.FirstOrDefaultAsync(x => x.Id == model.UserId && !x.DeletedTime.HasValue) ?? throw new ErrorException(StatusCodes.Status404NotFound, ErrorCode.NotFound, "Không tìm thấy người dùng!");
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
