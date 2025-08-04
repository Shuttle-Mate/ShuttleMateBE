using ShuttleMate.Core.Bases;
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
        Task<BasePaginatedList<AdminResponseUserModel>> GetAllAsync(int page = 0, int pageSize = 10, string? name = null, bool? gender = null, string? roleName = null, bool? Violate = null, string? email = null, string? phone = null, Guid? schoolId = null, Guid? parentId = null);
        Task<BasePaginatedList<ResponseStudentInRouteAndShiftModel>> GetStudentInRouteAndShift(int page = 0, int pageSize = 10, Guid? routeId = null, Guid? schoolShiftId = null);
        Task AssignParent(AssignParentModel model);
        Task AssignParentForParent(AssignParentForStudentModel model);
        Task CreateUserAdmin(CreateUserAdminModel model);
        Task<UserInforModel> GetById(Guid userId);
        Task<IEnumerable<ReponseYourChild>> GetYourChild(Guid Id);
        Task RemoveParent();
        Task RemoveStudent(RemoveStudentModel model);
    }
}
