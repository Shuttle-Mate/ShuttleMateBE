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
        Task DeleteSchool(DeleteSchoolModel model);
        Task UpdateSchool(UpdateSchoolModel model);
        Task CreateSchool(CreateSchoolModel model);
        Task<IEnumerable<SchoolResponseModel>> GetAllAsync(string? name = null, string? adress = null, TimeOnly? schoolTime = null);
        Task<SchoolResponseModel> GetById(Guid id);
    }
}
