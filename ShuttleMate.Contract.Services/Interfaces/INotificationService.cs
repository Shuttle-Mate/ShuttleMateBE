using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ShuttleMate.ModelViews.NotificationModelViews;
using ShuttleMate.ModelViews.RouteModelViews;

namespace ShuttleMate.Contract.Services.Interfaces
{
    public interface INotificationService
    {
        Task<Guid> SendNotificationFromTemplateAsync(string templateType, List<Guid> recipientIds, Dictionary<string, string> metadata, string createdBy);
        Task CreateNotification(NotiModel model);
        Task<Guid> CreateNotificationForAllUsers(NotiModel model);
        Task<List<ResponseNotiModel>> GetAll();
        Task<ResponseNotiModel> GetById(Guid notiId);
        Task UpdateNoti(UpdateNotiModel model);
        Task DeleteNoti(Guid notiId);
    }
}
