using Microsoft.EntityFrameworkCore;
using FileService.Infrastructure.Persistence;
using FileService.Infrastructure.Storage;
using FileService.Infrastructure.Messaging;
using FileService.Features.Processing;
using MediatR;
using System.Reflection;
using FluentValidation;
using FileService.Common.Behaviors;
using FileService.Common.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using FileService.Features.Processing.Hubs;

var builder = WebApplication.CreateBuilder(args);

// 1. Service registrations
builder.Services.AddControllers();
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()));
builder.Services.AddSwaggerGen();
builder.Services.AddSignalR();
builder.Services.AddDbContext<FileServiceDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found");
    options.UseNpgsql(connectionString, postgresOptions =>
    {
        postgresOptions.EnableRetryOnFailure(3, TimeSpan.FromSeconds(5), null);
        postgresOptions.CommandTimeout(30);
    });
    if (builder.Environment.IsDevelopment())
        options.EnableSensitiveDataLogging();
});
builder.Services.Configure<MinioOptions>(builder.Configuration.GetSection("Minio"));
builder.Services.Configure<RabbitMqOptions>(builder.Configuration.GetSection("RabbitMQ"));
builder.Services.Configure<FfmpegOptions>(builder.Configuration.GetSection("Ffmpeg"));
builder.Services.AddSingleton<IStorageService, MinioStorageService>();
builder.Services.AddSingleton<IMessagePublisher, RabbitMqPublisher>();
builder.Services.AddHostedService<RabbitMqConsumer>();
builder.Services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
builder.Services.AddCors(options =>
    options.AddPolicy("SignalRPolicy", policy => policy
        .WithOrigins(
            "http://localhost:3000",
            "http://localhost:3001",
            "https://localhost:3443")
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials()
        .WithExposedHeaders("*")));
builder.Services.AddHealthChecks();

var app = builder.Build();

// === TEST: Ensure MinIO bucket exists at startup ===
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var minio = scope.ServiceProvider.GetRequiredService<IStorageService>() as MinioStorageService;
    if (minio != null)
    {
        // Викликаємо EnsureBucketExistsAsync без await (fire-and-forget)
        _ = minio.GetType()
            .GetMethod("EnsureBucketExistsAsync", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?.Invoke(minio, new object[] { CancellationToken.None });
    }
}

// 2. Exception handler
app.UseExceptionHandler(errApp => errApp.Run(async context =>
{
    var feature = context.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerFeature>();
    var ex = feature?.Error;
    var (status, title) = ex switch
    {
        ValidationException ve => (400, ve.Message),
        StorageException      => (502, "Storage service error"),
        ProcessingException   => (500, "Processing error"),
        KeyNotFoundException ke => (404, ke.Message),
        InvalidOperationException ioe => (400, ioe.Message),
        _                     => (500, "Internal server error")
    };
    context.Response.StatusCode = status;
    context.Response.ContentType = "application/problem+json";
    await context.Response.WriteAsJsonAsync(new { title, status });
}));

// 3. CORS
app.UseCors("SignalRPolicy");

// 4. Swagger (dev only)
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// 5. Auth
app.UseAuthentication();
app.UseAuthorization();

// 6. Controllers
app.MapControllers();

// 7. SignalR hub
app.MapHub<ProcessingHub>("/hubs/processing");

// 8. Health checks
app.MapHealthChecks("/health");

app.Run();
