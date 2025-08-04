using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking.Internal;
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
    public class TicketService : ITicketService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IConfiguration _configuration;
        private readonly IHttpContextAccessor _contextAccessor;
        private readonly IEmailService _emailService;

        public TicketService(IUnitOfWork unitOfWork, IMapper mapper, IConfiguration configuration, IHttpContextAccessor contextAccessor, IEmailService emailService)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _configuration = configuration;
            _contextAccessor = contextAccessor;
            _emailService = emailService;
        }
        public async Task<BasePaginatedList<TicketResponseModel>> GetAllAsync(int page = 0, int pageSize = 10, string? type = null, string? search = null, bool? price = null, Decimal? lowerBound = null, Decimal? upperBound = null, Guid? routeId = null, Guid? schoolId = null)
        {
            var ticketRepo = _unitOfWork.GetRepository<Ticket>();

            var query = ticketRepo.Entities
            .Where(x => x.Route.School.IsActive == true
            && x.Route.IsActive == true
            && !x.DeletedTime.HasValue)
            .Include(u => u.Route)
            .ThenInclude(x => x.School)
            .AsQueryable();
            if (!string.IsNullOrWhiteSpace(type) && Enum.TryParse<TicketTypeEnum>(type, true, out var parsedType))
            {
                query = query.Where(u => u.Type == parsedType);
            }
            if (routeId != null)
            {
                query = query.Where(u => u.Route.Id.ToString().Contains(routeId.ToString()!));
            }
            if (schoolId != null)
            {
                query = query.Where(u => u.Route.School.Id.ToString().Contains(schoolId.ToString()!));
            }
            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(u => u.Route.RouteName.Contains(search)
                || u.Route.RouteCode.Contains(search)
                || u.Route.School.Name.Contains(search));
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
            else if(upperBound != null)
            {
                query = query.Where(x => x.Price <= upperBound);
            }

            var tickets = await query
                .Select(u => new TicketResponseModel
                {
                    Id = u.Id,
                    Price = u.Price,
                    RouteName = u.Route.RouteName,
                    Type = u.Type.ToString().ToUpper(),
                    RouteId = u.RouteId,
                    Schoolname = u.Route.School.Name,
                })
                .ToListAsync();
            var totalCount = await query.CountAsync();

            var pagedItems = await query
                .Skip(page * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new BasePaginatedList<TicketResponseModel>(tickets, totalCount, page, pageSize);
        }
        static string ConvertStatusToString(TicketTypeEnum status)
        {
            return status switch
            {
                //TicketTypeEnum.DAY_PASS => "Vé ngày",
                TicketTypeEnum.MONTHLY => "Vé tháng",
                TicketTypeEnum.WEEKLY => "Vé tuần",
                //TicketTypeEnum.SINGLE_RIDE => "Vé 1 chiều",
                TicketTypeEnum.SEMESTER_ONE => "Vé học kì 1",
                TicketTypeEnum.SEMESTER_TWO => "Vé học kì 2",
                _ => "Không xác định"
            };
        }
        public async Task<TicketResponseModel> GetById(Guid Id)
        {
            var ticket = await _unitOfWork.GetRepository<Ticket>().Entities.FirstOrDefaultAsync(x => x.Id == Id
            && x.Route.School.IsActive == true
            && x.Route.IsActive == true
            && !x.DeletedTime.HasValue) ?? throw new ErrorException(StatusCodes.Status404NotFound, ResponseCodeConstants.NOT_FOUND, "Loại vé không tồn tại!");
            var response = new TicketResponseModel
            {
                Id = ticket.Id,
                Price = ticket.Price,
                RouteName = ticket.Route.RouteName,
                Type = ticket.Type.ToString().ToUpper(),
                RouteId = ticket.RouteId,
                Schoolname = ticket.Route.School.Name
            };
            return response;
        }
        public async Task CreateTicket(CreateTicketModel model)
        {
            if (model.Price <= 0)
            {
                throw new ErrorException(StatusCodes.Status404NotFound, ResponseCodeConstants.NOT_FOUND, "Giá loại vé phải lớn hơn 0!");
            }
            var route = await _unitOfWork.GetRepository<Route>().Entities.FirstOrDefaultAsync(x => x.Id == model.RouteId
            && x.IsActive == true
            && x.School.IsActive == true
            && !x.DeletedTime.HasValue) ?? throw new ErrorException(StatusCodes.Status404NotFound, ResponseCodeConstants.NOT_FOUND, "Tuyến đường không tồn tại!");
            switch (model.Type)
            {
                //case "SINGLE_RIDE":
                //    var newTicketSINGLE_RIDE = new Ticket
                //    {
                //        Id = Guid.NewGuid(),
                //        CreatedTime = DateTime.Now,
                //        Price = model.Price,
                //        RouteId = model.RouteId,
                //        //Type = TicketTypeEnum.SINGLE_RIDE,
                //        LastUpdatedTime = DateTime.Now,
                //    };
                //    await _unitOfWork.GetRepository<Ticket>().InsertAsync(newTicketSINGLE_RIDE);
                //    await _unitOfWork.SaveAsync();
                //    break;
                //case "DAY_PASS":
                //    var newTicketDAY_PASS = new Ticket
                //    {
                //        Id = Guid.NewGuid(),
                //        CreatedTime = DateTime.Now,
                //        Price = model.Price,
                //        RouteId = model.RouteId,
                //        //Type = TicketTypeEnum.DAY_PASS,
                //        LastUpdatedTime = DateTime.Now,
                //    };
                //    await _unitOfWork.GetRepository<Ticket>().InsertAsync(newTicketDAY_PASS);
                //    await _unitOfWork.SaveAsync();
                //    break;
                case "WEEKLY":
                    var newTicketWEEKLY = new Ticket
                    {
                        Id = Guid.NewGuid(),
                        CreatedTime = DateTime.Now,
                        Price = model.Price,
                        RouteId = model.RouteId,
                        Type = TicketTypeEnum.WEEKLY,
                        LastUpdatedTime = DateTime.Now,
                    };
                    await _unitOfWork.GetRepository<Ticket>().InsertAsync(newTicketWEEKLY);
                    await _unitOfWork.SaveAsync();
                    break;
                case "MONTHLY":
                    var newTicketMONTHLY = new Ticket
                    {
                        Id = Guid.NewGuid(),
                        CreatedTime = DateTime.Now,
                        Price = model.Price,
                        RouteId = model.RouteId,
                        Type = TicketTypeEnum.MONTHLY,
                        LastUpdatedTime = DateTime.Now,
                    };
                    await _unitOfWork.GetRepository<Ticket>().InsertAsync(newTicketMONTHLY);
                    await _unitOfWork.SaveAsync();
                    break;
                case "SEMESTER_ONE":
                    var newTicketSEMESTER_ONE = new Ticket
                    {
                        Id = Guid.NewGuid(),
                        CreatedTime = DateTime.Now,
                        Price = model.Price,
                        RouteId = model.RouteId,
                        Type = TicketTypeEnum.SEMESTER_ONE,
                        LastUpdatedTime = DateTime.Now,
                    };
                    await _unitOfWork.GetRepository<Ticket>().InsertAsync(newTicketSEMESTER_ONE);
                    await _unitOfWork.SaveAsync();
                    break;
                case "SEMESTER_TWO":
                    var newTicketSEMESTER_TWO = new Ticket
                    {
                        Id = Guid.NewGuid(),
                        CreatedTime = DateTime.Now,
                        Price = model.Price,
                        RouteId = model.RouteId,
                        Type = TicketTypeEnum.SEMESTER_TWO,
                        LastUpdatedTime = DateTime.Now,
                    };
                    await _unitOfWork.GetRepository<Ticket>().InsertAsync(newTicketSEMESTER_TWO);
                    await _unitOfWork.SaveAsync();
                    break;
                default:
                    throw new ErrorException(StatusCodes.Status404NotFound, ResponseCodeConstants.NOT_FOUND, "Không tìm thấy loại vé!");
            }
        }
        public async Task UpdateTicket(UpdateTicketModel model)
        {

            if (model.Price <= 0)
            {
                throw new ErrorException(StatusCodes.Status404NotFound, ResponseCodeConstants.NOT_FOUND, "Giá loại vé phải lớn hơn 0!");
            }
            var route = await _unitOfWork.GetRepository<Route>().Entities.FirstOrDefaultAsync(x => x.Id == model.RouteId
            && x.IsActive == true
            && x.School.IsActive == true
            && !x.DeletedTime.HasValue) ?? throw new ErrorException(StatusCodes.Status404NotFound, ResponseCodeConstants.NOT_FOUND, "Tuyến đường không tồn tại!");
            var ticket = await _unitOfWork.GetRepository<Ticket>().Entities.FirstOrDefaultAsync(x => x.Id == model.TicketTypeId && !x.DeletedTime.HasValue) ?? throw new ErrorException(StatusCodes.Status404NotFound, ResponseCodeConstants.NOT_FOUND, "Loại vé không tồn tại!");

            ticket.Price = model.Price;
            ticket.RouteId = model.RouteId;
            switch (model.Type)
            {
                //case "SINGLE_RIDE":
                //    ticket.Type = TicketTypeEnum.SINGLE_RIDE;
                //    break;
                //case "DAY_PASS":
                //    ticket.Type = TicketTypeEnum.DAY_PASS;
                //    break;
                case "WEEKLY":
                    ticket.Type = TicketTypeEnum.WEEKLY;
                    break;
                case "MONTHLY":
                    ticket.Type = TicketTypeEnum.MONTHLY;
                    break;
                case "SEMESTER_ONE":
                    ticket.Type = TicketTypeEnum.SEMESTER_ONE;
                    break;
                case "SEMESTER_TWO":
                    ticket.Type = TicketTypeEnum.SEMESTER_TWO;
                    break;
                default:
                    throw new ErrorException(StatusCodes.Status404NotFound, ResponseCodeConstants.NOT_FOUND, "Không tìm thấy loại vé!");
            }

            await _unitOfWork.GetRepository<Ticket>().UpdateAsync(ticket);
            await _unitOfWork.SaveAsync();
        }
        public async Task DeleteTicket(Guid ticketId)
        {
            var ticket = await _unitOfWork.GetRepository<Ticket>().Entities.FirstOrDefaultAsync(x => x.Id == ticketId && !x.DeletedTime.HasValue) ?? throw new ErrorException(StatusCodes.Status404NotFound, ResponseCodeConstants.NOT_FOUND, "Loại vé không tồn tại!");

            ticket.DeletedTime = DateTime.Now;

            await _unitOfWork.GetRepository<Ticket>().UpdateAsync(ticket);
            await _unitOfWork.SaveAsync();
        }
    }
}
