using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EducationContentService.Infrastructure.Postgres;

public static class DependencyInjection
{
    public static IServiceCollection AddPostgresConfiguration(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<EducationDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

        return services;
    }
}