using ShuttleMate.ModelViews.SchoolShiftModelViews;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShuttleMate.Contract.Services.Interfaces
{
    public interface ISchoolShiftService
    {
        Task CreateSchoolShift(CreateSchoolShiftModel model);
        Task UpdateSchoolShift(UpdateSchoolShiftModel model);
        Task DeleteSchoolShift(DeleteSchoolShiftModel model);
    }
}
