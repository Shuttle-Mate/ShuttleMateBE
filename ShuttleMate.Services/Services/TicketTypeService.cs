using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ShuttleMate.Contract.Repositories.Entities;
using ShuttleMate.Contract.Repositories.IUOW;
using ShuttleMate.Contract.Services.Interfaces;
using ShuttleMate.ModelViews.TicketTypeModelViews;
using ShuttleMate.ModelViews.UserModelViews;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
        private string ConvertStatusToString(TicketTypeEnum status)
        {
            return status switch
            {
                TicketTypeEnum.DayPass => "",
                TicketTypeEnum.Monthly => "",
                TicketTypeEnum.Weekly => "",
                TicketTypeEnum.SingleRide => "",
                _ => "Không xác định"
            };
        }
    }
}
