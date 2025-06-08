using ShuttleMate.ModelViews.UserModelViews;

namespace ShuttleMate.Contract.Services.Interfaces
{
    public interface IUserService
    {
        Task<string> BlockUserForAdmin(BlockUserForAdminModel model);
        Task<string> UnBlockUserForAdmin(UnBlockUserForAdminModel model);
        Task AssignUserToRoleAsync(Guid userId, Guid roleId);
        Task<UserInforModel> GetInfor();
    }
}
