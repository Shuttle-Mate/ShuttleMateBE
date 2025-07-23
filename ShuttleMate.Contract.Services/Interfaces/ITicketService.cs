using ShuttleMate.Core.Bases;
using ShuttleMate.ModelViews.TicketTypeModelViews;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static ShuttleMate.Contract.Repositories.Enum.GeneralEnum;

namespace ShuttleMate.Contract.Services.Interfaces
{
    public interface ITicketService
    {
        Task<BasePaginatedList<TicketResponseModel>> GetAllAsync(int page = 0, int pageSize = 10, string? type = null, string? routeName = null, bool? price = null, Decimal? lowerBound = null, Decimal? upperBound = null, Guid? routeId = null);
        Task<TicketResponseModel> GetById(Guid Id);
        Task CreateTicket(CreateTicketModel model);
        Task UpdateTicket(UpdateTicketModel model);
        Task DeleteTicket(DeleteTicketModel model);
    }
}
