namespace SkillScape.Application.DTOs;

public class AdminDashboardStatsDto
{
    public int TotalUsers { get; set; }
    public int TotalMentors { get; set; }
    public int PendingMentorApprovals { get; set; }
    public int TotalSessions { get; set; }
    public int CompletedSessions { get; set; }
    public int ActiveStudents { get; set; }
    public decimal TotalRevenue { get; set; }
    public double AverageMentorRating { get; set; }
}

public class AdminUserDto
{
    public string Id { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public bool IsBlocked { get; set; }
    public string? BlockedReason { get; set; }
    public bool ProfileCompleted { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class BlockUserDto
{
    public string UserId { get; set; } = string.Empty;
    public bool IsBlocked { get; set; }
    public string? Reason { get; set; }
}

public class UpdateUserRoleDto
{
    public string UserId { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
}

public class RejectMentorDto
{
    public string Reason { get; set; } = string.Empty;
}

public class AdminSessionDto
{
    public string Id { get; set; } = string.Empty;
    public string StudentName { get; set; } = string.Empty;
    public string MentorName { get; set; } = string.Empty;
    public DateTime ScheduledDateTime { get; set; }
    public string Status { get; set; } = string.Empty;
    public int DurationMinutes { get; set; }
    public decimal SessionPrice { get; set; }
}

public class PlatformAnalyticsDto
{
    public string MostSelectedCareerPath { get; set; } = string.Empty;
    public string WeakestQuizCategory { get; set; } = string.Empty;
    public double AverageQuizScore { get; set; }
    public int DailySignups { get; set; }
    public int ActiveUsersLast30Days { get; set; }
    public int MonthlySessions { get; set; }
    public List<MentorPerformanceDto> TopMentors { get; set; } = new();
}

public class MentorPerformanceDto
{
    public string MentorId { get; set; } = string.Empty;
    public string MentorName { get; set; } = string.Empty;
    public int CompletedSessions { get; set; }
    public double AverageRating { get; set; }
    public decimal Revenue { get; set; }
}

public class AdminAnnouncementDto
{
    public string Message { get; set; } = string.Empty;
}

public class SessionComplaintDto
{
    public string Id { get; set; } = string.Empty;
    public string SessionId { get; set; } = string.Empty;
    public string ReportedByName { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public string? ResolutionNote { get; set; }
}

public class ResolveComplaintDto
{
    public string ResolutionNote { get; set; } = string.Empty;
}

public class AdminQuizOptionCrudDto
{
    public string? Id { get; set; }
    public string Text { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }
    public Dictionary<string, int> DomainWeights { get; set; } = new();
}

public class AdminQuizQuestionCrudDto
{
    public string Id { get; set; } = string.Empty;
    public string CareerDomainId { get; set; } = string.Empty;
    public string CareerDomainName { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; }
    public List<AdminQuizOptionCrudDto> Options { get; set; } = new();
}

public class UpsertAdminQuizQuestionDto
{
    public string CareerDomainId { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; } = true;
    public List<AdminQuizOptionCrudDto> Options { get; set; } = new();
}

public class AdminRoadmapTopicCrudDto
{
    public string? Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? ResourceUrl { get; set; }
    public int DisplayOrder { get; set; }
}

public class AdminRoadmapModuleCrudDto
{
    public string Id { get; set; } = string.Empty;
    public string CareerDomainId { get; set; } = string.Empty;
    public string CareerDomainName { get; set; } = string.Empty;
    public string SkillId { get; set; } = string.Empty;
    public string SkillName { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int StepNumber { get; set; }
    public int EstimatedHours { get; set; }
    public bool IsActive { get; set; }
    public List<AdminRoadmapTopicCrudDto> Topics { get; set; } = new();
}

public class UpsertAdminRoadmapModuleDto
{
    public string CareerDomainId { get; set; } = string.Empty;
    public string SkillId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int StepNumber { get; set; }
    public int EstimatedHours { get; set; } = 10;
    public bool IsActive { get; set; } = true;
    public List<AdminRoadmapTopicCrudDto> Topics { get; set; } = new();
}

public class AdminMentorCrudDto
{
    public string Id { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public bool IsApproved { get; set; }
    public bool IsBlocked { get; set; }
    public string? BlockedReason { get; set; }
    public string ExpertiseArea { get; set; } = string.Empty;
    public int YearsOfExperience { get; set; }
    public decimal SessionPrice { get; set; }
    public bool IsAvailable { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateAdminMentorDto
{
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string ExpertiseArea { get; set; } = string.Empty;
    public int YearsOfExperience { get; set; }
    public decimal SessionPrice { get; set; }
    public string? CurrentCompany { get; set; }
    public string? Bio { get; set; }
    public List<string> Skills { get; set; } = new();
    public string? LinkedInUrl { get; set; }
    public string? AvailabilitySchedule { get; set; }
}

public class AdminBlockMentorDto
{
    public bool IsBlocked { get; set; }
    public string? Reason { get; set; }
}

public class AdminResumeSummaryDto
{
    public string UserId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public int CompletedSkills { get; set; }
    public int Certifications { get; set; }
    public long TotalXP { get; set; }
    public int Level { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class AdminUserProgressSnapshotDto
{
    public string UserId { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public int Level { get; set; }
    public long TotalXP { get; set; }
    public int CompletedSkills { get; set; }
    public double AverageDomainProgress { get; set; }
    public DateTime UpdatedAt { get; set; }
}
