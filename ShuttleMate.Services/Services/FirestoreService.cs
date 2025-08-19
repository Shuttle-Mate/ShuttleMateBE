using Google.Apis.Auth.OAuth2;
using Google.Cloud.Firestore;
using Grpc.Auth;
using Microsoft.Identity.Client.Platforms.Features.DesktopOs.Kerberos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShuttleMate.Services.Services
{
    public class FirestoreService
    {
        private readonly FirestoreDb _firestoreDb;

        public FirestoreService()
        {
            var credential = GoogleCredential.GetApplicationDefault();

            //string projectId = ((ServiceAccountCredential)credential.UnderlyingCredential).ProjectId;

            var projectId = ((ServiceAccountCredential)credential.UnderlyingCredential).Id.Split('@')[1].Split('.')[0];

            //_firestoreDb = FirestoreDb.Create(projectId);

            _firestoreDb = new FirestoreDbBuilder
            {
                ProjectId = projectId,
                ChannelCredentials = credential.ToChannelCredentials()
            }.Build();
        }

        //public async Task AddAttendanceRecordAsync(string shuttleId, string userId, string userName, string action)
        //{
        //    var collectionRef = _firestoreDb.Collection("shuttle_checkins")
        //                                    .Document(shuttleId)
        //                                    .Collection("records");

        //    var record = new
        //    {
        //        UserId = userId,
        //        UserName = userName,
        //        Action = action, // "CheckIn" hoặc "CheckOut"
        //        Timestamp = DateTime.UtcNow
        //    };

        //    await collectionRef.AddAsync(record);
        //}

        public async Task AddDataAsync(string collection, string documentId, object data)
        {
            DocumentReference docRef = _firestoreDb.Collection(collection).Document(documentId);
            await docRef.SetAsync(data);
        }

        public FirestoreDb GetDb() => _firestoreDb;

        public CollectionReference GetCollection(string name)
        {
            return _firestoreDb.Collection(name);
        }
    }
}
