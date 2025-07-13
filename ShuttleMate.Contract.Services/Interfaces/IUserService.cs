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
        Task<IEnumerable<AdminResponseUserModel>> GetAllAsync(string? name = null, bool? gender = null, string? roleName = null, bool? Violate = null, string? email = null, string? phone = null, Guid? schoolId = null, Guid? parentId = null);
        Task AssignParent(AssignParentModel model);
        Task AssignParentForParent(AssignParentForStudentModel model);
        Task CreateUserAdmin(CreateUserAdminModel model);
        Task<UserResponseModel> GetById(Guid userId);
        Task<IEnumerable<ReponseYourChild>> GetYourChild(Guid Id);
    }
}
