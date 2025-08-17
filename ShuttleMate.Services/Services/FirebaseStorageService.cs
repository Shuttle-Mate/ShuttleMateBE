using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;
using Google.Cloud.Storage.V1;
using Microsoft.Extensions.Configuration;
using ShuttleMate.Contract.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShuttleMate.Services.Services
{
    public class FirebaseStorageService : IFirebaseStorageService
    {
        private readonly StorageClient _storageClient;
        private readonly string _bucketName;

        public FirebaseStorageService(IConfiguration config)
        {
            var credential = GoogleCredential.GetApplicationDefault();

            _storageClient = StorageClient.Create(credential);
            _bucketName = config["Firebase:Bucket"];
        }

        public async Task<string> UploadAvatarAsync(Stream fileStream, string fileName, string contentType)
        {
            var objectName = $"avatars/{Guid.NewGuid()}_{fileName}";

            var obj = await _storageClient.UploadObjectAsync(
                bucket: _bucketName,
                objectName: objectName,
                contentType: contentType,
                source: fileStream,
                options: new UploadObjectOptions
                {
                    PredefinedAcl = PredefinedObjectAcl.PublicRead
                }
            );


            // Link public (nếu rule đã cho phép read: if true)
            return $"https://storage.googleapis.com/{_bucketName}/{objectName}";
        }

    }
}
