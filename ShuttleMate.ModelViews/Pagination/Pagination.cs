using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShuttleMate.ModelViews.Pagination
{
    /// <summary>
    /// Thông tin phân trang
    /// </summary>
    public class PaginationReq
    {
        /// <summary>
        /// Trang hiện tại (bắt đầu từ 0)
        /// </summary>
        public int page { get; set; } = 0;
        /// <summary>
        /// Số lượng bản ghi mỗi trang.
        /// </summary>
        public int pageSize { get; set; } = 10;
    }

    public class PaginationResp<T>
    {
        public List<T> Items { get; set; } = [];
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalRecords { get; set; }
    }
}
