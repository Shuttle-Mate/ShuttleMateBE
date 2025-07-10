using ShuttleMate.ModelViews.AuthModelViews;
using ShuttleMate.ModelViews.UserModelViews;

namespace ShuttleMate.Contract.Services.Interfaces
{
    public interface IUserService
    {
        Task<string> BlockUserForAdmin(BlockUserForAdminModel model);
        Task<string> UnBlockUserForAdmin(UnBlockUserForAdminModel model);
        Task AssignUserToRoleAsync(Guid userId, Guid roleId);
        Task<UserInforModel> GetInfor();
        Task RemoveUserToRoleAsync(Guid userId);
        Task UpdateProfiel(UpdateProfileModel model);
        Task<IEnumerable<AdminResponseUserModel>> GetAllAsync(Guid? roleId = null, string? name = null, bool? gender= null);
        Task AssignParent(AssignParentModel model);
        Task AssignParentForParent(AssignParentForStudentModel model);
        Task CreateUserAdmin(CreateUserAdminModel model);
        Task<UserResponseModel> GetById(Guid userId);
    }
}
