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
        /// Trang hiện tại (bắt đầu từ 1)
        /// </summary>
        public int Page { get; set; }
        /// <summary>
        /// Số lượng bản ghi mỗi trang.
        /// </summary>
        public int PageSize { get; set; }
    }

    public class PaginationResp<T>
    {
        public List<T> Items { get; set; } = [];
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalRecords { get; set; }
    }
}
