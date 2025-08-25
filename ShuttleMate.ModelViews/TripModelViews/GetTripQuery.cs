using ShuttleMate.ModelViews.Pagination;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static ShuttleMate.Contract.Repositories.Enum.GeneralEnum;

namespace ShuttleMate.ModelViews.TripModelViews
{
    public class GetTripQuery : PaginationReq
    {
        /// <summary>
        /// filter theo ngày (ngày bắt đầu) format: 2025-07-23
        /// </summary>
        public string? startDate { get; set; } = null!;
        /// <summary>
        /// filter theo ngày (ngày kết thúc) Note: nếu truyền 1 trong 2
        /// startDate = từ ngày đó đổ về sau
        /// endDate = từ ngày đó đổ về trước
        /// </summary>
        public string? endDate { get; set; } = null!;
        /// <summary>
        /// Filter theo Status (SCHEDULED, IN_PROGRESS, COMPLETED, CANCELLED)
        /// </summary>
        public string? status { get; set; } = null!;
    }
}
