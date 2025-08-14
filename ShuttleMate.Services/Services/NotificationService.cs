using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.IdentityModel.Tokens;
using Org.BouncyCastle.Cms;
using ShuttleMate.Contract.Repositories.Entities;
using ShuttleMate.Contract.Repositories.Enum;
using ShuttleMate.Contract.Repositories.IUOW;
using ShuttleMate.Contract.Services.Interfaces;
using ShuttleMate.Core.Bases;
using ShuttleMate.Core.Constants;
using ShuttleMate.ModelViews.NotificationModelViews;
using ShuttleMate.ModelViews.ShuttleModelViews;
using ShuttleMate.Services.Services.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using static ShuttleMate.Contract.Repositories.Enum.GeneralEnum;

namespace ShuttleMate.Services.Services
{
    public class NotificationService : INotificationService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IHttpContextAccessor _contextAccessor;
        private readonly IFirebaseService _firebaseService;
        private readonly FirestoreService _firestoreService;

        public NotificationService(IUnitOfWork unitOfWork, IMapper mapper, IHttpContextAccessor contextAccessor, IFirebaseService firebaseService, FirestoreService firestoreService)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _contextAccessor = contextAccessor;
            _firebaseService = firebaseService;
            _firestoreService = firestoreService;
        }


        public async Task CreateNotification(NotiModel model)
        {
            string userId = Authentication.GetUserIdFromHttpContextAccessor(_contextAccessor);

            //Notification noti = await _unitOfWork.GetRepository<Notification>().Entities.FirstOrDefaultAsync(x => x.Title == model.Title);
            //if (noti != null)
            //{
            //    throw new ErrorException(StatusCodes.Status400BadRequest, ErrorCode.BadRequest, "Thông báo này đã tồn tại!!");
            //}
            var newNoti = _mapper.Map<Notification>(model);
            newNoti.CreatedBy = userId;
            newNoti.LastUpdatedBy = userId;
            await _unitOfWork.GetRepository<Notification>().InsertAsync(newNoti);
            await _unitOfWork.SaveAsync();
        }

        public async Task<Guid> CreateNotificationForAllUsers(NotiModel model)
        {
            var createdBy = Authentication.GetUserIdFromHttpContextAccessor(_contextAccessor);

            // Parse string -> enum
            if (!Enum.TryParse<NotificationCategoryEnum>(model.NotificationCategory, true, out var categoryEnum))
            {
                throw new ErrorException(
                    StatusCodes.Status400BadRequest,
                    ErrorCode.BadRequest,
                    $"Giá trị notiCategory '{model.NotificationCategory}' không hợp lệ."
                );
            }

            // 1. Create the notification record
            var notification = _mapper.Map<Notification>(model);
            notification.Id = Guid.NewGuid();
            notification.CreatedBy = createdBy;
            notification.LastUpdatedBy = createdBy;
            notification.NotificationCategory = categoryEnum;
            notification.Status = NotificationStatusEnum.SENT;
            notification.CreatedTime = DateTimeOffset.UtcNow;

            await _unitOfWork.GetRepository<Notification>().InsertAsync(notification);

            // 2. Get all active users
            var userIds = await _unitOfWork.GetRepository<User>()
                .Entities
                .Where(u => !u.DeletedTime.HasValue)
                .Select(u => u.Id)
                .ToListAsync();

            // 3. Create NotificationRecipient records and send push notifications
            var recipients = new List<NotificationRecipient>();
            foreach (var userId in userIds)
            {
                var recipient = new NotificationRecipient
                {
                    Id = Guid.NewGuid(),
                    NotificationId = notification.Id,
                    RecipientId = userId,
                    RecipientType = "User",
                    NotificationCategory = categoryEnum,
                    CreatedBy = createdBy,
                    CreatedTime = DateTimeOffset.UtcNow,
                    Status = NotificationStatusEnum.SENT
                };

                // Send push notification to all devices of the user
                var userDevices = await _unitOfWork.GetRepository<UserDevice>()
                    .Entities
                    .Where(d => d.UserId == userId && !d.DeletedTime.HasValue && d.IsValid == true)
                    .ToListAsync();

                if (userDevices.Count == 0)
                {
                    // Không có device, giữ nguyên status = SENT
                    recipients.Add(recipient);
                    continue;
                }

                bool atLeastOneSuccess = false;
                bool allFailed = true;

                foreach (var device in userDevices)
                {
                    if (!string.IsNullOrEmpty(device.PushToken))
                    {
                        try
                        {
                            await _firebaseService.SendNotificationAsync(notification.Title, notification.Content, device.PushToken);
                            var docRef = _firestoreService.GetCollection("notifications").Document(device.UserId.ToString());
                            await docRef.SetAsync(new
                            {
                                Title = notification.Title.ToString(),
                                Content = notification.Content.ToString(),
                                Status = notification.Status.ToString(),
                                NotificationCategory = notification.NotificationCategory.ToString(),
                                RecipientId = device.UserId.ToString()
                            });
                            atLeastOneSuccess = true;
                            allFailed = false;
                        }
                        catch
                        {
                            // Nếu lỗi, vẫn kiểm tra các device khác
                            allFailed = allFailed && true;
                        }
                    }
                }

                if (atLeastOneSuccess)
                    recipient.Status = NotificationStatusEnum.SENT;
                // Nếu không có device hợp lệ, giữ nguyên SENT

                recipients.Add(recipient);
            }

            await _unitOfWork.GetRepository<NotificationRecipient>().InsertRangeAsync(recipients);
            await _unitOfWork.SaveAsync();

            return notification.Id;
        }

        public async Task DeleteNoti(Guid notiId)
        {
            string userId = Authentication.GetUserIdFromHttpContextAccessor(_contextAccessor);

            var noti = await _unitOfWork.GetRepository<Notification>().Entities.FirstOrDefaultAsync(x => x.Id == notiId && !x.DeletedTime.HasValue) ?? throw new ErrorException(StatusCodes.Status404NotFound, ErrorCode.NotFound, "Không tìm thấy thông báo!");
            noti.DeletedTime = DateTime.Now;
            noti.DeletedBy = userId;
            await _unitOfWork.GetRepository<Notification>().UpdateAsync(noti);
            await _unitOfWork.SaveAsync();
        }

        public async Task<List<ResponseNotiModel>> GetAll()
        {
            var notis = await _unitOfWork.GetRepository<Notification>().Entities.Where(x => !x.DeletedTime.HasValue).OrderBy(x => x.CreatedTime).ToListAsync();
            if (!notis.Any())
            {
                throw new ErrorException(StatusCodes.Status404NotFound, ErrorCode.NotFound, "Không có thông báo nào tồn tại!");
            }
            return _mapper.Map<List<ResponseNotiModel>>(notis);
        }

        public async Task<ResponseNotiModel> GetById(Guid notiId)
        {
            var noti = await _unitOfWork.GetRepository<Notification>()
                .Entities
                .FirstOrDefaultAsync(x => x.Id == notiId && !x.DeletedTime.HasValue) 
                ?? throw new ErrorException(StatusCodes.Status404NotFound, ErrorCode.NotFound, "Không tìm thấy thông báo!");

            return _mapper.Map<ResponseNotiModel>(noti);
        }

        public async Task<Guid> SendNotificationForAllFromTemplateAsync(string templateType, Dictionary<string, string> metadata, string notiCategory)
        {
            var createdBy = Authentication.GetUserIdFromHttpContextAccessor(_contextAccessor);

            // Parse string -> enum
            if (!Enum.TryParse<NotificationCategoryEnum>(notiCategory, true, out var categoryEnum))
            {
                throw new ErrorException(
                    StatusCodes.Status400BadRequest,
                    ErrorCode.BadRequest,
                    $"Giá trị notiCategory '{notiCategory}' không hợp lệ."
                );
            }

            var template = await _unitOfWork
                .GetRepository<NotificationTemplate>()
                .Entities
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Type == templateType && x.DeletedTime == null);

            if (template == null)
            {
                throw new ErrorException(StatusCodes.Status404NotFound, ErrorCode.NotFound, $"không tìm thấy mẫu thông báo '{templateType}'");
            }



            //thay biến
            string content = template.Template;
            foreach (var kvp in metadata)
            {
                content = content.Replace($"{{{{{kvp.Key}}}}}", kvp.Value);
            }

            // 3. Tạo bản ghi Notifications
            var notification = new Notification
            {
                Id = Guid.NewGuid(),
                Title = $"Thông báo: {template.Type}", // modify
                Content = content,
                TemplateType = template.Type,
                Status = NotificationStatusEnum.SENT, // đã gửi
                CreatedBy = createdBy,
                NotificationCategory = categoryEnum,
                CreatedTime = DateTimeOffset.UtcNow,
                MetaData = JsonSerializer.Serialize(metadata)
            };

            _unitOfWork.GetRepository<Notification>().Insert(notification);

            // 4. Gửi FCM và tạo bản ghi NotificationRecipient
            var userIds = await _unitOfWork.GetRepository<User>()
                .Entities
                .Where(u => !u.DeletedTime.HasValue)
                .Select(u => u.Id)
                .ToListAsync();
            var recipients = new List<NotificationRecipient>();

            foreach (var recipientId in userIds)
            {
                var recipient = new NotificationRecipient
                {
                    Id = Guid.NewGuid(),
                    NotificationId = notification.Id,
                    RecipientId = recipientId,
                    RecipientType = "User",
                    NotificationCategory = categoryEnum,
                    CreatedBy = createdBy,
                    CreatedTime = DateTimeOffset.UtcNow,
                    Status = NotificationStatusEnum.SENT // sẽ cập nhật sau khi gửi FCM
                };

                try
                {
                    var userDevices = await _unitOfWork.GetRepository<UserDevice>()
                        .Entities
                        .Include(x => x.User)
                        .Where(u => !u.DeletedTime.HasValue && u.IsValid == true && u.UserId.Equals(recipientId))
                        .ToListAsync();

                    if (userDevices.Count == 0)
                    {
                        // Không có device, giữ nguyên status = SENT
                        recipients.Add(recipient);
                        continue;
                    }

                    bool atLeastOneSuccess = false;
                    bool allFailed = true;

                    foreach (var device in userDevices)
                    {
                        if (!string.IsNullOrEmpty(device.PushToken))
                        {
                            try
                            {
                                await _firebaseService.SendNotificationAsync(notification.Title, notification.Content, device.PushToken);
                                var docRef = _firestoreService.GetCollection("notifications").Document(device.UserId.ToString());
                                await docRef.SetAsync(new
                                {
                                    Title = notification.Title.ToString(),
                                    Content = notification.Content.ToString(),
                                    Status = notification.Status.ToString(),
                                    NotificationCategory = notification.NotificationCategory.ToString(),
                                    RecipientId = device.UserId.ToString()
                                });
                                atLeastOneSuccess = true;
                                allFailed = false;
                            }
                            catch
                            {
                                // Nếu lỗi, vẫn kiểm tra các device khác
                                allFailed = allFailed && true;
                            }
                        }
                    }
                    if (atLeastOneSuccess)
                        recipient.Status = NotificationStatusEnum.SENT;
                }
                catch (Exception ex)
                {
                    recipient.Status = NotificationStatusEnum.SENT;
                }


                recipients.Add(recipient);
            }

            _unitOfWork.GetRepository<NotificationRecipient>().InsertRange(recipients);
            await _unitOfWork.SaveAsync();
            return notification.Id;
        }

        public async Task<Guid> SendNotificationFromTemplateAsync(string templateType, List<Guid> recipientIds, Dictionary<string, string> metadata, string createdBy, string notiCategory)
        {
            if (createdBy.IsNullOrEmpty())
            {
                createdBy = Authentication.GetUserIdFromHttpContextAccessor(_contextAccessor);
            }

            // Parse string -> enum
            if (!Enum.TryParse<NotificationCategoryEnum>(notiCategory, true, out var categoryEnum))
            {
                throw new ErrorException(
                    StatusCodes.Status400BadRequest,
                    ErrorCode.BadRequest,
                    $"Giá trị notiCategory '{notiCategory}' không hợp lệ."
                );
            }

            var template = await _unitOfWork
                .GetRepository<NotificationTemplate>()
                .Entities
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Type == templateType && x.DeletedTime == null);

            if (template == null)
            {
                throw new ErrorException(StatusCodes.Status404NotFound, ErrorCode.NotFound, $"không tìm thấy mẫu thông báo '{templateType}'");
            }

            //thay biến
            string content = template.Template;
            foreach (var kvp in metadata)
            {
                content = content.Replace($"{{{{{kvp.Key}}}}}", kvp.Value);
            }

            // 3. Tạo bản ghi Notifications
            var notification = new Notification
            {
                Id = Guid.NewGuid(),
                Title = $"Thông báo: {template.Type}", // modify
                Content = content,
                TemplateType = template.Type,
                Status = NotificationStatusEnum.SENT, // đã gửi
                NotificationCategory = categoryEnum,
                CreatedBy = createdBy,
                CreatedTime = DateTimeOffset.UtcNow,
                MetaData = JsonSerializer.Serialize(metadata)
            };

            _unitOfWork.GetRepository<Notification>().Insert(notification);

            // 4. Gửi FCM và tạo bản ghi NotificationRecipient
            var recipients = new List<NotificationRecipient>();

            foreach (var recipientId in recipientIds)
            {
                var recipient = new NotificationRecipient
                {
                    Id = Guid.NewGuid(),
                    NotificationId = notification.Id,
                    RecipientId = recipientId,
                    RecipientType = "User",
                    NotificationCategory = categoryEnum,
                    CreatedBy = createdBy,
                    CreatedTime = DateTimeOffset.UtcNow,
                    Status = NotificationStatusEnum.PENDING // sẽ cập nhật sau khi gửi FCM
                };

                try
                {
                    var userDevices = await _unitOfWork.GetRepository<UserDevice>()
                        .Entities
                        .Include(x => x.User)
                        .Where(u => !u.DeletedTime.HasValue && u.IsValid == true && u.UserId.Equals(recipientId))
                        .ToListAsync();

                    if (userDevices.Count == 0)
                    {
                        // Không có device, giữ nguyên status = SENT
                        recipients.Add(recipient);
                        continue;
                    }

                    bool atLeastOneSuccess = false;
                    bool allFailed = true;

                    //var deviceTokens = userDevices.Select(d => d.PushToken).Where(t => !string.IsNullOrEmpty(t)).ToList();

                    //var token = await _deviceTokenService.GetTokenByUserIdAsync(recipientId); // bạn cần triển khai service này
                    //var token = "duyf6BD7RhOE66NtvuyQyL:APA91bGdMhNmmXaVI45wv-kSi6HubP0PyLgE52j-R_PT763N7v-xqUGnvZ0CX13fZREX41hg5rI722zKyNC1YmYy7FHjPKpWXEPlCj2oYJklvIyjeZppDto";

                    foreach (var device in userDevices)
                    {
                        if (!string.IsNullOrEmpty(device.PushToken))
                        {
                            try
                            {
                                await _firebaseService.SendNotificationAsync(
                                    notification.Title,
                                    notification.Content,
                                    token: device.PushToken
                                );
                                var docRef = _firestoreService.GetCollection("notifications").Document(device.UserId.ToString());
                                await docRef.SetAsync(new
                                {
                                    Title = notification.Title.ToString(),
                                    Content = notification.Content.ToString(),
                                    Status = notification.Status.ToString(),
                                    NotificationCategory = notification.NotificationCategory.ToString(),
                                    RecipientId = device.UserId.ToString()
                                });
                                atLeastOneSuccess = true;
                                allFailed = false;
                                //recipient.Status = NotificationStatusEnum.DELIVERED; // hoặc FAILED nếu có exception
                            }
                            catch
                            {
                                allFailed = allFailed && true;
                            }
                        }
                    }
                    if (atLeastOneSuccess)
                        recipient.Status = NotificationStatusEnum.SENT;
                }
                catch (Exception ex)
                {
                    recipient.Status = NotificationStatusEnum.SENT;
                }

                recipients.Add(recipient);
            }

            _unitOfWork.GetRepository<NotificationRecipient>().InsertRange(recipients);

            //// 4. Tạo bản ghi NotificationRecipients
            //var recipients = recipientIds.Select(recipientId => new NotificationRecipient
            //{
            //    Id = Guid.NewGuid(),
            //    NotificationId = notification.Id,
            //    RecipientId = recipientId,
            //    RecipientType = "User",
            //    Status = NotificationStatusEnum.PENDING,
            //    CreatedBy = createdBy,
            //    CreatedTime = DateTimeOffset.UtcNow
            //}).ToList();

            //_unitOfWork.GetRepository<NotificationRecipient>().InsertRange(recipients);

            await _unitOfWork.SaveAsync();
            return notification.Id;
        }

        public Task UpdateNoti(UpdateNotiModel model)
        {
            throw new NotImplementedException();
        }
    }
}
