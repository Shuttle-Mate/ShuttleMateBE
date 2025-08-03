using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using ShuttleMate.Contract.Repositories.Entities;
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

        public NotificationService(IUnitOfWork unitOfWork, IMapper mapper, IHttpContextAccessor contextAccessor, IFirebaseService firebaseService)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _contextAccessor = contextAccessor;
            _firebaseService = firebaseService;
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
                ?? throw new ErrorException(StatusCodes.Status404NotFound, ErrorCode.NotFound, "Không tìm thấy tuyến!");

            return _mapper.Map<ResponseNotiModel>(noti);
        }

        public async Task<Guid> SendNotificationFromTemplateAsync(string templateType, List<Guid> recipientIds, Dictionary<string, string> metadata, string createdBy)
        {
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
                Type = template.Type,
                Status = NotificationStatusEnum.SENT, // đã gửi
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
                    CreatedBy = createdBy,
                    CreatedTime = DateTimeOffset.UtcNow,
                    Status = NotificationStatusEnum.PENDING // sẽ cập nhật sau khi gửi FCM
                };

                try
                {
                    // Lấy FCM token (giả sử bạn có service riêng để lấy)
                    //var token = await _deviceTokenService.GetTokenByUserIdAsync(recipientId); // bạn cần triển khai service này
                    var token = "duyf6BD7RhOE66NtvuyQyL:APA91bGdMhNmmXaVI45wv-kSi6HubP0PyLgE52j-R_PT763N7v-xqUGnvZ0CX13fZREX41hg5rI722zKyNC1YmYy7FHjPKpWXEPlCj2oYJklvIyjeZppDto";

                    if (!string.IsNullOrEmpty(token))
                    {
                        await _firebaseService.SendNotificationAsync(
                            notification.Title,
                            notification.Content,
                            token: token
                        );
                        recipient.Status = NotificationStatusEnum.DELIVERED; // hoặc FAILED nếu có exception
                    }
                    else
                    {
                        recipient.Status = NotificationStatusEnum.FAILED;
                    }
                }
                catch (Exception ex)
                {
                    recipient.Status = NotificationStatusEnum.FAILED;
                    // log nếu cần: _logger.LogError(ex, "Failed to send FCM to recipient {RecipientId}", recipientId);
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
