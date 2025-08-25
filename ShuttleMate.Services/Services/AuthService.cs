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
using System.Text.RegularExpressions;

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
            if (string.IsNullOrEmpty(model.Email))
            {
                throw new ErrorException(StatusCodes.Status400BadRequest, ErrorCode.BadRequest, "Email không được để trống!");
            }

            if (!Regex.IsMatch(model.Email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
            {
                throw new ErrorException(StatusCodes.Status400BadRequest, ErrorCode.BadRequest, "Email không hợp lệ!");
            }

            if (model.Password != model.ConfirmPassword)
            {
                throw new ErrorException(StatusCodes.Status400BadRequest, ErrorCode.BadRequest, "Xác nhận mật khẩu không đúng!");
            }
            if (string.IsNullOrWhiteSpace(model.Password) || model.Password.Length < 6)
            {
                throw new ErrorException(StatusCodes.Status400BadRequest, ErrorCode.BadRequest, "Mật khẩu phải có ít nhất 6 ký tự và không được để trống!");
            }
            if (string.IsNullOrWhiteSpace(model.PhoneNumber) || !Regex.IsMatch(model.PhoneNumber, @"^\d{10}$"))
            {
                throw new ErrorException(StatusCodes.Status400BadRequest, ErrorCode.BadRequest, "Số điện thoại phải gồm đúng 10 chữ số!");
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
            newUser.AssignCode = await GenerateUniqueAssignCodeAsync();


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
            await _emailService.SendEmailAsync(
                model.Email,
                                                        "Mã OTP xác nhận tài khoản",
                $@"
                <div style='font-family: Arial, sans-serif; max-width: 600px; margin: auto; padding: 20px; background-color: #FAF9F7;'>
                    <h2 style='color: #124DA3; border-bottom: 2px solid #F37022; padding-bottom: 10px;'>Mã OTP của bạn</h2>
                    
                    <p style='color: #333;'>Xin chào,</p>
                    
                    <p style='color: #333;'>Mã OTP một lần để xác nhận tài khoản của bạn là:</p>
                    
                    <div style='font-size: 24px; font-weight: bold; letter-spacing: 2px; 
                                color: #124DA3; background: white; padding: 15px; 
                                display: inline-block; margin: 15px 0; border-radius: 4px;
                                border: 2px dashed #F37022;'>
                        {OTP}
                    </div>
                    
                    <p style='color: #333;'>Mã này có hiệu lực trong <strong style='color: #124DA3;'>2 phút</strong>.</p>
                    
                    <a href='#' style='display: inline-block; background-color: #F37022; color: white; 
                                      padding: 10px 20px; text-decoration: none; border-radius: 4px;
                                      margin: 15px 0; font-weight: bold;'>
                        Xác nhận ngay
                    </a>
                    
                    <p style='color: #ff0000;'><strong>⚠️ Lưu ý:</strong> Vui lòng không chia sẻ mã này với bất kỳ ai.</p>
                    
                    <p style='color: #4EB748; font-style: italic;'>
                        <strong>✔️ Thành công:</strong> Yêu cầu OTP của bạn đã được tạo!
                    </p>
                    
                    <p style='color: #333; font-size: 14px;'>
                        Nếu bạn không yêu cầu mã này, vui lòng bỏ qua email này.
                    </p>
                    
                    <div style='margin-top: 30px; padding-top: 15px; border-top: 1px solid #eee;'>
                        <p style='color: #124DA3; font-weight: bold;'>Đội ngũ hỗ trợ ShuttleMate </p>
                        <p style='font-size: 12px; color: #999;'>
                            © {DateTime.Now.Year} ShuttleMate . Bảo lưu mọi quyền.
                        </p>
                    </div>
                </div>
                "
            );
            await _unitOfWork.SaveAsync();

        }

        private async Task<string> GenerateUniqueAssignCodeAsync()
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            var random = new Random();
            string assignCode;
            bool exists;

            do
            {
                assignCode = new string(Enumerable.Repeat(chars, 8)
                    .Select(s => s[random.Next(s.Length)]).ToArray());

                exists = await _unitOfWork.GetRepository<User>().Entities
                    .AnyAsync(x => x.AssignCode == assignCode && !x.DeletedTime.HasValue);
            }
            while (exists);

            return assignCode;
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
            await _emailService.SendEmailAsync(
                user.Email,
                            "Mã OTP xác nhận tài khoản",
                $@"
                <div style='font-family: Arial, sans-serif; max-width: 600px; margin: auto; padding: 20px; background-color: #FAF9F7;'>
                    <h2 style='color: #124DA3; border-bottom: 2px solid #F37022; padding-bottom: 10px;'>Mã OTP của bạn</h2>
                    
                    <p style='color: #333;'>Xin chào,</p>
                    
                    <p style='color: #333;'>Mã OTP một lần để xác nhận tài khoản của bạn là:</p>
                    
                    <div style='font-size: 24px; font-weight: bold; letter-spacing: 2px; 
                                color: #124DA3; background: white; padding: 15px; 
                                display: inline-block; margin: 15px 0; border-radius: 4px;
                                border: 2px dashed #F37022;'>
                        {OTP}
                    </div>
                    
                    <p style='color: #333;'>Mã này có hiệu lực trong <strong style='color: #124DA3;'>2 phút</strong>.</p>
                    
                    <a href='#' style='display: inline-block; background-color: #F37022; color: white; 
                                      padding: 10px 20px; text-decoration: none; border-radius: 4px;
                                      margin: 15px 0; font-weight: bold;'>
                        Xác nhận ngay
                    </a>
                    
                    <p style='color: #ff0000;'><strong>⚠️ Lưu ý:</strong> Vui lòng không chia sẻ mã này với bất kỳ ai.</p>
                    
                    <p style='color: #4EB748; font-style: italic;'>
                        <strong>✔️ Thành công:</strong> Yêu cầu OTP của bạn đã được tạo!
                    </p>
                    
                    <p style='color: #333; font-size: 14px;'>
                        Nếu bạn không yêu cầu mã này, vui lòng bỏ qua email này.
                    </p>
                    
                    <div style='margin-top: 30px; padding-top: 15px; border-top: 1px solid #eee;'>
                        <p style='color: #124DA3; font-weight: bold;'>Đội ngũ hỗ trợ ShuttleMate </p>
                        <p style='color: #124DA3; font-weight: bold;'>Mọi chi tiết xin liên hệ: shuttlemate.service@gmail.com</p>
                        <p style='font-size: 12px; color: #999;'>
                            © {DateTime.Now.Year} ShuttleMate . Bảo lưu mọi quyền.
                        </p>
                    </div>
                </div>
                "
            );
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

            UserRole roleUser = _unitOfWork.GetRepository<UserRole>().Entities.Where(x => x.UserId == user.Id).FirstOrDefault()
                                ?? throw new ErrorException(StatusCodes.Status404NotFound, ErrorCode.NotFound, "Không tìm thấy tài khoản");
            string roleName = _unitOfWork.GetRepository<Role>().Entities.Where(x => x.Id == roleUser.RoleId).Select(x => x.Name).FirstOrDefault()
             ?? "unknow";
            var tokenResponse = _tokenService.GenerateTokens(user, roleName);

            //var token = Authentication.CreateToken(user.Id.ToString(), _jwtSettings);
            var loginResponse = new LoginResponse
            {
                TokenResponse = tokenResponse,
                //Role = roleName,
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
                //Role = roleName
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
    }
}
