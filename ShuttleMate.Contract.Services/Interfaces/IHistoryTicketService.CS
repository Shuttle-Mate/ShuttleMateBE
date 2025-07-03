using ShuttleMate.ModelViews.HistoryTicketModelView;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static ShuttleMate.Contract.Repositories.Enum.GeneralEnum;

namespace ShuttleMate.Contract.Services.Interfaces
{
    public interface IHistoryTicketService
    {
        Task<CreateHistoryTicketResponse> CreateHistoryTicket(CreateHistoryTicketModel model);
        Task<IEnumerable<HistoryTicketResponseModel>> GetAllForUserAsync(HistoryTicketStatus? status, DateTime? PurchaseAt = null, bool? CreateTime = null, DateTime? ValidFrom = null, DateTime? ValidUntil = null, Guid? ticketId = null);
        Task<IEnumerable<HistoryTicketAdminResponseModel>> GetAllForAdminAsync(HistoryTicketStatus? status, DateTime? PurchaseAt = null, bool? CreateTime = null, DateTime? ValidFrom = null, DateTime? ValidUntil = null, Guid? userId = null, Guid? ticketId = null);
        Task PayOSCallback(PayOSWebhookRequest request);
        Task<string> CreateZaloPayOrder(CreateZaloPayOrderModel model);
    }
}
