using ContentService.Application.Interfaces;
using ContentService.Infrastructure.Persistence;
using ContentService.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ContentService.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration config)
    {
        services.AddDbContext<ContentDbContext>(opts =>
            opts.UseNpgsql(config.GetConnectionString("Postgres")));

        services.AddScoped<ILessonRepository, LessonRepository>();

        return services;
    }
}
