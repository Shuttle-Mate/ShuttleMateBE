using ShuttleMate.ModelViews.Pagination;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShuttleMate.ModelViews.TripModelViews
{
    public class GetTripQuery : PaginationReq
    {
        public string? search { get; set; } = null;
    }
}
