using ShuttleMate.ModelViews.Pagination;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShuttleMate.ModelViews.RouteModelViews
{
    public class GetRouteQuery : PaginationReq
    {
        /// <summary>
        /// Từ khoá tìm kiếm (lọc theo RouteCode, RouteName, Description). Tùy chọn.
        /// </summary>
        public string? search { get; set; } = null!;
        /// <summary>
        /// Tiêu chí sắp xếp: CODE, NAME, PRICE (default UPDATED_AT).
        /// </summary>
        public string? sortBy { get; set; } = null!;
    }
}
