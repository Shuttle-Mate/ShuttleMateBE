using ShuttleMate.Core.Bases;
using ShuttleMate.ModelViews.NotiTemplateModelView;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShuttleMate.Contract.Services.Interfaces
{
    public interface INotificationTemplateService
    {
        Task CreateNotiTemplate(NotiTemplateModel model);
        Task<BasePaginatedList<ResponseNotiTemplateModel>> GetAll(GetNotiTemplateQuery req);
        Task<ResponseNotiTemplateModel> GetById(Guid notiTempId);
        Task UpdateNotiTemplate(UpdateNotiTemplateModel model);
        Task DeleteNotiTemplate(Guid notiTempId);
    }
}
