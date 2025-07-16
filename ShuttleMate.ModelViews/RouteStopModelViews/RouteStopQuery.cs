using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ShuttleMate.ModelViews.Pagination;

namespace ShuttleMate.ModelViews.RouteStopModelViews
{
    public class RouteStopQuery : PaginationReq
    {
        /// <summary>
        /// Từ khoá tìm kiếm theo tên trạm. Tùy chọn.
        /// </summary>
        public string? SearchStopName { get; set; } = null!;
    }
}
