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
    }
}
