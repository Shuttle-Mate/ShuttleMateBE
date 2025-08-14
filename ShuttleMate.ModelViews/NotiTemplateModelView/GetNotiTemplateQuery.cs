using ShuttleMate.ModelViews.Pagination;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static ShuttleMate.Contract.Repositories.Enum.GeneralEnum;

namespace ShuttleMate.ModelViews.NotiTemplateModelView
{
    public class GetNotiTemplateQuery : PaginationReq
    {
        public string? search { get; set; } = null!;
    }
}
