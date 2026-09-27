using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HighSchoolManagementSystem.Models.Entities;

[Table("notifications")]
public class Notification
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("sender_id")]
    public int SenderId { get; set; }

    [Column("recipient_id")]
    public int RecipientId { get; set; }

    [Required]
    [MaxLength(100)]
    [Column("title")]
    public string Title { get; set; } = null!;

    [Required]
    [MaxLength(1000)]
    [Column("content")]
    public string Content { get; set; } = null!;

    [Column("is_read")]
    public bool IsRead { get; set; } = false;

    [Column("sent_at")]
    public DateTime SentAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    [ForeignKey(nameof(SenderId))]
    [InverseProperty(nameof(User.SentNotifications))]
    public virtual User Sender { get; set; } = null!;

    [ForeignKey(nameof(RecipientId))]
    [InverseProperty(nameof(User.ReceivedNotifications))]
    public virtual User Recipient { get; set; } = null!;
}
