using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ShuttleMate.ModelViews.NotificationModelViews;
using ShuttleMate.ModelViews.NotiRecipientModelView;

namespace ShuttleMate.Contract.Services.Interfaces
{
    public interface INotiRecipientService
    {
        Task CreateNotiRecipient(NotiRecipientModel model);
        Task<List<ResponseNotiRecipientModel>> GetAll();
        Task<ResponseNotiRecipientModel> GetById(Guid notiRecipientId);
        Task UpdateStatusNotiRecipient(UpdateNotiRecipientModel model);
        Task DeleteNotiRecipient(Guid notiRecipeientId);
    }
}
