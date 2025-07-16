using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ShuttleMate.ModelViews.Pagination;
using static ShuttleMate.Contract.Repositories.Enum.GeneralEnum;

namespace ShuttleMate.ModelViews.ShuttleModelViews
{
    /// <summary>
    /// Query phân trang danh sách xe
    /// </summary>
    public class GetShuttleQuery : PaginationReq
    {
        /// <summary>
        /// Từ khoá tìm kiếm (lọc theo tên, màu, hãng, model). Tùy chọn.
        /// </summary>
        public string? SearchKeyword { get; set; } = null!;
        /// <summary>
        /// Tiêu chí sắp xếp: Name = 0, Brand = 1, InsuranceExpiryDate = 2, SeatCount (default).
        /// </summary>
        public ShuttleSortByEnum? SortBy { get; set; } = null!;
        /// <summary>
        /// Lọc theo trạng thái hoạt động (IsActive). Tùy chọn.
        /// </summary>
        public bool? IsActive { get; set; } = null!;
        /// <summary>
        /// Lọc theo tình trạng sẵn sàng (IsAvailable). Tùy chọn.
        /// </summary>
        public bool? IsAvailable { get; set; } = null!;
    }
}
