using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShuttleMate.Contract.Services.Interfaces
{
    public interface IFirebaseStorageService
    {
        Task<string> UploadAvatarAsync(Stream fileStream, string fileName, string contentType);
    }
}
