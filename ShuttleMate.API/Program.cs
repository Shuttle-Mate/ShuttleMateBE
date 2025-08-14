using Hangfire;
using ShuttleMate.API;
using ShuttleMate.API.Middleware;
using ShuttleMate.API.Middleware;
using System.Reflection;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// Config appsettings by env
builder.Configuration
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables();

// Add services to the container.

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
        options.JsonSerializerOptions.WriteIndented = true;
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    var baseDir = AppContext.BaseDirectory;

    var apiXml = Path.Combine(baseDir, "ShuttleMate.API.xml");
    var modelXml = Path.Combine(baseDir, "ShuttleMate.ModelViews.xml");

    if (File.Exists(apiXml)) c.IncludeXmlComments(apiXml);
    if (File.Exists(modelXml)) c.IncludeXmlComments(modelXml);
});

builder.Services.AddHttpContextAccessor();
builder.Services.AddMemoryCache();
builder.Services.AddConfig(builder.Configuration);
builder.Services.AddHttpClient();

var app = builder.Build();

// Configure the HTTP request pipeline.

app.UseHangfireDashboard("/hangfire");
app.UseSwagger();
app.UseSwaggerUI();
app.UseCors("AllowAllOrigins");

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseAuthentication();

app.UseAuthorization();
app.UseMiddleware<ExceptionMiddleware>();
app.UseMiddleware<PermissionMiddleware>();
app.MapControllers();

app.Run();
