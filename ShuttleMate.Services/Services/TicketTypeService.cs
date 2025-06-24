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
        public async Task<IEnumerable<TicketTypeResponseModel>> GetAllAsync(TicketTypeEnum? type, string? routeName = null, bool? price = null, Decimal? lowerBound = null, Decimal? upperBound = null)
        {
            var ticketRepo = _unitOfWork.GetRepository<TicketType>();

            var query = ticketRepo.Entities
        .Include(u => u.Route)
        .AsQueryable();
            if (type.HasValue)
            {
                query = query.Where(u => u.Type == type);
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
                    Type = ConvertStatusToString(u.Type)
                })
                .ToListAsync();

            return tickets;
        }
        static string ConvertStatusToString(TicketTypeEnum status)
        {
            return status switch
            {
                TicketTypeEnum.DayPass => "Vé ngày",
                TicketTypeEnum.Monthly => "Vé tháng",
                TicketTypeEnum.Weekly => "Vé tuần",
                TicketTypeEnum.SingleRide => "Vé 1 chiều",
                _ => "Không xác định"
            };
        }
        public async Task<TicketTypeResponseModel> GetById(Guid Id)
        {
            var ticketType = await _unitOfWork.GetRepository<TicketType>().Entities.FirstOrDefaultAsync(x=>x.Id == Id && !x.DeletedTime.HasValue) ?? throw new ErrorException(StatusCodes.Status404NotFound, ResponseCodeConstants.NOT_FOUND, "Loại vé không tồn tại!");
            var response =  new TicketTypeResponseModel
                {
                    Id = ticketType.Id,
                    Price = ticketType.Price,
                    RouteName = ticketType.Route.RouteName,
                    Type = ConvertStatusToString(ticketType.Type)
                };
            return response;
        }
        public async Task CreateTicketType(CreateTicketTypeModel model)
        {
            if(model.Price <= 0)
            {
                throw new ErrorException(StatusCodes.Status404NotFound, ResponseCodeConstants.NOT_FOUND, "Giá loại vé phải lớn hơn 0!");
            }
            var route = await _unitOfWork.GetRepository<Route>().Entities.FirstOrDefaultAsync(x=>x.Id == model.RouteId && !x.DeletedTime.HasValue) ?? throw new ErrorException(StatusCodes.Status404NotFound, ResponseCodeConstants.NOT_FOUND, "Tuyến đường không tồn tại!");

            var newTicketType = new TicketType
            {
                Id = Guid.NewGuid(),
                CreatedTime = DateTime.Now,
                Price = model.Price,
                RouteId = model.RouteId,
                Type = model.Type,
                LastUpdatedTime = DateTime.Now,
                
            };
            await _unitOfWork.GetRepository<TicketType>().InsertAsync(newTicketType);
            await _unitOfWork.SaveAsync();
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
            ticketType.Type = model.Type;

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
