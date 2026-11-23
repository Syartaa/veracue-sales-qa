using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VeraCue.Api.Entities;

namespace VeraCue.Api.Data.Configurations;

/// <summary>Database rules for Evaluations (see "VeraCue — Database Design", Evaluations).</summary>
public class EvaluationConfiguration : IEntityTypeConfiguration<Evaluation>
{
    public void Configure(EntityTypeBuilder<Evaluation> builder)
    {
        builder.ToTable("Evaluations", table =>
        {
            // "Reviewed" always records who reviewed it, and only then.
            table.HasCheckConstraint("CK_Evaluations_Reviewed_HasReviewer",
                "(\"ReviewedAt\" IS NULL) = (\"ReviewedByUserId\" IS NULL)");
        });

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedNever();
        builder.Property(e => e.TemplateNameSnapshot).HasMaxLength(150).IsRequired();
        builder.Property(e => e.ModelName).HasMaxLength(100).IsRequired();
        builder.Property(e => e.PromptVersion).HasMaxLength(20).IsRequired();
        builder.Property(e => e.CreatedAt).IsRequired();

        // One evaluation per call (MVP). Dropping this unique index later would allow re-runs.
        builder.HasOne(e => e.Call).WithOne()
            .HasForeignKey<Evaluation>(e => e.CallId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(e => e.CallId).IsUnique();

        builder.HasOne(e => e.ReviewedBy).WithMany()
            .HasForeignKey(e => e.ReviewedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(e => e.Results).WithOne(r => r.Evaluation)
            .HasForeignKey(r => r.EvaluationId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
