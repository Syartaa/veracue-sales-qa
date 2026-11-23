using Microsoft.EntityFrameworkCore;
using VeraCue.Api.Entities;

namespace VeraCue.Api.Data;

/// <summary>
/// EF Core's view of the VeraCue PostgreSQL database (Neon).
/// The 8 tables from the database design are added feature by feature:
/// templates (Phase 2), calls and transcripts (Phase 3), evaluations (Phase 4), users (Phase 6).
/// </summary>
public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<QaTemplate> QaTemplates => Set<QaTemplate>();
    public DbSet<QaCriterion> QaCriteria => Set<QaCriterion>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Call> Calls => Set<Call>();
    public DbSet<TranscriptSegment> TranscriptSegments => Set<TranscriptSegment>();
    public DbSet<Evaluation> Evaluations => Set<Evaluation>();
    public DbSet<CriterionResult> CriterionResults => Set<CriterionResult>();
    public DbSet<ResultEvidence> ResultEvidence => Set<ResultEvidence>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Entity configurations (keys, constraints, indexes) will live in
        // IEntityTypeConfiguration classes in this folder and be picked up here.
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
