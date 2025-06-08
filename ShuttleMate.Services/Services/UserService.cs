using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using ShuttleMate.Contract.Repositories.Entities;
using ShuttleMate.Contract.Repositories.IUOW;
using ShuttleMate.Contract.Services.Interfaces;
using ShuttleMate.Core.Bases;
using ShuttleMate.Core.Constants;
using ShuttleMate.ModelViews.UserModelViews;
using ShuttleMate.Services.Services.Infrastructure;

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
        public async Task<string> BlockUserForAdmin(BlockUserForAdminModel model)
        {
            var user = await _unitOfWork.GetRepository<User>().Entities.FirstOrDefaultAsync(x => x.Id == model.UserId && !x.DeletedTime.HasValue) ?? throw new ErrorException(StatusCodes.Status400BadRequest, ErrorCode.BadRequest, "Không tìm thấy người dùng!");
            user.Violate = true;
            await _unitOfWork.GetRepository<User>().UpdateAsync(user);
            await _unitOfWork.SaveAsync();
            // Gửi email thông báo cho user
            await SendBlockUserEmail(user);
            return "Khóa người dùng thành công!";
        }
        public async Task<string> UnBlockUserForAdmin(UnBlockUserForAdminModel model)
        {
            var user = await _unitOfWork.GetRepository<User>().Entities.FirstOrDefaultAsync(x => x.Id == model.UserId && !x.DeletedTime.HasValue) ?? throw new ErrorException(StatusCodes.Status400BadRequest, ErrorCode.BadRequest, "Không tìm thấy người dùng!");
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
