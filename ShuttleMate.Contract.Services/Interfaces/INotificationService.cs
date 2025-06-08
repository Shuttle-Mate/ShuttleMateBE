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
        Task CreateNotification(NotiModel model);
        Task<List<ResponseNotiModel>> GetAll();
        Task<ResponseNotiModel> GetById(Guid notiId);
        Task UpdateNoti(UpdateNotiModel model);
        Task DeleteNoti(Guid notiId);
    }
}
