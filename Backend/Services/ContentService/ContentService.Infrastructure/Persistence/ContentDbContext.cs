using ContentService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System.Reflection;

namespace ContentService.Infrastructure.Persistence;

public class ContentDbContext : DbContext
{
    public DbSet<Lesson> Lessons => Set<Lesson>();

    public ContentDbContext(DbContextOptions<ContentDbContext> options) 
        : base(options) 
    { 

    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
    }
}