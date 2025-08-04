using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ShuttleMate.ModelViews.Pagination;

namespace ShuttleMate.ModelViews.RouteStopModelViews
{
    public class GetRouteStopQuery : PaginationReq
    {
        /// <summary>
        /// Từ khoá tìm kiếm theo tên trạm. Tùy chọn.
        /// </summary>
        public string? search { get; set; } = null!;
        /// <summary>
        /// filter theo school
        /// </summary>
        public Guid? SchoolId { get; set; }
    }
}
