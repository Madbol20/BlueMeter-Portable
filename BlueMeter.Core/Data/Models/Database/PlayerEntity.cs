using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using BlueMeter.Core.Models;

namespace BlueMeter.Core.Data.Models.Database;

/// <summary>
/// Represents a player in the database with cached stats
/// </summary>
[Table("Players")]
public class PlayerEntity
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public long UID { get; set; }

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    public int ProfessionID { get; set; }

    [MaxLength(50)]
    public string? SubProfessionName { get; set; }

    public ClassSpec Spec { get; set; }

    public Classes Class { get; set; }

    public int CombatPower { get; set; }

    public int Level { get; set; }

    public int RankLevel { get; set; }

    public int Critical { get; set; }

    public int Lucky { get; set; }

    public long MaxHP { get; set; }

    public DateTime LastSeenTime { get; set; }

    public DateTime FirstSeenTime { get; set; }

    /// <summary>
    /// Whether this is an NPC
    /// </summary>
    public bool IsNpc { get; set; }

    /// <summary>
    /// Navigation property for encounter statistics
    /// </summary>
    public virtual ICollection<PlayerEncounterStatsEntity> EncounterStats { get; set; } = new List<PlayerEncounterStatsEntity>();
}
