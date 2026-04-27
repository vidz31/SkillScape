namespace SkillScape.Application.DTOs;

/// <summary>
/// Mentor DTO
/// </summary>
public class MentorDto
{
    public string Id { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string? ProfileImageUrl { get; set; }
    public string Expertise { get; set; } = string.Empty;
    public string ExpertiseArea { get; set; } = string.Empty;
    public string? CurrentCompany { get; set; }
    public string? Bio { get; set; }
    public List<string> Skills { get; set; } = new();
    public string? LinkedInUrl { get; set; }
    public string? AvailabilitySchedule { get; set; }
    public int YearsOfExperience { get; set; }
    public decimal HourlyRate { get; set; }
    public decimal? SessionPrice { get; set; }
    public bool IsAvailable { get; set; }
    public bool IsApproved { get; set; }
    public string? ApprovedByAdminId { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public string? RejectionReason { get; set; }
    public int TotalStudentsAssigned { get; set; }
}

/// <summary>
/// Mentor request DTO
/// </summary>
public class MentorRequestDto
{
    public string Id { get; set; } = string.Empty;
    public string StudentId { get; set; } = string.Empty;
    public string StudentName { get; set; } = string.Empty;
    public string MentorId { get; set; } = string.Empty;
    public string MentorName { get; set; } = string.Empty;
    public string Topic { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime? ScheduledAt { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Create mentor request
/// </summary>
public class CreateMentorRequestDto
{
    public string MentorId { get; set; } = string.Empty;
    public string Topic { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}

/// <summary>
/// Accept/Reject mentor request
/// </summary>
public class UpdateMentorRequestStatusDto
{
    public string Status { get; set; } = string.Empty; // Accepted, Rejected
    public DateTime? ScheduledAt { get; set; }
}

/// <summary>
/// Apply as Mentor DTO
/// </summary>
public class ApplyMentorDto
{
    public string Expertise { get; set; } = string.Empty;
    public string? ExpertiseArea { get; set; }
    public string? CurrentCompany { get; set; }
    public string Bio { get; set; } = string.Empty;
    public List<string> Skills { get; set; } = new();
    public string? LinkedInUrl { get; set; }
    public string? AvailabilitySchedule { get; set; }
    public int YearsOfExperience { get; set; }
    public decimal HourlyRate { get; set; }
    public decimal? SessionPrice { get; set; }
}

public class CreateMentorProfileDto
{
    public string Expertise { get; set; } = string.Empty;
    public string ExpertiseArea { get; set; } = string.Empty;
    public int YearsOfExperience { get; set; }
    public string? CurrentCompany { get; set; }
    public string? Bio { get; set; }
    public List<string> Skills { get; set; } = new();
    public string? LinkedInUrl { get; set; }
    public string? AvailabilitySchedule { get; set; }
    public decimal? SessionPrice { get; set; }
}

public class MentorFilterDto
{
    public string? Domain { get; set; }
    public double? MinRating { get; set; }
    public decimal? MaxPrice { get; set; }
    public int? MinYearsOfExperience { get; set; }
    public bool? FreeOnly { get; set; }
    public bool? AvailableToday { get; set; }
}

public class BookSessionDto
{
    public string MentorId { get; set; } = string.Empty;
    public DateTime ScheduledDateTime { get; set; }
    public int DurationMinutes { get; set; } = 60;
    public string? Notes { get; set; }
}

public class CreateMentorSessionDto
{
    public string StudentId { get; set; } = string.Empty;
    public DateTime ScheduledDateTime { get; set; }
    public int DurationMinutes { get; set; } = 60;
    public string? Notes { get; set; }
    public string? MeetingLink { get; set; }
}

public class ApproveSessionDto
{
    public string SessionId { get; set; } = string.Empty;
    public bool Approve { get; set; }
    public string? MeetingLink { get; set; }
    public string? Notes { get; set; }
}

public class UpdateSessionDto
{
    public DateTime? ScheduledDateTime { get; set; }
    public int? DurationMinutes { get; set; }
    public string? Notes { get; set; }
}

public class MentorSessionDto
{
    public string Id { get; set; } = string.Empty;
    public string StudentId { get; set; } = string.Empty;
    public string StudentName { get; set; } = string.Empty;
    public string MentorId { get; set; } = string.Empty;
    public string MentorName { get; set; } = string.Empty;
    public DateTime ScheduledDateTime { get; set; }
    public int DurationMinutes { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? MeetingLink { get; set; }
    public string? Notes { get; set; }
    public decimal SessionFee { get; set; }
    public bool IsPaid { get; set; }
    public string PaymentStatus { get; set; } = "Unpaid";
    public DateTime CreatedAt { get; set; }
}

public class AddFeedbackDto
{
    public string SessionId { get; set; } = string.Empty;
    public int Rating { get; set; }
    public string? ReviewText { get; set; }
}

public class SessionFeedbackDto
{
    public string Id { get; set; } = string.Empty;
    public string SessionId { get; set; } = string.Empty;
    public string MentorId { get; set; } = string.Empty;
    public int Rating { get; set; }
    public string? ReviewText { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class SessionFeedbackSummaryDto
{
    public string SessionId { get; set; } = string.Empty;
    public int Rating { get; set; }
    public string? ReviewText { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class UpsertMentorshipProgressDto
{
    public string StudentId { get; set; } = string.Empty;
    public string RoadmapStage { get; set; } = string.Empty;
    public string? MentorFeedback { get; set; }
    public string? CompletedTasks { get; set; }
    public string? NextMilestone { get; set; }
}

public class MentorshipProgressDto
{
    public string Id { get; set; } = string.Empty;
    public string StudentId { get; set; } = string.Empty;
    public string MentorId { get; set; } = string.Empty;
    public string RoadmapStage { get; set; } = string.Empty;
    public string? MentorFeedback { get; set; }
    public string? CompletedTasks { get; set; }
    public string? NextMilestone { get; set; }
    public DateTime UpdatedAt { get; set; }
    
    // Enrollment approval fields
    public bool IsApproved { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public string ApprovalStatus { get; set; } = "Pending";
    
    // Payment fields
    public bool IsPaid { get; set; }
    public DateTime? PaidAt { get; set; }
    public decimal Amount { get; set; }
    public string? PaymentMethod { get; set; }
    public string PaymentStatus { get; set; } = "Unpaid";
}

public class NotificationDto
{
    public string Id { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; }
}
/// <summary>
/// Mentor Dashboard DTO - Aggregates all dashboard data
/// </summary>
public class MentorDashboardDto
{
    public MentorDashboardStatsDto Stats { get; set; } = new();
    public List<StudentProgressDto> AssignedStudents { get; set; } = new();
    public List<MentorSessionDto> UpcomingSessions { get; set; } = new();
    public List<FeedbackSummaryDto> RecentFeedback { get; set; } = new();
    public List<MentorshipProgressDto> MentorshipProgresses { get; set; } = new();
    public List<MentorPaymentStatementDto> PaymentStatements { get; set; } = new();
}

/// <summary>
/// Dashboard Statistics
/// </summary>
public class MentorDashboardStatsDto
{
    public int TotalStudents { get; set; }
    public decimal TotalEarnings { get; set; }
    public decimal ThisMonthEarnings { get; set; }
    public double AvgRating { get; set; }
    public int TotalSessions { get; set; }
    public int TotalFeedback { get; set; }
    public int CompletionRate { get; set; }
    public int PendingRequests { get; set; }
    public double SessionAttendanceRate { get; set; } = 85;
    public double SatisfactionRate { get; set; } = 92;
    public int StudentsReachedLevel10 { get; set; }
    public int SkillsMasteredByStudents { get; set; }
    public int CareerPathCompletions { get; set; }
    public int BadgesEarnedByStudents { get; set; }
    public double AvgStudentLevel { get; set; }
    public int AvgQuizPerformance { get; set; }
    public int AvgHoursPerStudent { get; set; }
    public int CourseCompletionRate { get; set; }
    public int ThisMonthGrowth { get; set; }
    public int StudentRetention { get; set; }
    public int SkillRecommendationsUsed { get; set; }
    public int CareerPathTransitions { get; set; }
}

/// <summary>
/// Student Progress Details for Dashboard
/// </summary>
public class StudentProgressDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public int CurrentLevel { get; set; }
    public int Progress { get; set; }
    public string RoadmapStage { get; set; } = string.Empty;
    public string LastActive { get; set; } = string.Empty;
    public int QuizScore { get; set; }
    public int HoursSpent { get; set; }
    public string Status { get; set; } = "Active";
}

/// <summary>
/// Feedback Summary for Dashboard
/// </summary>
public class FeedbackSummaryDto
{
    public string Id { get; set; } = string.Empty;
    public string StudentName { get; set; } = string.Empty;
    public int Rating { get; set; }
    public string? Comment { get; set; }
    public string Date { get; set; } = string.Empty;
}

/// <summary>
/// Approve or Reject Enrollment Request
/// </summary>
public class ApproveEnrollmentDto
{
    public string EnrollmentId { get; set; } = string.Empty;
    public bool IsApproved { get; set; }
    public string? RejectionReason { get; set; }
}

/// <summary>
/// Enrollment Approval Response
/// </summary>
public class EnrollmentApprovalDto
{
    public string EnrollmentId { get; set; } = string.Empty;
    public bool IsApproved { get; set; }
    public string ApprovalStatus { get; set; } = string.Empty;
    public DateTime ApprovedAt { get; set; }
}

/// <summary>
/// Payment Request
/// </summary>
public class PaymentRequestDto
{
    public string EnrollmentId { get; set; } = string.Empty;
    public string? SessionId { get; set; }
    public string PaymentMethod { get; set; } = string.Empty; // Card, UPI, NetBanking
    public decimal Amount { get; set; }
}

/// <summary>
/// Payment Response
/// </summary>
public class PaymentResponseDto
{
    public string TransactionId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string PaymentMethod { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Student Wallet DTO
/// </summary>
public class StudentWalletDto
{
    public string Id { get; set; } = string.Empty;
    public string StudentId { get; set; } = string.Empty;
    public decimal Balance { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class MentorPaymentStatementDto
{
    public string TransactionId { get; set; } = string.Empty;
    public string StudentId { get; set; } = string.Empty;
    public string StudentName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public decimal SessionPrice { get; set; }
    public decimal SessionsCovered { get; set; }
    public string PaymentMethod { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime? CompletedAt { get; set; }
    public DateTime CreatedAt { get; set; }
}