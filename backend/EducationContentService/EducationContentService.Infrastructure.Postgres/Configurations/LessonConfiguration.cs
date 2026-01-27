using EducationContentService.Domain.Lesson;
using EducationContentService.Domain.Lesson.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EducationContentService.Infrastructure.Postgres.Configurations;

public class LessonConfiguration : IEntityTypeConfiguration<Lesson>
{
    public void Configure(EntityTypeBuilder<Lesson> builder)
    {
        builder.ToTable("lessons");

        builder.HasKey(l => l.Id);

        builder.Property(l => l.Id)
            .ValueGeneratedNever()
            .HasConversion(
                id => id.Value,
                value => LessonId.Create(value))
            .HasColumnName("id");

        builder.Property(l => l.Title)
            .HasConversion(
                title => title.Value,
                value => Title.Create(value))
            .HasMaxLength(Title.MAX_LENGTH)
            .IsRequired()
            .HasColumnName("title");

        builder.Property(l => l.Description)
            .HasConversion(
                description => description.Value,
                value => Description.Create(value))
            .HasMaxLength(Description.MAX_LENGTH)
            .IsRequired()
            .HasColumnName("description");

        builder.Property(l => l.CreatedAt)
            .IsRequired()
            .HasColumnName("createdAt");

        builder.Property(l => l.UpdatedAt)
            .IsRequired()
            .HasColumnName("updatedAt");

        builder.Property(l => l.IsDeleted)
            .IsRequired()
            .HasDefaultValue(false)
            .HasColumnName("isDeleted");

        builder.Property(l => l.DeletedAt)
            .IsRequired(false)
            .HasColumnName("deletedAt");
    }
}
