using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using ShuttleMate.Contract.Repositories.Entities;
using ShuttleMate.Contract.Repositories.IUOW;
using ShuttleMate.Contract.Services.Interfaces;
using ShuttleMate.Core.Bases;
using ShuttleMate.Core.Constants;
using ShuttleMate.ModelViews.ShuttleModelViews;
using ShuttleMate.Services.Services.Infrastructure;
using static ShuttleMate.Contract.Repositories.Enum.GeneralEnum;

namespace ShuttleMate.Services.Services
{
    public class ShuttleService : IShuttleService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IHttpContextAccessor _contextAccessor;

        public ShuttleService(IUnitOfWork unitOfWork, IMapper mapper, IHttpContextAccessor contextAccessor)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _contextAccessor = contextAccessor;
        }

        public async Task CreateShuttle(ShuttleModel model)
        {
            string userId = Authentication.GetUserIdFromHttpContextAccessor(_contextAccessor);

            Shuttle shuttle = await _unitOfWork.GetRepository<Shuttle>().Entities.FirstOrDefaultAsync(x => x.LicensePlate == model.LicensePlate && !x.DeletedTime.HasValue);
            if (shuttle != null)
            {
                throw new ErrorException(StatusCodes.Status400BadRequest, ErrorCode.BadRequest, "Biển số xe này đã tồn tại!");
            }
            var newShuttle = _mapper.Map<Shuttle>(model);
            newShuttle.CreatedBy = userId;
            newShuttle.LastUpdatedBy = userId;
            await _unitOfWork.GetRepository<Shuttle>().InsertAsync(newShuttle);
            await _unitOfWork.SaveAsync();
        }

        public async Task DeleteShuttle(Guid shuttleId)
        {
            string userId = Authentication.GetUserIdFromHttpContextAccessor(_contextAccessor);

            var shuttle = await _unitOfWork.GetRepository<Shuttle>().Entities.FirstOrDefaultAsync(x => x.Id == shuttleId && !x.DeletedTime.HasValue) ?? throw new ErrorException(StatusCodes.Status404NotFound, ErrorCode.NotFound, "Không tìm thấy xe!");
            if (shuttle.Schedules.Any())
            {
                throw new ErrorException(StatusCodes.Status400BadRequest, ErrorCode.BadRequest, "Không thể xóa xe vì có lịch hoạt động!");
            }
            shuttle.DeletedTime = DateTime.Now;
            shuttle.DeletedBy = userId;
            await _unitOfWork.GetRepository<Shuttle>().UpdateAsync(shuttle);
            await _unitOfWork.SaveAsync();
        }

        public async Task<List<ResponseShuttleModel>> GetAll()
        {
            var shuttles = await _unitOfWork.GetRepository<Shuttle>().Entities.Where(x => !x.DeletedTime.HasValue).OrderBy(x => x.LicensePlate).ToListAsync();
            //if (!shuttles.Any())
            //{
            //    throw new ErrorException(StatusCodes.Status404NotFound, ErrorCode.NotFound, "Không có xe nào tồn tại!");
            //}
            return _mapper.Map<List<ResponseShuttleModel>>(shuttles);
        }

        public async Task<BasePaginatedList<ResponseShuttleModel>> GetAllPaging(GetShuttleQuery req)
        {
            string searchKeyword = req.search ?? "";
            var page = req.page > 0 ? req.page : 0;
            var pageSize = req.pageSize > 0 ? req.pageSize : 10;

            var query = _unitOfWork.GetRepository<Shuttle>().Entities
                        .Where(x => !x.DeletedTime.HasValue);

            if (!string.IsNullOrWhiteSpace(searchKeyword))
            {
                query = query.Where(x =>
                    x.Name.ToLower().Contains(searchKeyword.ToLower()) ||
                    x.Model.ToLower().Contains(searchKeyword.ToLower()) ||
                    x.Color.ToLower().Contains(searchKeyword.ToLower()) ||
                    x.Brand.ToLower().Contains(searchKeyword.ToLower()));
            }

            if (!string.IsNullOrWhiteSpace(req.sortBy) && Enum.TryParse<ShuttleSortByEnum>(req.sortBy, true, out var sortByEnum))
            {
                query = sortByEnum switch
                {
                    ShuttleSortByEnum.NAME => query.OrderBy(c => c.Name),
                    ShuttleSortByEnum.BRAND => query.OrderBy(c => c.Brand),
                    ShuttleSortByEnum.INSURANCE_EXPIRY_DATE => query.OrderBy(c => c.InsuranceExpiryDate),
                    _ => query.OrderBy(c => c.SeatCount)
                               .ThenBy(c => c.LastUpdatedTime)
                               .ThenBy(c => c.Name)
                };
            }
            else
            {
                // Default sorting if no sortBy is provided
                query = query.OrderBy(c => c.SeatCount)
                             .ThenBy(c => c.LastUpdatedTime)
                             .ThenBy(c => c.Name);
            }

            //query = req.sortBy switch
            //{
            //    ShuttleSortByEnum.Name => query.OrderBy(c => c.Name),
            //    ShuttleSortByEnum.Brand => query.OrderBy(c => c.Brand),
            //    ShuttleSortByEnum.InsuranceExpiryDate => query.OrderBy(c => c.InsuranceExpiryDate),
            //    _ => query.OrderBy(c => c.SeatCount)
            //               .OrderBy(c => c.LastUpdatedTime)
            //               .ThenBy(c => c.Name)
            //};

            if (req.isActive.HasValue)
            {
                query = query.Where(x => x.IsActive == req.isActive.Value);
            }

            if (req.isAvailable.HasValue)
            {
                query = query.Where(x => x.IsAvailable == req.isAvailable.Value);
            }

            var totalCount = query.Count();

            //Paging
            var shuttles = await query
                .Skip(req.page * req.pageSize)
                .Take(req.pageSize)
                .ToListAsync();

            //if (!shuttles.Any())
            //{
            //    throw new ErrorException(StatusCodes.Status404NotFound, ErrorCode.NotFound, "Không có xe nào tồn tại!");
            //}

            var result = _mapper.Map<List<ResponseShuttleModel>>(shuttles);

            return new BasePaginatedList<ResponseShuttleModel>(result, totalCount, page, pageSize);
            //return new BasePaginatedList<ResponseShuttleModel>
            //{
            //    Items = _mapper.Map<List<ResponseShuttleModel>>(shuttles),
            //    Page = req.Page,
            //    PageSize = req.PageSize,
            //    TotalRecords = totalCount
            //};
            //return _mapper.Map<List<ResponseShuttleModel>>(shuttles);
        }

        public async Task<ResponseShuttleModel> GetById(Guid shuttleId)
        {
            var shuttle = await _unitOfWork.GetRepository<Shuttle>().Entities.FirstOrDefaultAsync(x => x.Id == shuttleId && !x.DeletedTime.HasValue) ?? throw new ErrorException(StatusCodes.Status404NotFound, ErrorCode.NotFound, "Không tìm thấy xe!");

            return _mapper.Map<ResponseShuttleModel>(shuttle);
        }

        public async Task UpdateShuttle(UpdateShuttleModel model)
        {
            string userId = Authentication.GetUserIdFromHttpContextAccessor(_contextAccessor);

            if (string.IsNullOrWhiteSpace(model.LicensePlate))
            {
                throw new ErrorException(StatusCodes.Status404NotFound, ErrorCode.NotFound, "Biển số xe không được để trống!");
            }
            var shutle = await _unitOfWork.GetRepository<Shuttle>().Entities.FirstOrDefaultAsync(x => x.Id == model.Id && !x.DeletedTime.HasValue) ?? throw new ErrorException(StatusCodes.Status404NotFound, ErrorCode.NotFound, "Không tìm thấy xe!");

            if (model.LicensePlate != shutle.LicensePlate)
            {
                var check = await _unitOfWork.GetRepository<Shuttle>().Entities.FirstOrDefaultAsync(x => x.LicensePlate == model.LicensePlate && !x.DeletedTime.HasValue) ?? throw new ErrorException(StatusCodes.Status404NotFound, ErrorCode.NotFound, "Biển số xe này đã được sử dụng!");
            }
            //route = _mapper.Map<Route>(model);
            _mapper.Map(model, shutle);
            shutle.LastUpdatedBy = userId;
            shutle.LastUpdatedTime = DateTime.Now;
            await _unitOfWork.GetRepository<Shuttle>().UpdateAsync(shutle);
            await _unitOfWork.SaveAsync();
        }
    }
}
