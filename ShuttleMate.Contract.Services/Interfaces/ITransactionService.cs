using ShuttleMate.ModelViews.TransactionModelView;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static ShuttleMate.Contract.Repositories.Enum.GeneralEnum;

namespace ShuttleMate.Contract.Services.Interfaces
{
    public interface ITransactionService
    {
        Task<IEnumerable<TransactionResponseModel>> GetAllForUserAsync(PaymentMethodEnum? paymentMethodEnum, PaymentStatus? paymentStatus = null, int? orderCode = null, string? description = null, bool? CreateTime = null);
    }
}
