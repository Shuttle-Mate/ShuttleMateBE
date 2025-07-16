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
    public interface ITicketTypeService
    {
        Task<BasePaginatedList<TicketTypeResponseModel>> GetAllAsync(int page = 0, int pageSize = 10, string? type = null, string? routeName = null, bool? price = null, Decimal? lowerBound = null, Decimal? upperBound = null, Guid? routeId = null);
        Task<TicketTypeResponseModel> GetById(Guid Id);
        Task CreateTicketType(CreateTicketTypeModel model);
        Task UpdateTicketType(UpdateTicketTypeModel model);
        Task DeleteTicketType(DeleteTicketTypeModel model);
    }
}
