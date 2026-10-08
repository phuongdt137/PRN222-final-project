namespace HighSchoolManagementSystem.Services.Common;

public static class SchoolDate
{
    // Vietnam uses UTC+7 without daylight saving; keep date-only validation independent of server timezone.
    public static DateTime Today => DateTimeOffset.UtcNow.ToOffset(TimeSpan.FromHours(7)).Date;
}
