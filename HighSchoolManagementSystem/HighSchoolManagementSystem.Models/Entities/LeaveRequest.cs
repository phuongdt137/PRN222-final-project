using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HighSchoolManagementSystem.Models.Entities;

[Table("leave_requests")]
public class LeaveRequest
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("student_id")]
    public int StudentId { get; set; }

    [Column("request_by")]
    public int RequestBy { get; set; }

    [Required]
    [MaxLength(500)]
    [Column("reason")]
    public string Reason { get; set; } = null!;

    [MaxLength(255)]
    [Column("attachment")]
    public string? Attachment { get; set; }

    [MaxLength(500)]
    [Column("review_note")]
    public string? ReviewNote { get; set; }

    [Column("start_date")]
    public DateTime StartDate { get; set; }

    [Column("end_date")]
    public DateTime EndDate { get; set; }

    [Required]
    [MaxLength(30)]
    [Column("status")]
    public string Status { get; set; } = "Pending"; // Pending, Approved, Rejected

    [Column("requested_at")]
    public DateTime RequestedAt { get; set; } = DateTime.UtcNow;

    [Column("review_at")]
    public DateTime? ReviewAt { get; set; }

    [Column("reviewed_by")]
    public int? ReviewedBy { get; set; }

    // Navigation properties
    [ForeignKey(nameof(StudentId))]
    public virtual Student Student { get; set; } = null!;

    [ForeignKey(nameof(RequestBy))]
    public virtual User RequestedByUser { get; set; } = null!;

    [ForeignKey(nameof(ReviewedBy))]
    public virtual Teacher? ReviewedByTeacher { get; set; }

    public virtual ICollection<AttendanceRecord> AttendanceRecords { get; set; } = new List<AttendanceRecord>();
}
