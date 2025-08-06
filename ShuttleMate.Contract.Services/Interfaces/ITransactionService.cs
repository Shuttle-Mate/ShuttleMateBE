using ShuttleMate.Core.Bases;
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
        Task<BasePaginatedList<TransactionResponseModel>> GetAllForUserAsync(int page = 0, int pageSize = 10 , string? paymentStatus = null, int? orderCode = null, string? description = null, bool? CreateTime = null);
        Task<BasePaginatedList<TransactionAdminResponseModel>> GetAllForAdminAsync(int page = 0, int pageSize = 10,  string? paymentStatus = null, int? orderCode = null, string? description = null, bool? CreateTime = null, Guid? userId= null);
        Task<TransactionResponseModel> GetById(Guid transactionId);
    }
}
