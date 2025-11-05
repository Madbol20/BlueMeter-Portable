using Microsoft.EntityFrameworkCore;
using BlueMeter.Core.Data.Models.Database;

namespace BlueMeter.Core.Data.Database;

/// <summary>
/// Entity Framework DbContext for BlueMeter database
/// </summary>
public class BlueMeterDbContext : DbContext
{
    public DbSet<PlayerEntity> Players { get; set; } = null!;
    public DbSet<EncounterEntity> Encounters { get; set; } = null!;
    public DbSet<PlayerEncounterStatsEntity> PlayerEncounterStats { get; set; } = null!;

    public BlueMeterDbContext(DbContextOptions<BlueMeterDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure Player entity
        modelBuilder.Entity<PlayerEntity>(entity =>
        {
            entity.HasKey(e => e.UID);
            entity.HasIndex(e => e.Name);
            entity.HasIndex(e => e.LastSeenTime);
            entity.HasIndex(e => e.IsNpc);
        });

        // Configure Encounter entity
        modelBuilder.Entity<EncounterEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.EncounterId).IsUnique();
            entity.HasIndex(e => e.StartTime);
            entity.HasIndex(e => e.IsActive);

            // One encounter has many player stats
            entity.HasMany(e => e.PlayerStats)
                .WithOne(p => p.Encounter)
                .HasForeignKey(p => p.EncounterId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure PlayerEncounterStats entity
        modelBuilder.Entity<PlayerEncounterStatsEntity>(entity =>
        {
            entity.HasKey(e => e.Id);

            // Composite index for player + encounter lookup
            entity.HasIndex(e => new { e.PlayerUID, e.EncounterId }).IsUnique();
            entity.HasIndex(e => e.EncounterId);
            entity.HasIndex(e => e.PlayerUID);

            // One player has many encounter stats
            entity.HasOne(e => e.Player)
                .WithMany(p => p.EncounterStats)
                .HasForeignKey(e => e.PlayerUID)
                .OnDelete(DeleteBehavior.Cascade);

            // One encounter has many player stats (configured above)
        });
    }
}
