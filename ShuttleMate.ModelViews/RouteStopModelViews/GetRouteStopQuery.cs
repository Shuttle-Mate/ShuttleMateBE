using ShuttleMate.ModelViews.Pagination;

namespace ShuttleMate.ModelViews.RouteStopModelViews
{
    public class GetRouteStopQuery : PaginationReq
    {
        /// <summary>
        /// Vĩ độ của vị trí hiện tại. Bắt buộc.
        /// </summary>
        public double Lat { get; set; }
        /// <summary>
        /// Kinh độ của vị trí hiện tại. Bắt buộc.
        /// </summary>
        public double Lng { get; set; }
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
