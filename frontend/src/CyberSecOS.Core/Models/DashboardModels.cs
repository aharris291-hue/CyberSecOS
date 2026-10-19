namespace CyberSecOS.Core.Models;

public sealed record DashboardData(
    string DisplayName,
    string TargetRole,
    ProgressTotals Totals,
    WeekProgress Week,
    NextLesson? NextLesson,
    IReadOnlyList<DailyMinutes> DailyActivity,
    IReadOnlyList<CourseProgress> Courses,
    IReadOnlyList<CertificationProgress> Certifications,
    IReadOnlyList<ActivityEntry> RecentActivity);
