using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HighSchoolManagementSystem.Models.Entities;

[Table("users")]
public class User
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Required]
    [MaxLength(50)]
    [Column("username")]
    public string Username { get; set; } = null!;

    [Required]
    [MaxLength(100)]
    [EmailAddress]
    [Column("email")]
    public string Email { get; set; } = null!;

    [Required]
    [MaxLength(100)]
    [Column("full_name")]
    public string FullName { get; set; } = null!;

    [Required]
    [MaxLength(255)]
    [Column("password_hash")]
    public string PasswordHash { get; set; } = null!;

    [MaxLength(20)]
    [Column("phone_number")]
    public string? PhoneNumber { get; set; }

    [MaxLength(10)]
    [Column("gender")]
    public string? Gender { get; set; }

    [Column("create_at")]
    public DateTime CreateAt { get; set; } = DateTime.UtcNow;

    [Column("date_of_birth")]
    public DateTime? DateOfBirth { get; set; }

    [MaxLength(255)]
    [Column("avatar_url")]
    public string? AvatarUrl { get; set; }

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    [Column("role_id")]
    public int RoleId { get; set; }

    // Navigation properties
    [ForeignKey(nameof(RoleId))]
    public virtual Role Role { get; set; } = null!;

    public virtual Teacher? Teacher { get; set; }
    public virtual Student? Student { get; set; }
    public virtual Parent? Parent { get; set; }

    [InverseProperty(nameof(Notification.Sender))]
    public virtual ICollection<Notification> SentNotifications { get; set; } = new List<Notification>();

    [InverseProperty(nameof(Notification.Recipient))]
    public virtual ICollection<Notification> ReceivedNotifications { get; set; } = new List<Notification>();

    public virtual ICollection<ScoreAuditLog> ModifiedScoreAuditLogs { get; set; } = new List<ScoreAuditLog>();
}
