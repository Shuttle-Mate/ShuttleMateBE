using ShuttleMate.ModelViews.Pagination;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShuttleMate.ModelViews.NotiRecipientModelView
{
    public class GetNotiRecipQuery : PaginationReq
    {
        /// <summary>
        /// Lọc theo RecipientId (userId)
        /// </summary>
        public Guid? userId { get; set; } = null!;
        /// <summary>
        /// Lọc theo trạng thái PENDING, SENT, DELIVERED, READ, FAILED, ARCHIEVED
        /// </summary>
        public string? status { get; set; } = null!;
    }
}