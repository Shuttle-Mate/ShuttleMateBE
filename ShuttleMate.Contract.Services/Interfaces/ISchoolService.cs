using ShuttleMate.Contract.Repositories.Entities;
using ShuttleMate.Core.Bases;
using ShuttleMate.ModelViews.SchoolModelView;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShuttleMate.Contract.Services.Interfaces
{
    public interface ISchoolService
    {
        Task CreateSchool(CreateSchoolModel model);
        Task UpdateSchool(Guid id, UpdateSchoolModel model);
        Task DeleteSchool(Guid schoolId);
        Task AssignSchoolForManager(AssignSchoolForManagerModel model);
        Task<BasePaginatedList<SchoolResponseModel>> GetAllAsync(int page = 0, int pageSize = 10, string? search = null, bool? isActive = null, bool sortAsc = false);
        Task<BasePaginatedList<ListStudentInSchoolResponse>> GetAllStudentInSchool(int page = 0, int pageSize = 10, string? search = null, bool sortAsc = false, Guid? schoolShiftId = null);
        Task<BasePaginatedList<ListStudentInSchoolResponse>> GetAllStudentInSchoolForAdmin(int page = 0, int pageSize = 10, string? search = null, bool sortAsc = false, Guid? schoolShiftId = null, Guid? schoolId = null);
        Task<BasePaginatedList<RouteToSchoolResponseModel>> GetAllRouteToSchool(int page = 0, int pageSize = 10, string? search = null,  bool sortAsc = false, Guid ? schoolId = null);
        Task SendEmailToSchool(SendEmailToSchoolModel model);
        Task<SchoolResponseModel> GetById(Guid id);
    }
}
