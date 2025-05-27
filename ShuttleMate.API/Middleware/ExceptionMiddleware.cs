using System.Text.Json;
using ShuttleMate.Contract.Repositories.IUOW;
using ShuttleMate.Core.Bases;

namespace ShuttleMate.API.Middleware
{
    public class ExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionMiddleware> _logger;

        public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task Invoke(HttpContext context, IUnitOfWork unitOfWork)
        {
            try
            {
                await _next(context);
            }
            catch (CoreException ex)
            {
                _logger.LogError(ex, ex.Message);

                if (!context.Response.HasStarted)
                {
                    context.Response.Clear();
                    context.Response.StatusCode = ex.StatusCode;
                    context.Response.ContentType = "application/json";

                    var result = JsonSerializer.Serialize(new
                    {
                        errorCode = ex.Code ?? "Bad Request",
                        errorMessage = ex.Message ?? "Lỗi không xác định"
                    });

                    await context.Response.WriteAsync(result);
                }
            }
            catch (ErrorException ex)
            {

                if (!context.Response.HasStarted)
                {
                    context.Response.Clear();
                    context.Response.StatusCode = ex.StatusCode;
                    context.Response.ContentType = "application/json";

                    var result = JsonSerializer.Serialize(new
                    {
                        errorCode = ex.ErrorDetail?.ErrorCode ?? "Bad Request",
                        errorMessage = ex.ErrorDetail?.ErrorMessage ?? "Lỗi không xác định"
                    });

                    await context.Response.WriteAsync(result);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unexpected error occurred.");

                if (!context.Response.HasStarted)
                {
                    context.Response.Clear();
                    context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                    context.Response.ContentType = "application/json";

                    var result = JsonSerializer.Serialize(new
                    {
                        errorCode = "Internal Server Error",
                        errorMessage = ex.Message
                    });

                    await context.Response.WriteAsync(result);
                }
            }
        }

    }
}
