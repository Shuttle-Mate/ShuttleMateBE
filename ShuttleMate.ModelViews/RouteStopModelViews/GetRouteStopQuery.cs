using ShuttleMate.ModelViews.Pagination;

namespace ShuttleMate.ModelViews.RouteStopModelViews
{
    public class GetRouteStopQuery : PaginationReq
    {
        public string? search { get; set; } = null!;
        /// <summary>
        /// filter theo school
        /// </summary>
        public Guid? schoolId { get; set; }
    }
}
