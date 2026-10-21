using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HighSchoolManagementSystem.Models.Entities;

[Table("attendance_audit_logs")]
public class AttendanceAuditLog
{
    [Key, Column("id")] public int Id { get; set; }
    [Column("attendance_session_id")] public int AttendanceSessionId { get; set; }
    [Column("actor_user_id")] public int ActorUserId { get; set; }
    [Required, MaxLength(30), Column("action")] public string Action { get; set; } = "";
    [Required, Column("details")] public string Details { get; set; } = "";
    [Column("occurred_at")] public DateTime OccurredAt { get; set; }
    public AttendanceSession AttendanceSession { get; set; } = null!;
    public User ActorUser { get; set; } = null!;
}
