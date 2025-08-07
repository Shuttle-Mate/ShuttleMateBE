using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using ShuttleMate.Contract.Repositories.Entities;
using ShuttleMate.Contract.Repositories.IUOW;
using ShuttleMate.Contract.Services.Interfaces;
using ShuttleMate.Core.Bases;
using ShuttleMate.Core.Constants;
using ShuttleMate.Core.Utils;
using ShuttleMate.ModelViews.WithdrawalRequestModelViews;
using ShuttleMate.Services.Services.Infrastructure;
using static ShuttleMate.Contract.Repositories.Enum.GeneralEnum;

namespace ShuttleMate.Services.Services
{
    public class WithdrawalRequestService : IWithdrawalRequestService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IHttpContextAccessor _contextAccessor;

        public WithdrawalRequestService(IUnitOfWork unitOfWork, IMapper mapper, IHttpContextAccessor contextAccessor)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _contextAccessor = contextAccessor;
        }

        public async Task<BasePaginatedList<ResponseWithdrawalRequestModel>> GetAllAsync(
            string? status,
            Guid? userId,
            bool sortAsc = false,
            int page = 0,
            int pageSize = 10)
        {
            var query = _unitOfWork.GetRepository<WithdrawalRequest>()
                .GetQueryable()
                .Include(x => x.User)
                .Where(x => !x.DeletedTime.HasValue);

            if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<WithdrawalRequestStatusEnum>(status, true, out var parsedStatus))
            {
                query = query.Where(x => x.Status == parsedStatus);
            }

            if (userId.HasValue)
                query = query.Where(f => f.UserId == userId.Value);

            query = sortAsc
                ? query.OrderBy(x => x.CreatedTime)
                : query.OrderByDescending(x => x.CreatedTime);

            var totalCount = await query.CountAsync();

            var pagedItems = await query
                .Skip(page * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var result = _mapper.Map<List<ResponseWithdrawalRequestModel>>(pagedItems);

            return new BasePaginatedList<ResponseWithdrawalRequestModel>(result, totalCount, page, pageSize);
        }

        public async Task<ResponseWithdrawalRequestModel> GetByIdAsync(Guid withdrawalRequestId)
        {
            var withdrawalRequest = await _unitOfWork.GetRepository<WithdrawalRequest>().GetByIdAsync(withdrawalRequestId)
                ?? throw new ErrorException(StatusCodes.Status404NotFound, ResponseCodeConstants.NOT_FOUND, "Yêu cầu hoàn tiền không tồn tại.");

            if (withdrawalRequest.DeletedTime.HasValue)
                throw new ErrorException(StatusCodes.Status404NotFound, ResponseCodeConstants.NOT_FOUND, "Yêu cầu hoàn tiền đã bị xóa.");

            return _mapper.Map<ResponseWithdrawalRequestModel>(withdrawalRequest);
        }

        public async Task CreateAsync(CreateWithdrawalRequestModel model)
        {
            var userId = Authentication.GetUserIdFromHttpContextAccessor(_contextAccessor);
            Guid.TryParse(userId, out Guid userIdGuid);
            model.TrimAllStrings();

            var existingWithdrawals = await _unitOfWork.GetRepository<WithdrawalRequest>().FindAsync(x => x.TransactionId == model.TransactionId && !x.DeletedTime.HasValue);

            if (existingWithdrawals != null)
                throw new ErrorException(StatusCodes.Status400BadRequest, ResponseCodeConstants.BADREQUEST, "Đã tồn tại yêu cầu hoàn tiền cho giao dịch này.");

            var transaction = await _unitOfWork.GetRepository<Transaction>().GetByIdAsync(model.TransactionId)
                ?? throw new ErrorException(StatusCodes.Status404NotFound, ResponseCodeConstants.NOT_FOUND, "Giao dịch không tồn tại.");

            if (transaction.DeletedTime.HasValue)
                throw new ErrorException(StatusCodes.Status404NotFound, ResponseCodeConstants.NOT_FOUND, "Giao dịch đã bị xóa.");

            if (transaction.Status != PaymentStatus.PAID)
                throw new ErrorException(StatusCodes.Status400BadRequest, ResponseCodeConstants.BADREQUEST, "Giao dịch phải đang ở trạng thái đã thanh toán.");

            if (string.IsNullOrWhiteSpace(model.BankAccount))
                throw new ErrorException(StatusCodes.Status400BadRequest, ResponseCodeConstants.BADREQUEST, "Vui lòng điền số tài khoản ngân hàng.");

            if (string.IsNullOrWhiteSpace(model.BankAccountName))
                throw new ErrorException(StatusCodes.Status400BadRequest, ResponseCodeConstants.BADREQUEST, "Vui lòng điền tên tài khoản ngân hàng.");

            if (string.IsNullOrWhiteSpace(model.BankName))
                throw new ErrorException(StatusCodes.Status400BadRequest, ResponseCodeConstants.BADREQUEST, "Vui lòng điền tên ngân hàng.");

            var newWithdrawalRequest = _mapper.Map<WithdrawalRequest>(model);
            newWithdrawalRequest.Amount = transaction.Amount;
            newWithdrawalRequest.Status = WithdrawalRequestStatusEnum.IN_PROGRESS;
            newWithdrawalRequest.UserId = userIdGuid;
            newWithdrawalRequest.CreatedBy = userId;
            newWithdrawalRequest.LastUpdatedBy = userId;

            await _unitOfWork.GetRepository<WithdrawalRequest>().InsertAsync(newWithdrawalRequest);
            await _unitOfWork.SaveAsync();
        }

        public async Task UpdateAsync(Guid withdrawalRequestId, UpdateWithdrawalRequestModel model)
        {
            string userId = Authentication.GetUserIdFromHttpContextAccessor(_contextAccessor);
            model.TrimAllStrings();

            var withdrawalRequest = await _unitOfWork.GetRepository<WithdrawalRequest>().GetByIdAsync(withdrawalRequestId)
                ?? throw new ErrorException(StatusCodes.Status404NotFound, ResponseCodeConstants.NOT_FOUND, "Yêu cầu hoàn tiền không tồn tại.");

            if (withdrawalRequest.DeletedTime.HasValue)
                throw new ErrorException(StatusCodes.Status404NotFound, ResponseCodeConstants.NOT_FOUND, "Yêu cầu hoàn tiền đã bị xóa.");

            if (withdrawalRequest.Status == WithdrawalRequestStatusEnum.COMPLETED)
                throw new ErrorException(StatusCodes.Status404NotFound, ResponseCodeConstants.NOT_FOUND, "Yêu cầu hoàn tiền đã hoàn thành.");

            if (withdrawalRequest.Status == WithdrawalRequestStatusEnum.REJECTED)
                throw new ErrorException(StatusCodes.Status404NotFound, ResponseCodeConstants.NOT_FOUND, "Yêu cầu hoàn tiền đã bị từ chối.");

            if (string.IsNullOrWhiteSpace(model.BankAccount))
                throw new ErrorException(StatusCodes.Status400BadRequest, ResponseCodeConstants.BADREQUEST, "Vui lòng điền số tài khoản ngân hàng.");

            if (string.IsNullOrWhiteSpace(model.BankAccountName))
                throw new ErrorException(StatusCodes.Status400BadRequest, ResponseCodeConstants.BADREQUEST, "Vui lòng điền tên tài khoản ngân hàng.");

            if (string.IsNullOrWhiteSpace(model.BankName))
                throw new ErrorException(StatusCodes.Status400BadRequest, ResponseCodeConstants.BADREQUEST, "Vui lòng điền tên ngân hàng.");

            _mapper.Map(model, withdrawalRequest);
            withdrawalRequest.LastUpdatedBy = userId;
            withdrawalRequest.LastUpdatedTime = CoreHelper.SystemTimeNow;

            await _unitOfWork.GetRepository<WithdrawalRequest>().UpdateAsync(withdrawalRequest);
            await _unitOfWork.SaveAsync();
        }

        public async Task CompleteAsync(Guid withdrawalRequestId)
        {
            string userId = Authentication.GetUserIdFromHttpContextAccessor(_contextAccessor);

            var withdrawalRequest = await _unitOfWork.GetRepository<WithdrawalRequest>().GetByIdAsync(withdrawalRequestId)
                ?? throw new ErrorException(StatusCodes.Status404NotFound, ResponseCodeConstants.NOT_FOUND, "Yêu cầu hoàn tiền không tồn tại.");

            if (withdrawalRequest.DeletedTime.HasValue)
                throw new ErrorException(StatusCodes.Status404NotFound, ResponseCodeConstants.NOT_FOUND, "Yêu cầu hoàn tiền đã bị xóa.");

            if (withdrawalRequest.Status == WithdrawalRequestStatusEnum.COMPLETED)
                throw new ErrorException(StatusCodes.Status404NotFound, ResponseCodeConstants.NOT_FOUND, "Yêu cầu hoàn tiền đã hoàn thành.");

            if (withdrawalRequest.Status == WithdrawalRequestStatusEnum.REJECTED)
                throw new ErrorException(StatusCodes.Status404NotFound, ResponseCodeConstants.NOT_FOUND, "Yêu cầu hoàn tiền đã bị từ chối.");

            withdrawalRequest.Status = WithdrawalRequestStatusEnum.COMPLETED;
            withdrawalRequest.LastUpdatedTime = CoreHelper.SystemTimeNow;
            withdrawalRequest.LastUpdatedBy = userId;

            await _unitOfWork.GetRepository<WithdrawalRequest>().UpdateAsync(withdrawalRequest);
            await _unitOfWork.SaveAsync();
        }

        public async Task RejectAsync(Guid withdrawalRequestId, RejectWithdrawalRequestModel model)
        {
            string userId = Authentication.GetUserIdFromHttpContextAccessor(_contextAccessor);

            var withdrawalRequest = await _unitOfWork.GetRepository<WithdrawalRequest>().GetByIdAsync(withdrawalRequestId)
                ?? throw new ErrorException(StatusCodes.Status404NotFound, ResponseCodeConstants.NOT_FOUND, "Yêu cầu hoàn tiền không tồn tại.");

            if (withdrawalRequest.DeletedTime.HasValue)
                throw new ErrorException(StatusCodes.Status404NotFound, ResponseCodeConstants.NOT_FOUND, "Yêu cầu hoàn tiền đã bị xóa.");

            if (withdrawalRequest.Status == WithdrawalRequestStatusEnum.COMPLETED)
                throw new ErrorException(StatusCodes.Status404NotFound, ResponseCodeConstants.NOT_FOUND, "Yêu cầu hoàn tiền đã hoàn thành.");

            if (withdrawalRequest.Status == WithdrawalRequestStatusEnum.REJECTED)
                throw new ErrorException(StatusCodes.Status404NotFound, ResponseCodeConstants.NOT_FOUND, "Yêu cầu hoàn tiền đã bị từ chối.");

            if (string.IsNullOrWhiteSpace(model.Reason))
                throw new ErrorException(StatusCodes.Status400BadRequest, ResponseCodeConstants.BADREQUEST, "Vui lòng điền lý do từ chối.");

            withdrawalRequest.RejectReason = model.Reason;
            withdrawalRequest.Status = WithdrawalRequestStatusEnum.REJECTED;
            withdrawalRequest.LastUpdatedTime = CoreHelper.SystemTimeNow;
            withdrawalRequest.LastUpdatedBy = userId;

            await _unitOfWork.GetRepository<WithdrawalRequest>().UpdateAsync(withdrawalRequest);
            await _unitOfWork.SaveAsync();
        }

        public async Task DeleteAsync(Guid withdrawalRequestId)
        {
            string userId = Authentication.GetUserIdFromHttpContextAccessor(_contextAccessor);

            var withdrawalRequest = await _unitOfWork.GetRepository<WithdrawalRequest>().GetByIdAsync(withdrawalRequestId)
                ?? throw new ErrorException(StatusCodes.Status404NotFound, ResponseCodeConstants.NOT_FOUND, "Yêu cầu hoàn tiền không tồn tại.");

            if (withdrawalRequest.DeletedTime.HasValue)
                throw new ErrorException(StatusCodes.Status404NotFound, ResponseCodeConstants.NOT_FOUND, "Yêu cầu hoàn tiền đã bị xóa.");

            if (withdrawalRequest.Status == WithdrawalRequestStatusEnum.COMPLETED)
                throw new ErrorException(StatusCodes.Status404NotFound, ResponseCodeConstants.NOT_FOUND, "Yêu cầu hoàn tiền đã hoàn thành.");

            if (withdrawalRequest.Status == WithdrawalRequestStatusEnum.REJECTED)
                throw new ErrorException(StatusCodes.Status404NotFound, ResponseCodeConstants.NOT_FOUND, "Yêu cầu hoàn tiền đã bị từ chối.");

            withdrawalRequest.LastUpdatedTime = CoreHelper.SystemTimeNow;
            withdrawalRequest.LastUpdatedBy = userId;
            withdrawalRequest.DeletedTime = CoreHelper.SystemTimeNow;
            withdrawalRequest.DeletedBy = userId;

            await _unitOfWork.GetRepository<WithdrawalRequest>().UpdateAsync(withdrawalRequest);
            await _unitOfWork.SaveAsync();
        }
    }
}
