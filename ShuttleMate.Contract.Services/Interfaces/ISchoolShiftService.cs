using ShuttleMate.Core.Bases;
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
        Task<BasePaginatedList<ResponseSchoolShiftListByTicketIdMode>> GetAllSchoolShift(int page = 0, int pageSize = 10, string? sessionType = null, string? shiftType = null, bool sortAsc = false);
        Task<BasePaginatedList<ResponseSchoolShiftListByTicketIdMode>> GetAllSchoolShiftForAdmin(int page = 0, int pageSize = 10, string? sessionType = null, string? shiftType = null, bool sortAsc = false, Guid? schoolId = null);
        Task<List<ResponseSchoolShiftListByTicketIdMode>> GetSchoolShiftListByTicketId(Guid ticketId);
        Task CreateSchoolShift(CreateSchoolShiftModel model);
        Task UpdateSchoolShift(UpdateSchoolShiftModel model);
        Task DeleteSchoolShift(DeleteSchoolShiftModel model);
    }
}
