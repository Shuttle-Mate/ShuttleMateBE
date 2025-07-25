using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using ShuttleMate.Core.Bases;
using ShuttleMate.ModelViews.AttendanceModelViews;
using ShuttleMate.ModelViews.ShuttleModelViews;

namespace ShuttleMate.Contract.Services.Interfaces
{
    public interface IAttendanceService
    {
        Task<List<ResponseAttendanceModel>> GetMyAttendance(DateTime? fromDate, DateTime? toDate);
        Task CheckIn(CheckInModel model);
        Task CheckOut(CheckOutModel model);
        Task BulkCheckOutByTrip(Guid tripId, string checkOutLocation, string? notes = null);
        Task<BasePaginatedList<ResponseAttendanceModel>> GetAll(GetAttendanceQuery query);
        Task<ResponseAttendanceModel> GetById(Guid attendanceId);
        //Task UpdateAttendance(UpdateAttendanceModel model);
        Task DeleteAttendance(Guid attendanceId);
    }
}
