using EducationContentService.Core.EndpointsSettings;
using Serilog;

namespace EducationContentService.Core.Configuration;

public static class ApplicationBuilderExtensions
{
    public static IApplicationBuilder UseCoreConfiguration(this WebApplication app)
    {
        app.UseSerilogRequestLogging();

        app.UseSwagger();
        app.UseSwaggerUI();

        RouteGroupBuilder apiGroup = app.MapGroup("/api/lessons");
        app.UseEdnpoints(apiGroup);

        return app;
    }
}
