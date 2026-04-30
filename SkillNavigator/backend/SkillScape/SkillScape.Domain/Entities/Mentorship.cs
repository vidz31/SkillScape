namespace SkillScape.Domain.Entities;

public class MentorSession
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string StudentId { get; set; } = string.Empty;
    public string MentorId { get; set; } = string.Empty;
    public DateTime ScheduledDateTime { get; set; }
    public int DurationMinutes { get; set; } = 60;
    public string Status { get; set; } = "Pending"; // Pending / Approved / Completed / Cancelled
    public string? MeetingLink { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ApplicationUser? Student { get; set; }
    public ApplicationMentor? Mentor { get; set; }
    public SessionFeedback? Feedback { get; set; }
}

public class SessionFeedback
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string SessionId { get; set; } = string.Empty;
    public string MentorId { get; set; } = string.Empty;
    public int Rating { get; set; } = 5;
    public string? ReviewText { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public MentorSession? Session { get; set; }
    public ApplicationMentor? Mentor { get; set; }
}

public class MentorshipProgress
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string StudentId { get; set; } = string.Empty;
    public string MentorId { get; set; } = string.Empty;
    public string RoadmapStage { get; set; } = string.Empty;
    public string? MentorFeedback { get; set; }
    public string? CompletedTasks { get; set; }
    public string? NextMilestone { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    // Enrollment approval fields
    public bool IsApproved { get; set; } = false;
    public DateTime? ApprovedAt { get; set; }
    public string ApprovalStatus { get; set; } = "Pending"; // Pending, Approved, Rejected
    
    // Payment fields
    public bool IsPaid { get; set; } = false;
    public DateTime? PaidAt { get; set; }
    public decimal Amount { get; set; } = 0;
    public string? PaymentMethod { get; set; }
    public string PaymentStatus { get; set; } = "Unpaid"; // Unpaid, Pending, Completed, Failed

    public ApplicationUser? Student { get; set; }
    public ApplicationMentor? Mentor { get; set; }
}

public class Notification
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string UserId { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty; // Badge, LevelUp, Session, Message, Enrollment, MentorApproval, etc.
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public bool IsRead { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ApplicationUser? User { get; set; }
}

public class StudentWallet
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string StudentId { get; set; } = string.Empty;
    public decimal Balance { get; set; } = 1000; // 1000 rupees initial balance
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    public ApplicationUser? Student { get; set; }
}

public class PaymentTransaction
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string StudentId { get; set; } = string.Empty;
    public string MentorshipProgressId { get; set; } = string.Empty;
    public string? SessionId { get; set; }
    public decimal Amount { get; set; }
    public string PaymentMethod { get; set; } = string.Empty; // Card, UPI, NetBanking
    public string Status { get; set; } = "Pending"; // Pending, Completed, Failed
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
    
    public ApplicationUser? Student { get; set; }
    public MentorshipProgress? MentorshipProgress { get; set; }
    public MentorSession? Session { get; set; }
}

public class MentorSessionPriceHistory
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string MentorId { get; set; } = string.Empty;
    public decimal SessionPrice { get; set; }
    public DateTime EffectiveFrom { get; set; } = DateTime.UtcNow;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ApplicationMentor? Mentor { get; set; }
}
