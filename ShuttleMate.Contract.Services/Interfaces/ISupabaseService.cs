using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShuttleMate.Contract.Services.Interfaces
{
    public interface ISupabaseService
    {
        Task<string?> UploadAsync(Stream fileStream, string fileName, string contentType);
        string GetPublicUrl(string filePath);
        Task DeleteAsync(string filePath);
    }
}
