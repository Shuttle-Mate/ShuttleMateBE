using ShuttleMate.Core.Bases;
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
        Task<CreateHistoryTicketResponse> CreateHistoryTicket(CreateHistoryTicketModel? model = null);
        Task<CreateHistoryTicketResponse> CreateHistoryTicketForParent(CreateHistoryTicketForParentModel model);
        Task<BasePaginatedList<HistoryTicketResponseModel>> GetAllForUserAsync(int page = 0, int pageSize = 10, string? status = null, DateTime? PurchaseAt = null, bool? CreateTime = null, DateOnly? ValidFrom = null, DateOnly? ValidUntil = null, Guid? ticketId = null, string? ticketType = null);
        Task<BasePaginatedList<HistoryTicketResponseModel>> GetAllForParentAsync(int page = 0, int pageSize = 10, string? status = null, DateTime? PurchaseAt = null, bool? CreateTime = null, DateOnly? ValidFrom = null, DateOnly? ValidUntil = null, Guid? ticketId = null, Guid? studentId = null, string? ticketType = null);
        Task<BasePaginatedList<HistoryTicketAdminResponseModel>> GetAllForAdminAsync(int page = 0, int pageSize = 10, string? status = null, DateTime? PurchaseAt = null, bool? CreateTime = null, DateOnly? ValidFrom = null, DateOnly? ValidUntil = null, Guid? userId = null, Guid? ticketId = null, string? ticketType = null);
        Task PayOSCallback(PayOSWebhookRequest request);
        Task<string> CreateZaloPayOrder(CreateZaloPayOrderModel model);
        Task<string> ResponseHistoryTicketStatus(Guid historyTicketId);
        Task CancelTicket(Guid historyTicketId);
    }
}
