using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ShuttleMate.ModelViews.AttendanceModelViews;
using ShuttleMate.ModelViews.ShuttleModelViews;

namespace ShuttleMate.Contract.Services.Interfaces
{
    public interface IAttendanceService
    {
        Task CheckIn(CheckInModel model);
        Task CheckOut(CheckOutModel model);
        Task<List<ResponseAttendanceModel>> GetAll();
        Task<ResponseAttendanceModel> GetById(Guid attendanceId);
        //Task UpdateAttendance(UpdateAttendanceModel model);
        Task DeleteAttendance(Guid attendanceId);
    }
}
