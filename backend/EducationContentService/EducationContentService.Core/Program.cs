using EducationContentService.Core.EndpointsSettings;
using Microsoft.OpenApi;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

builder.Services.AddEndpoints(typeof(Program).Assembly);

builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Education Content Service API",
        Version = "v1",
    });
});

WebApplication app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

RouteGroupBuilder apiGroup = app.MapGroup("/api/lessons");
app.UseEdnpoints(apiGroup);

app.Run();