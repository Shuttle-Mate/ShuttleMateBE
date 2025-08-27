using ShuttleMate.Contract.Repositories.Entities;
using ShuttleMate.Core.Bases;
using ShuttleMate.ModelViews.AuthModelViews;
using ShuttleMate.ModelViews.SchoolModelView;
using ShuttleMate.ModelViews.UserModelViews;

namespace ShuttleMate.Contract.Services.Interfaces
{
    public interface IUserService
    {
        Task<string> BlockUserForAdmin(Guid userId);
        Task<string> UnBlockUserForAdmin(Guid userId);
        Task AssignUserToRoleAsync(Guid userId, Guid roleId);
        Task<UserInforModel> GetInfor();
        Task RemoveUserToRoleAsync(Guid userId);
        Task UpdateProfiel(Guid? userId = null, UpdateProfileModel? model = null);
        Task<BasePaginatedList<AdminResponseUserModel>> GetAllAsync(int page = 0, int pageSize = 10, string? name = null, bool? gender = null, string? roleName = null, bool? Violate = null, string? email = null, string? phone = null, Guid? schoolId = null, Guid? parentId = null);
        Task<BasePaginatedList<ResponseStudentInRouteAndShiftModel>> GetStudentInRouteAndShift(int page = 0, int pageSize = 10, Guid? routeId = null, Guid? schoolShiftId = null, string? search = null);
        Task AssignParent(Guid studentId, AssignParentModel model);
        Task AssignStudent(Guid parentId, AssignStudentModel model);
        Task CreateUserAdmin(CreateUserAdminModel model);
        Task UpdateSchoolForUser(Guid? studentId = null, UpdateSchoolForUserModel? model = null);
        Task<UserInforModel> GetById(Guid userId);
        Task<IEnumerable<ReponseYourChild>> GetYourChild(Guid Id);
        Task RemoveParent();
        Task DeleteUser(Guid userId);
        Task AssignSchoolForManager(AssignSchoolForManagerModel model);
        Task RemoveStudent(RemoveStudentModel model);
    }
}
