using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VeraCue.Api.Entities;

namespace VeraCue.Api.Data.Configurations;

/// <summary>Database rules for QaTemplates (see "VeraCue — Database Design", QaTemplates).</summary>
public class QaTemplateConfiguration : IEntityTypeConfiguration<QaTemplate>
{
    public void Configure(EntityTypeBuilder<QaTemplate> builder)
    {
        builder.ToTable("QaTemplates", table =>
        {
            table.HasCheckConstraint("CK_QaTemplates_Name_NotBlank", """length(btrim("Name")) > 0""");
        });

        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id).ValueGeneratedNever(); // the app creates the GUID

        builder.Property(t => t.Name).HasMaxLength(150).IsRequired();
        builder.Property(t => t.Description); // text, optional
        builder.Property(t => t.IsArchived).IsRequired();
        builder.Property(t => t.CreatedAt).IsRequired();
        builder.Property(t => t.UpdatedAt).IsRequired();

        // Criteria belong to their template: deleting a template row deletes its criteria.
        // (In practice templates are archived, not deleted.)
        builder.HasMany(t => t.Criteria)
            .WithOne(c => c.Template)
            .HasForeignKey(c => c.TemplateId)
            .OnDelete(DeleteBehavior.Cascade);

        // Unique *active* name, case-insensitive: created with raw SQL in the migration,
        // because EF Core can't express an index on lower("Name") with a filter.
    }
}
