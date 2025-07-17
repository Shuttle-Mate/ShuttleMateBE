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
        public string? search { get; set; } = null!;
        /// <summary>
        /// Tiêu chí sắp xếp: NAME, BRAND, INSURANCE_EXPIRY_DATE, SeatCount (default).
        /// </summary>
        public string? sortBy { get; set; } = null!;
        /// <summary>
        /// Lọc theo trạng thái hoạt động (IsActive). Tùy chọn.
        /// </summary>
        public bool? isActive { get; set; } = null!;
        /// <summary>
        /// Lọc theo tình trạng sẵn sàng (IsAvailable). Tùy chọn.
        /// </summary>
        public bool? isAvailable { get; set; } = null!;
    }
}
