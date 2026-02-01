using EducationContentService.Core.EndpointsSettings;
using EducationContentService.Infrastructure.Postgres;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace EducationContentService.Core.Configuration;

public static class ApplicationBuilderExtensions
{
    public static IApplicationBuilder UseCoreConfiguration(this WebApplication app)
    {
        app.UseSerilogRequestLogging();

        app.UseSwagger();
        app.UseSwaggerUI();
        app.ApplyMigration();

        RouteGroupBuilder apiGroup = app.MapGroup("/api/lessons");
        app.UseEdnpoints(apiGroup);

        return app;
    }

    public static IApplicationBuilder ApplyMigration(this IApplicationBuilder app)
    {
        using IServiceScope scope = app.ApplicationServices.CreateScope();

        using EducationDbContext dbContext =
            scope.ServiceProvider.GetRequiredService<EducationDbContext>();

        dbContext.Database.Migrate();

        return app;
    }
}
