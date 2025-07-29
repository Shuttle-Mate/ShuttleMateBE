using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShuttleMate.Contract.Services.Interfaces
{
    public interface IFirebaseService
    {
        Task SendNotificationAsync(string title, string body, string token);
    }
}
