using EducationContentService.Domain.Lesson;
using Microsoft.EntityFrameworkCore;

namespace EducationContentService.Infrastructure.Postgres;

public class EducationDbContext : DbContext
{
    public DbSet<Lesson> Lessons { get; set; } = null!;

    public EducationDbContext(DbContextOptions<EducationDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(EducationDbContext).Assembly);
    }
}