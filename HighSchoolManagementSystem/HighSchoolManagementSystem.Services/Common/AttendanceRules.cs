namespace HighSchoolManagementSystem.Services.Common;

/// <summary>School dates are Vietnam local dates; timestamps remain UTC.</summary>
public static class AttendanceRules
{
    public static readonly TimeSpan SchoolOffset = TimeSpan.FromHours(7);
    public static DateTime Today(DateTimeOffset now) => now.ToOffset(SchoolOffset).Date;
    public static DateTimeOffset StartsAt(DateTime date, TimeSpan start) =>
        new(DateTime.SpecifyKind(date.Date.Add(start), DateTimeKind.Unspecified), SchoolOffset);
    public static DateTimeOffset Deadline(DateTime date) =>
        new(DateTime.SpecifyKind(date.Date.AddDays(1), DateTimeKind.Unspecified), SchoolOffset);

    // The end boundary is exclusive: 00:00:00 is already locked.
    public static bool CanWrite(DateTime date, TimeSpan start, DateTimeOffset now) =>
        now >= StartsAt(date, start) && now < Deadline(date);

    public static decimal? Rate(int present, int late, int total) =>
        total == 0 ? null : Math.Round((present + late) * 100m / total, 1);
}
