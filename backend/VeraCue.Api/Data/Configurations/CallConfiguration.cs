using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VeraCue.Api.Entities;

namespace VeraCue.Api.Data.Configurations;

/// <summary>Database rules for Calls (see "VeraCue — Database Design", Calls).</summary>
public class CallConfiguration : IEntityTypeConfiguration<Call>
{
    public void Configure(EntityTypeBuilder<Call> builder)
    {
        var statuses = SqlList(Enum.GetNames<CallStatus>());
        var stages = SqlList(Enum.GetNames<ProcessingStage>());

        builder.ToTable("Calls", table =>
        {
            table.HasCheckConstraint("CK_Calls_Title_NotBlank", "length(btrim(\"Title\")) > 0");
            table.HasCheckConstraint("CK_Calls_FileSizeBytes_Positive", "\"FileSizeBytes\" > 0");
            table.HasCheckConstraint("CK_Calls_DurationMs_Positive", "\"DurationMs\" IS NULL OR \"DurationMs\" > 0");
            table.HasCheckConstraint("CK_Calls_Status", $"\"Status\" IN ({statuses})");
            table.HasCheckConstraint("CK_Calls_FailedStage", $"\"FailedStage\" IS NULL OR \"FailedStage\" IN ({stages})");
            // A failed call must say which stage failed, so Retry knows where to restart.
            table.HasCheckConstraint("CK_Calls_Failed_HasStage", "\"Status\" <> 'Failed' OR \"FailedStage\" IS NOT NULL");
        });

        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).ValueGeneratedNever();

        builder.Property(c => c.Title).HasMaxLength(200).IsRequired();
        builder.Property(c => c.OriginalFileName).HasMaxLength(255).IsRequired();
        builder.Property(c => c.ContentType).HasMaxLength(100).IsRequired();
        builder.Property(c => c.AudioStorageKey).HasMaxLength(500).IsRequired();
        builder.Property(c => c.ErrorMessage).HasMaxLength(1000);
        builder.Property(c => c.TranscriptionModel).HasMaxLength(50);
        builder.Property(c => c.ProcessingPermissionConfirmedAt).IsRequired();
        builder.Property(c => c.UploadedAt).IsRequired();
        builder.Property(c => c.UpdatedAt).IsRequired();

        // Enums are stored as readable text ("ReadyForReview"), not numbers.
        builder.Property(c => c.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(c => c.FailedStage).HasConversion<string>().HasMaxLength(20);

        // PostgreSQL's system column xmin changes on every update: a free concurrency token.
        builder.Property(c => c.Version).IsRowVersion();

        // A template or user that has calls can't be deleted (templates are archived instead).
        builder.HasOne(c => c.Template).WithMany()
            .HasForeignKey(c => c.TemplateId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(c => c.UploadedBy).WithMany()
            .HasForeignKey(c => c.UploadedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        // Deleting a call deletes its transcript.
        builder.HasMany(c => c.Segments).WithOne(s => s.Call)
            .HasForeignKey(s => s.CallId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(c => c.AudioStorageKey).IsUnique();
        builder.HasIndex(c => c.UploadedAt).IsDescending(); // Call History, newest first
        builder.HasIndex(c => c.Status);                     // status filter; worker finding unfinished calls
        // TemplateId gets an index automatically as a foreign key (template filter, call counts).
    }

    private static string SqlList(IEnumerable<string> values) => string.Join(", ", values.Select(v => $"'{v}'"));
}
