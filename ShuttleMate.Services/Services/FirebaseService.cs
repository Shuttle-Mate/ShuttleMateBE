using FirebaseAdmin;
using FirebaseAdmin.Messaging;
using Google.Apis.Auth.OAuth2;
using ShuttleMate.Contract.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShuttleMate.Services.Services
{
    public class FirebaseService : IFirebaseService
    {
        public FirebaseService()
        {
            if (FirebaseApp.DefaultInstance == null)
            {
                FirebaseApp.Create(new AppOptions()
                {
                    Credential = GoogleCredential.FromFile("/app/serviceaccountkey.json")
                });
            }
        }

        public async Task SendNotificationAsync(string title, string body, string token)
        {
            var message = new Message()
            {
                Token = token,
                Notification = new Notification
                {
                    Title = title,
                    Body = body
                }
            };
            await FirebaseMessaging.DefaultInstance.SendAsync(message);
        }
    }

}
