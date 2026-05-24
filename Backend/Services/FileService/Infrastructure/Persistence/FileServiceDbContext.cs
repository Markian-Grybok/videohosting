using Microsoft.EntityFrameworkCore;
using FileService.Common.Entities;

namespace FileService.Infrastructure.Persistence
{
    public class FileServiceDbContext : DbContext
    {
        public FileServiceDbContext(DbContextOptions<FileServiceDbContext> options) : base(options) { }

        public DbSet<VideoFile> VideoFiles { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<VideoFile>(entity =>
            {
                entity.ToTable("video_files");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).ValueGeneratedOnAdd();
                entity.Property(e => e.Status).HasConversion<string>();
                entity.Property(e => e.OriginalFileName).IsRequired().HasMaxLength(512);
                entity.Property(e => e.StoragePath).IsRequired().HasMaxLength(512);
                entity.Property(e => e.HlsManifestPath).HasMaxLength(512);
                entity.Property(e => e.AvailableQualities).HasMaxLength(512).HasColumnName("available_qualities");
            });
        }
    }
}
