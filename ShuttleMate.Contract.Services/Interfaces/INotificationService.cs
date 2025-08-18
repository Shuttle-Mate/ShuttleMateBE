using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ShuttleMate.ModelViews.NotificationModelViews;
using ShuttleMate.ModelViews.RouteModelViews;
using static ShuttleMate.Contract.Repositories.Enum.GeneralEnum;

namespace ShuttleMate.Contract.Services.Interfaces
{
    public interface INotificationService
    {
        Task<Guid> SendNotificationFromTemplateAsync(string templateType, List<Guid> recipientIds, Dictionary<string, string> metadata, string createdBy, string notiCategory);
        Task<Guid> SendNotificationForAllFromTemplateAsync(string templateType, Dictionary<string, string> metadata, string notiCategory);
        Task CreateNotification(NotiModel model);
        Task<Guid> CreateNotificationForAllUsers(NotiModel model);
        Task<List<ResponseNotiModel>> GetAll();
        Task<ResponseNotiModel> GetById(Guid notiId);
        Task UpdateNoti(UpdateNotiModel model);
        Task HandleNotiStatus(Guid notiReciId, NotificationStatusEnum status);
        Task DeleteNoti(Guid notiId);
    }
}
