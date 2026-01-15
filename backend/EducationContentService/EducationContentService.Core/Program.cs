using System.Globalization;
using EducationContentService.Core.Configuration;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console(formatProvider: CultureInfo.InvariantCulture)
    .CreateBootstrapLogger();

try
{
    Log.Information("Starting Education Content Service Core");

    WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

    builder.Services.AddCoreConfiguration(builder.Configuration);

    WebApplication app = builder.Build();

    app.UseCoreConfiguration();

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}