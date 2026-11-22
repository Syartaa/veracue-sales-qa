using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VeraCue.Api.Entities;

namespace VeraCue.Api.Data.Configurations;

/// <summary>Database rules for QaCriteria (see "VeraCue — Database Design", QaCriteria).</summary>
public class QaCriterionConfiguration : IEntityTypeConfiguration<QaCriterion>
{
    public void Configure(EntityTypeBuilder<QaCriterion> builder)
    {
        builder.ToTable("QaCriteria", table =>
        {
            table.HasCheckConstraint("CK_QaCriteria_Name_NotBlank", """length(btrim("Name")) > 0""");
            table.HasCheckConstraint("CK_QaCriteria_Instructions_NotBlank", """length(btrim("Instructions")) > 0""");
            table.HasCheckConstraint("CK_QaCriteria_MaxPoints_Range", "\"MaxPoints\" BETWEEN 1 AND 100");
            table.HasCheckConstraint("CK_QaCriteria_SortOrder_NonNegative", "\"SortOrder\" >= 0");
        });

        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).ValueGeneratedNever();

        builder.Property(c => c.Name).HasMaxLength(150).IsRequired();
        builder.Property(c => c.Instructions).IsRequired(); // text
        builder.Property(c => c.CreatedAt).IsRequired();
        builder.Property(c => c.UpdatedAt).IsRequired();

        // Loading a template's criteria in order. Not unique, so reordering
        // (swapping two SortOrder values) never trips a constraint mid-update.
        builder.HasIndex(c => new { c.TemplateId, c.SortOrder });
    }
}
