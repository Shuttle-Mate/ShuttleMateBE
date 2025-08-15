using ShuttleMate.Contract.Repositories.Entities;
using ShuttleMate.Contract.Repositories.IUOW;
using ShuttleMate.Core.Utils;

namespace ShuttleMate.API.Middleware
{
    public class SystemLogMiddleware
    {
        private readonly RequestDelegate _next;

        public SystemLogMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context, IUnitOfWork unitOfWork)
        {
            await _next(context);

            Guid? actorId = null;
            var userIdClaim = context.User.Claims.FirstOrDefault(c => c.Type == "sub" || c.Type == "id");
            if (userIdClaim != null && Guid.TryParse(userIdClaim.Value, out var parsedId))
            {
                actorId = parsedId;
            }

            var log = new SystemLogs
            {
                Id = Guid.NewGuid(),
                Action = $"{context.Request.Method} {context.Request.Path}",
                CreatedTime = CoreHelper.SystemTimeNow,
                CreatedBy = actorId?.ToString(),
                ActorId = actorId ?? Guid.Empty,
                MetaData = await ReadRequestBody(context)
            };

            await unitOfWork.GetRepository<SystemLogs>().InsertAsync(log);
            await unitOfWork.SaveAsync();
        }

        private async Task<string> ReadRequestBody(HttpContext context)
        {
            context.Request.EnableBuffering();
            var body = await new StreamReader(context.Request.Body).ReadToEndAsync();
            context.Request.Body.Position = 0;
            return body;
        }
    }
}
