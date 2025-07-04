using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using ShuttleMate.Contract.Repositories.Entities;
using ShuttleMate.Contract.Repositories.IUOW;
using ShuttleMate.Contract.Services.Interfaces;
using ShuttleMate.Core.Bases;
using ShuttleMate.Core.Constants;
using ShuttleMate.Core.Utils;
using ShuttleMate.ModelViews.AuthModelViews;
using ShuttleMate.ModelViews.RoleModelViews;
using static ShuttleMate.Contract.Repositories.Enum.GeneralEnum;

namespace ShuttleMate.Services.Services
{
    public class AuthService : IAuthService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IEmailService _emailService;
        private readonly ITokenService _tokenService;

        public AuthService(IUnitOfWork unitOfWork, IEmailService emailService, ITokenService tokenService)
        {
            _unitOfWork = unitOfWork;
            _emailService = emailService;
            _tokenService = tokenService;
        }
        private string GenerateOtp()
        {
            Random random = new Random();
            string otp = random.Next(100000, 999999).ToString();
            return otp;
        }
        public async Task ForgotPassword(EmailModelView model)
        {
            User? user = await _unitOfWork.GetRepository<User>().Entities.FirstOrDefaultAsync(x => x.Email == model.Email && !x.DeletedTime.HasValue)
                ?? throw new ErrorException(StatusCodes.Status400BadRequest, ErrorCode.BadRequest, "Vui lòng kiểm tra email của bạn");

            if (user.EmailVerified == false)
            {
                throw new ErrorException(StatusCodes.Status400BadRequest, ErrorCode.BadRequest, "Vui lòng kiểm tra email của bạn");
            }

            string OTP = GenerateOtp();
            user.EmailCode = int.Parse(OTP);
            user.CodeGeneratedTime = DateTime.Now;

            await _unitOfWork.GetRepository<User>().UpdateAsync(user);
            await _unitOfWork.SaveAsync();

            await _emailService.SendEmailAsync(model.Email, "Đặt lại mật khẩu", $"Vui lòng xác nhận tài khoản của bạn, OTP của bạn là: <div class='otp'>{OTP}</div>");
        }
        public async Task Register(RegisterModel model)
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
            newUser.EmailVerified = false;


            // Xác định vai trò
            if (model.RoleName == true)
            {
                var role = await _unitOfWork.GetRepository<Role>().Entities
                .FirstOrDefaultAsync(x => x.Name == "Student");
                if (role == null)
                {
                    throw new ErrorException(StatusCodes.Status400BadRequest, ErrorCode.BadRequest, "Vai trò không tồn tại!");
                }
                // Thêm người dùng  vào cơ sở dữ liệu
                await _unitOfWork.GetRepository<User>().InsertAsync(newUser);

                UserRole userRole = new UserRole()
                {
                    UserId = newUser.Id,
                    RoleId = role.Id,
                };
                await _unitOfWork.GetRepository<UserRole>().InsertAsync(userRole);

            }
            else
            {
                var role = await _unitOfWork.GetRepository<Role>().Entities
                .FirstOrDefaultAsync(x => x.Name == "Parent");
                if (role == null)
                {
                    throw new ErrorException(StatusCodes.Status400BadRequest, ErrorCode.BadRequest, "Vai trò không tồn tại!");
                }
                // Thêm người dùng  vào cơ sở dữ liệu
                await _unitOfWork.GetRepository<User>().InsertAsync(newUser);

                UserRole userRole = new UserRole()
                {
                    UserId = newUser.Id,
                    RoleId = role.Id,
                };
                await _unitOfWork.GetRepository<UserRole>().InsertAsync(userRole);

            }

            string OTP = GenerateOtp();
            newUser.EmailCode = int.Parse(OTP);
            newUser.CodeGeneratedTime = DateTime.Now;
            await _emailService.SendEmailAsync(model.Email, "Xác nhận tài khoản", $"Vui lòng xác nhận tài khoản của bạn, OTP của bạn là: <div class='otp'>{OTP}</div>");

            await _unitOfWork.SaveAsync();

        }
        public async Task ResendConfirmationEmail(EmailModelView emailModelView)
        {
            if (string.IsNullOrWhiteSpace(emailModelView.Email))
            {
                throw new ErrorException(StatusCodes.Status400BadRequest, ErrorCode.BadRequest, "Email không được để trống!");
            }
            var user = await _unitOfWork.GetRepository<User>().Entities.FirstOrDefaultAsync(x => x.Email == emailModelView.Email && !x.DeletedTime.HasValue) ?? throw new ErrorException(StatusCodes.Status404NotFound, ErrorCode.NotFound, "Không tìm thấy Email");

            int OTP = Int32.Parse(GenerateOtp());
            user.EmailCode = OTP;
            user.CodeGeneratedTime = DateTime.Now;
            await _emailService.SendEmailAsync(emailModelView.Email, "Xác nhận tài khoản",
           $"Vui lòng xác nhận tài khoản của bạn, OTP của bạn là:  <div class='otp'>{OTP}</div>");
            await _unitOfWork.GetRepository<User>().UpdateAsync(user);
            await _unitOfWork.SaveAsync();

        }
        public async Task<Guid> ConfirmEmail(ConfirmEmailModel model)
        {
            if (string.IsNullOrWhiteSpace(model.Email))
            {
                throw new ErrorException(StatusCodes.Status400BadRequest, ErrorCode.BadRequest, "Email không được để trống!");
            }
            if (string.IsNullOrWhiteSpace(model.Otp.ToString()))
            {
                throw new ErrorException(StatusCodes.Status400BadRequest, ErrorCode.BadRequest, "Otp không được để trống!");
            }
            User user = await _unitOfWork.GetRepository<User>().Entities.FirstOrDefaultAsync(x => x.Email == model.Email && !x.DeletedTime.HasValue) ?? throw new ErrorException(StatusCodes.Status400BadRequest, ErrorCode.BadRequest, "Email không tồn tại!");
            if (user.EmailCode.ToString() != model.Otp.ToString())
            {
                throw new ErrorException(StatusCodes.Status400BadRequest, ErrorCode.BadRequest, "Otp sai, vui lòng nhập lại!");
            }
            user.EmailVerified = true;
            await _unitOfWork.GetRepository<User>().UpdateAsync(user);
            await _unitOfWork.SaveAsync();
            return user.Id;
        }
        public async Task<LoginResponse> LoginAsync(LoginRequestModel request)
        {
            var user = _unitOfWork.GetRepository<User>().Entities
                .Where(u => !u.DeletedTime.HasValue && u.Email == request.Email)
                .FirstOrDefault()
                ?? throw new ErrorException(StatusCodes.Status404NotFound, ResponseCodeConstants.NOT_FOUND, "Không tìm thấy tài khoản");
            if (user.EmailVerified == false)
            {
                throw new ErrorException(StatusCodes.Status400BadRequest, ResponseCodeConstants.BADREQUEST, "Tài khoản chưa được xác thực!");
            }
            if (user.Violate == true)
            {
                throw new ErrorException(StatusCodes.Status400BadRequest, ErrorCode.BadRequest, "Tài khoản đã bị khóa");
            }
            // create hash
            var passwordHasher = new FixedSaltPasswordHasher<User>(Options.Create(new PasswordHasherOptions()));

            var hashedInputPassword = passwordHasher.HashPassword(null, request.Password);

            if (hashedInputPassword != user.PasswordHash)
            {
                throw new ErrorException(StatusCodes.Status400BadRequest, ErrorCode.BadRequest, "Tài khoản hoặc mật khẩu không chính xác!");
            }

            //// Get the user's role
            //var roles = await _userManager.GetRolesAsync(user);
            //var role = roles.FirstOrDefault(); // Assuming a single role for simplicity
            UserRole roleUser = _unitOfWork.GetRepository<UserRole>().Entities.Where(x => x.UserId == user.Id).FirstOrDefault()
                                ?? throw new ErrorException(StatusCodes.Status404NotFound, ErrorCode.NotFound, "Không tìm thấy tài khoản");
            string roleName = _unitOfWork.GetRepository<Role>().Entities.Where(x => x.Id == roleUser.RoleId).Select(x => x.Name).FirstOrDefault()
             ?? "unknow";
            var tokenResponse = _tokenService.GenerateTokens(user, roleName);
            //var token = Authentication.CreateToken(user.Id.ToString(), _jwtSettings);
            var loginResponse = new LoginResponse
            {
                TokenResponse = tokenResponse,
                Role = roleName,
            };
            return loginResponse;

        }
        public async Task ResetPassword(ResetPasswordModelView model)
        {
            User? user = await _unitOfWork.GetRepository<User>().Entities
                .FirstOrDefaultAsync(x => x.Email == model.Email && !x.DeletedTime.HasValue) ?? throw new ErrorException(StatusCodes.Status404NotFound, ErrorCode.NotFound, "Không tìm thấy tài khoản!");

            if (user.EmailVerified == false)
            {
                throw new ErrorException(StatusCodes.Status400BadRequest, ErrorCode.BadRequest, "Tài khoản của bạn chưa kích hoạt!");
            }
            if (model.Password != model.ConfirmPassword)
            {
                throw new ErrorException(StatusCodes.Status400BadRequest, ErrorCode.BadRequest, "Mật khẩu xác nhận không đúng!");
            }

            var passwordHasher = new FixedSaltPasswordHasher<User>(Options.Create(new PasswordHasherOptions()));

            user.PasswordHash = passwordHasher.HashPassword(null, model.Password); // Băm mật khẩu tại đây
            await _unitOfWork.GetRepository<User>().UpdateAsync(user);
            await _unitOfWork.SaveAsync();
        }
        public async Task<Guid> VerifyOtp(ConfirmOTPModelView model)
        {
            User? user = await _unitOfWork.GetRepository<User>().Entities
                .FirstOrDefaultAsync(x => x.Email == model.Email && !x.DeletedTime.HasValue) ?? throw new ErrorException(StatusCodes.Status404NotFound, ErrorCode.NotFound, "Không tìm thấy tài khoản!");

            if (user.EmailCode == null || user.EmailCode.ToString() != model.OTP)
            {
                throw new ErrorException(StatusCodes.Status400BadRequest, ErrorCode.BadRequest, "OTP không hợp lệ");
            }

            if (user.EmailVerified == false)
            {
                throw new ErrorException(StatusCodes.Status400BadRequest, ErrorCode.BadRequest, "Tài khoản của bạn chưa kích hoạt!");
            }

            if (!user.CodeGeneratedTime.HasValue || DateTime.Now > user.CodeGeneratedTime.Value.AddMinutes(5))
            {
                throw new ErrorException(StatusCodes.Status400BadRequest, ErrorCode.BadRequest, "OTP đã hết hạn");
            }

            user.EmailCode = null;
            user.CodeGeneratedTime = null;
            //đưa id để đổi mk
            return user.Id;
        }
        public async Task<string> ChangePasswordFromForgetPassword(ChangePasswordFromForgetPasswordModel model)
        {
            User? user = await _unitOfWork.GetRepository<User>().Entities
                .FirstOrDefaultAsync(x => x.Id == model.Id && !x.DeletedTime.HasValue) ?? throw new ErrorException(StatusCodes.Status404NotFound, ErrorCode.NotFound, "Không tìm thấy tài khoản!");
            if (model.Password != model.ConfirmPassword)
            {
                throw new ErrorException(StatusCodes.Status400BadRequest, ErrorCode.BadRequest, "Mật khẩu xác nhận không đúng!");
            }

            var passwordHasher = new FixedSaltPasswordHasher<User>(Options.Create(new PasswordHasherOptions()));

            user.PasswordHash = passwordHasher.HashPassword(null, model.Password); // Băm mật khẩu tại đây
            return "Đổi mật khẩu thành công!";
        }
        public async Task<LoginResponse> RefreshToken(RefreshTokenModel refreshTokenModel)
        {
            User? user = await CheckRefreshToken(refreshTokenModel.RefreshToken);

            UserRole roleUser = await _unitOfWork.GetRepository<UserRole>().Entities
                .FirstOrDefaultAsync(x => x.UserId == user.Id)
                ?? throw new ErrorException(StatusCodes.Status404NotFound, ErrorCode.NotFound, "Không tìm thấy tài khoản");

            string roleName = _unitOfWork.GetRepository<Role>().Entities
                .Where(x => x.Id == roleUser.RoleId)
                .Select(x => x.Name)
                .FirstOrDefault() ?? "unknown";

            var tokenResponse = _tokenService.GenerateTokens(user, roleName);

            return new LoginResponse
            {
                TokenResponse = tokenResponse,
                Role = roleName
            };
        }
        private async Task<User> CheckRefreshToken(string refreshToken)
        {

            User users = await _unitOfWork.GetRepository<User>().Entities
                .FirstOrDefaultAsync(x => x.RefeshToken == refreshToken)
                ?? throw new ErrorException(StatusCodes.Status404NotFound, ErrorCode.NotFound, "Không tìm thấy tài khoản");
            return users;
        }
        public async Task LogoutAsync(RefreshTokenModel model)
        {
            // Tải toàn bộ user (hoặc tối ưu hơn nếu bạn có userId từ JWT)
            var users = await _unitOfWork.GetRepository<User>().Entities.FirstOrDefaultAsync(x => x.RefeshToken == model.RefreshToken && !x.DeletedTime.HasValue) ?? throw new ErrorException(StatusCodes.Status400BadRequest, ErrorCode.BadRequest, "Token không hợp lệ");
            users.RefeshToken = null;
            await _unitOfWork.GetRepository<User>().UpdateAsync(users);
            await _unitOfWork.SaveAsync();
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
                case RoleEnum.Student:
                    var role = await _unitOfWork.GetRepository<Role>().Entities
                    .FirstOrDefaultAsync(x => x.Name == "Student");
                    if (role == null)
                    {
                        throw new ErrorException(StatusCodes.Status400BadRequest, ErrorCode.BadRequest, "Vai trò không tồn tại!");
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
                case RoleEnum.Parent:
                    var roleStudent = await _unitOfWork.GetRepository<Role>().Entities
                    .FirstOrDefaultAsync(x => x.Name == "Parent");
                    if (roleStudent == null)
                    {
                        throw new ErrorException(StatusCodes.Status400BadRequest, ErrorCode.BadRequest, "Vai trò không tồn tại!");
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
                case RoleEnum.Operator:
                    var roleOperator = await _unitOfWork.GetRepository<Role>().Entities
                    .FirstOrDefaultAsync(x => x.Name == "Operator");
                    if (roleOperator == null)
                    {
                        throw new ErrorException(StatusCodes.Status400BadRequest, ErrorCode.BadRequest, "Vai trò không tồn tại!");
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
                case RoleEnum.Driver:
                    var roleDriver = await _unitOfWork.GetRepository<Role>().Entities
                    .FirstOrDefaultAsync(x => x.Name == "Driver");
                    if (roleDriver == null)
                    {
                        throw new ErrorException(StatusCodes.Status400BadRequest, ErrorCode.BadRequest, "Vai trò không tồn tại!");
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
                default:
                    throw new ErrorException(StatusCodes.Status400BadRequest, ErrorCode.BadRequest, "Vui lòng chọn đúng vai trò!!");
                    
            }

            await _unitOfWork.SaveAsync();

        }
    }
}
