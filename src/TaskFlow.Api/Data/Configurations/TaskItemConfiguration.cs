using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskFlow.Api.Data.Seed;
using TaskFlow.Api.Domain;
using TaskFlow.Shared.Validation;

namespace TaskFlow.Api.Data.Configurations;

internal sealed class TaskItemConfiguration : IEntityTypeConfiguration<TaskItem>
{
    public void Configure(EntityTypeBuilder<TaskItem> b)
    {
        b.ToTable("Tasks", t =>
        {
            t.HasCheckConstraint("CK_Tasks_Status", "[Status] BETWEEN 0 AND 2");
            t.HasCheckConstraint("CK_Tasks_Priority", "[Priority] BETWEEN 0 AND 3");
        });

        b.HasKey(x => x.Id);
        b.Property(x => x.Title).IsRequired().HasMaxLength(TaskFieldLimits.TitleMaxLength);
        b.Property(x => x.Description).HasMaxLength(TaskFieldLimits.DescriptionMaxLength);
        b.Property(x => x.AssignedTo).HasMaxLength(TaskFieldLimits.AssignedToMaxLength);

        // Stored as tinyint so ORDER BY Priority sorts by urgency, not alphabetically.
        b.Property(x => x.Status).HasConversion<byte>();
        b.Property(x => x.Priority).HasConversion<byte>();

        // Every EF query excludes soft-deleted rows. Raw SQL must filter IsDeleted itself.
        b.HasQueryFilter(x => !x.IsDeleted);

        b.HasIndex(x => new { x.Status, x.Priority })
            .HasDatabaseName("IX_Tasks_Status_Priority")
            .HasFilter("[IsDeleted] = 0");
        b.HasIndex(x => x.AssignedTo)
            .HasDatabaseName("IX_Tasks_AssignedTo")
            .HasFilter("[IsDeleted] = 0");

        b.HasData(TaskSeedData.Tasks);
    }
}
