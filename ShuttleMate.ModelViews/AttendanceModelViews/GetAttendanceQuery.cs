using ShuttleMate.ModelViews.Pagination;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShuttleMate.ModelViews.AttendanceModelViews
{
    public class GetAttendanceQuery : PaginationReq
    {
        ///// <summary>
        ///// Từ khoá tìm kiếm (lọc theo tên, màu, hãng, model). Tùy chọn.
        ///// </summary>
        //public string? search { get; set; } = null!;
        /// <summary>
        /// Lọc theo TripId
        /// </summary>
        public Guid? tripId { get; set; } = null!;
    }
}
