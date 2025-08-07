using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static ShuttleMate.Contract.Repositories.Enum.GeneralEnum;

namespace ShuttleMate.ModelViews.TicketTypeModelViews
{
    public class UpdateTicketModel
    {
        public Guid RouteId { get; set; }
        public string Type { get; set; }
        public decimal Price { get; set; }
    }
}
