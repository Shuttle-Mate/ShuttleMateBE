using ShuttleMate.ModelViews.EmailModelViews;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShuttleMate.Contract.Services.Interfaces
{
    public interface IEmailService
    {
        Task SendEmailAsync(string email, string subject, string body);
        Task StaffSendEmailAsync(SendEmailRequestModel model);
    }
}
