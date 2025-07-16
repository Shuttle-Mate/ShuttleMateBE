using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ShuttleMate.Contract.Repositories.Entities;
using ShuttleMate.Contract.Repositories.IUOW;
using ShuttleMate.Contract.Services.Interfaces;
using ShuttleMate.Core.Bases;
using ShuttleMate.Core.Constants;
using ShuttleMate.ModelViews.SchoolModelView;
using ShuttleMate.ModelViews.TicketTypeModelViews;
using ShuttleMate.ModelViews.UserModelViews;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;
using static ShuttleMate.Contract.Repositories.Enum.GeneralEnum;

namespace ShuttleMate.Services.Services
{
    public class TicketTypeService : ITicketTypeService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IConfiguration _configuration;
        private readonly IHttpContextAccessor _contextAccessor;
        private readonly IEmailService _emailService;

        public TicketTypeService(IUnitOfWork unitOfWork, IMapper mapper, IConfiguration configuration, IHttpContextAccessor contextAccessor, IEmailService emailService)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _configuration = configuration;
            _contextAccessor = contextAccessor;
            _emailService = emailService;
        }
        public async Task<BasePaginatedList<TicketTypeResponseModel>> GetAllAsync(int page = 0, int pageSize = 10, string? type = null, string? routeName = null, bool? price = null, Decimal? lowerBound = null, Decimal? upperBound = null, Guid ? routeId = null)
        {
            var ticketRepo = _unitOfWork.GetRepository<TicketType>();

            var query = ticketRepo.Entities.Where(x => !x.DeletedTime.HasValue)
                .Include(u => u.Route)
                .AsQueryable();
            if (!string.IsNullOrWhiteSpace(type))
            {
                query = query.Where(u => u.Type.ToString().ToUpper() == type);
            }
            if (routeId != null)
            {
                query = query.Where(u => u.Route.Id.ToString().Contains(routeId.ToString()!));
            }
            if (!string.IsNullOrWhiteSpace(routeName))
            {
                query = query.Where(u => u.Route.RouteName.Contains(routeName));
            }
            if (price != null)
            {
                if (price == true)
                {
                    query = query.OrderBy(x => x.Price);
                }
                else
                {
                    query = query.OrderByDescending(x => x.Price);
                }
            }
            if (lowerBound != null && upperBound != null)
            {
                query = query.Where(x => x.Price <= upperBound && x.Price >= lowerBound);
            }
            else if (lowerBound != null)
            {
                query = query.Where(x => x.Price >= lowerBound);
            }
            else
            {
                query = query.Where(x => x.Price <= upperBound);
            }

            var tickets = await query
                .Select(u => new TicketTypeResponseModel
                {
                    Id = u.Id,
                    Price = u.Price,
                    RouteName = u.Route.RouteName,
                    Type = u.Type.ToString().ToUpper()
                })
                .ToListAsync();
            var totalCount = await query.CountAsync();

            var pagedItems = await query
                .Skip(page * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new BasePaginatedList<TicketTypeResponseModel>(tickets, totalCount, page, pageSize);
        }
        static string ConvertStatusToString(TicketTypeEnum status)
        {
            return status switch
            {
                TicketTypeEnum.DAY_PASS => "Vé ngày",
                TicketTypeEnum.MONTHLY => "Vé tháng",
                TicketTypeEnum.WEEKLY => "Vé tuần",
                TicketTypeEnum.SINGLE_RIDE => "Vé 1 chiều",
                TicketTypeEnum.SEMESTER_ONE => "Vé học kì 1",
                TicketTypeEnum.SEMESTER_TWO => "Vé học kì 2",
                _ => "Không xác định"
            };
        }
        public async Task<TicketTypeResponseModel> GetById(Guid Id)
        {
            var ticketType = await _unitOfWork.GetRepository<TicketType>().Entities.FirstOrDefaultAsync(x => x.Id == Id && !x.DeletedTime.HasValue) ?? throw new ErrorException(StatusCodes.Status404NotFound, ResponseCodeConstants.NOT_FOUND, "Loại vé không tồn tại!");
            var response = new TicketTypeResponseModel
            {
                Id = ticketType.Id,
                Price = ticketType.Price,
                RouteName = ticketType.Route.RouteName,
                Type = ticketType.Type.ToString().ToUpper(),
            };
            return response;
        }
        public async Task CreateTicketType(CreateTicketTypeModel model)
        {
            if (model.Price <= 0)
            {
                throw new ErrorException(StatusCodes.Status404NotFound, ResponseCodeConstants.NOT_FOUND, "Giá loại vé phải lớn hơn 0!");
            }
            var route = await _unitOfWork.GetRepository<Route>().Entities.FirstOrDefaultAsync(x => x.Id == model.RouteId && !x.DeletedTime.HasValue) ?? throw new ErrorException(StatusCodes.Status404NotFound, ResponseCodeConstants.NOT_FOUND, "Tuyến đường không tồn tại!");
            switch (model.Type)
            {
                case "SINGLE_RIDE":
                    var newTicketTypeSINGLE_RIDE = new TicketType
                    {
                        Id = Guid.NewGuid(),
                        CreatedTime = DateTime.Now,
                        Price = model.Price,
                        RouteId = model.RouteId,
                        Type = TicketTypeEnum.SINGLE_RIDE,
                        LastUpdatedTime = DateTime.Now,
                    };
                    await _unitOfWork.GetRepository<TicketType>().InsertAsync(newTicketTypeSINGLE_RIDE);
                    await _unitOfWork.SaveAsync();
                    break;
                case "DAY_PASS":
                    var newTicketTypeDAY_PASS = new TicketType
                    {
                        Id = Guid.NewGuid(),
                        CreatedTime = DateTime.Now,
                        Price = model.Price,
                        RouteId = model.RouteId,
                        Type = TicketTypeEnum.DAY_PASS,
                        LastUpdatedTime = DateTime.Now,
                    };
                    await _unitOfWork.GetRepository<TicketType>().InsertAsync(newTicketTypeDAY_PASS);
                    await _unitOfWork.SaveAsync();
                    break;
                case "WEEKLY":
                    var newTicketTypeWEEKLY = new TicketType
                    {
                        Id = Guid.NewGuid(),
                        CreatedTime = DateTime.Now,
                        Price = model.Price,
                        RouteId = model.RouteId,
                        Type = TicketTypeEnum.WEEKLY,
                        LastUpdatedTime = DateTime.Now,
                    };
                    await _unitOfWork.GetRepository<TicketType>().InsertAsync(newTicketTypeWEEKLY);
                    await _unitOfWork.SaveAsync();
                    break;
                case "MONTHLY":
                    var newTicketTypeMONTHLY = new TicketType
                    {
                        Id = Guid.NewGuid(),
                        CreatedTime = DateTime.Now,
                        Price = model.Price,
                        RouteId = model.RouteId,
                        Type = TicketTypeEnum.MONTHLY,
                        LastUpdatedTime = DateTime.Now,
                    };
                    await _unitOfWork.GetRepository<TicketType>().InsertAsync(newTicketTypeMONTHLY);
                    await _unitOfWork.SaveAsync();
                    break;
                case "SEMESTER_ONE":
                    var newTicketTypeSEMESTER_ONE = new TicketType
                    {
                        Id = Guid.NewGuid(),
                        CreatedTime = DateTime.Now,
                        Price = model.Price,
                        RouteId = model.RouteId,
                        Type = TicketTypeEnum.SEMESTER_ONE,
                        LastUpdatedTime = DateTime.Now,
                    };
                    await _unitOfWork.GetRepository<TicketType>().InsertAsync(newTicketTypeSEMESTER_ONE);
                    await _unitOfWork.SaveAsync();
                    break;
                case "SEMESTER_TWO":
                    var newTicketTypeSEMESTER_TWO = new TicketType
                    {
                        Id = Guid.NewGuid(),
                        CreatedTime = DateTime.Now,
                        Price = model.Price,
                        RouteId = model.RouteId,
                        Type = TicketTypeEnum.SEMESTER_TWO,
                        LastUpdatedTime = DateTime.Now,
                    };
                    await _unitOfWork.GetRepository<TicketType>().InsertAsync(newTicketTypeSEMESTER_TWO);
                    await _unitOfWork.SaveAsync();
                    break;
                default:
                    throw new ErrorException(StatusCodes.Status404NotFound, ResponseCodeConstants.NOT_FOUND, "Không tìm thấy loại vé!");
            }
        }
        public async Task UpdateTicketType(UpdateTicketTypeModel model)
        {

            if (model.Price <= 0)
            {
                throw new ErrorException(StatusCodes.Status404NotFound, ResponseCodeConstants.NOT_FOUND, "Giá loại vé phải lớn hơn 0!");
            }
            var route = await _unitOfWork.GetRepository<Route>().Entities.FirstOrDefaultAsync(x => x.Id == model.RouteId && !x.DeletedTime.HasValue) ?? throw new ErrorException(StatusCodes.Status404NotFound, ResponseCodeConstants.NOT_FOUND, "Tuyến đường không tồn tại!");
            var ticketType = await _unitOfWork.GetRepository<TicketType>().Entities.FirstOrDefaultAsync(x => x.Id == model.TicketTypeId && !x.DeletedTime.HasValue) ?? throw new ErrorException(StatusCodes.Status404NotFound, ResponseCodeConstants.NOT_FOUND, "Loại vé không tồn tại!");

            ticketType.Price = model.Price;
            ticketType.RouteId = model.RouteId;
            switch (model.Type)
            {
                case "SINGLE_RIDE":
                    ticketType.Type = TicketTypeEnum.SINGLE_RIDE;
                    break;
                case "DAY_PASS":
                    ticketType.Type = TicketTypeEnum.DAY_PASS;
                    break;
                case "WEEKLY":
                    ticketType.Type = TicketTypeEnum.WEEKLY;
                    break;
                case "MONTHLY":
                    ticketType.Type = TicketTypeEnum.MONTHLY;
                    break;
                case "SEMESTER_ONE":
                    ticketType.Type = TicketTypeEnum.SEMESTER_ONE;
                    break;
                case "SEMESTER_TWO":
                    ticketType.Type = TicketTypeEnum.SEMESTER_TWO;
                    break;
                default:
                    throw new ErrorException(StatusCodes.Status404NotFound, ResponseCodeConstants.NOT_FOUND, "Không tìm thấy loại vé!");
            }

            await _unitOfWork.GetRepository<TicketType>().UpdateAsync(ticketType);
            await _unitOfWork.SaveAsync();
        }
        public async Task DeleteTicketType(DeleteTicketTypeModel model)
        {
            var ticketType = await _unitOfWork.GetRepository<TicketType>().Entities.FirstOrDefaultAsync(x => x.Id == model.Id && !x.DeletedTime.HasValue) ?? throw new ErrorException(StatusCodes.Status404NotFound, ResponseCodeConstants.NOT_FOUND, "Loại vé không tồn tại!");

            ticketType.DeletedTime = DateTime.Now;

            await _unitOfWork.GetRepository<TicketType>().UpdateAsync(ticketType);
            await _unitOfWork.SaveAsync();
        }
    }
}
