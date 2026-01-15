using EducationContentService.Core.EndpointsSettings;
using Microsoft.OpenApi;
using Serilog;
using Serilog.Exceptions;

namespace EducationContentService.Core.Configuration;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddCoreConfiguration(this IServiceCollection services, IConfiguration configuration)
    {
        return services
            .AddSeriLogging(configuration)
            .AddOpenApiSpec()
            .AddEndpoints(typeof(Program).Assembly);
    }

    private static IServiceCollection AddOpenApiSpec(this IServiceCollection services)
    {
        services.AddOpenApi();

        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "Education Content Service API",
                Version = "v1",
            });
        });

        return services;
    }

    private static IServiceCollection AddSeriLogging(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSerilog((services, lc) => lc
            .ReadFrom.Configuration(configuration)
            .ReadFrom.Services(services)
            .Enrich.FromLogContext()
            .Enrich.WithExceptionDetails()
            .Enrich.WithProperty("ServiceName", "EducationContent"));

        return services;
    }
}
