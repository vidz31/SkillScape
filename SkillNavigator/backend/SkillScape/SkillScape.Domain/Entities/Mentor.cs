namespace SkillScape.Domain.Entities;

/// <summary>
/// Mentor - user who can mentor others
/// </summary>
public class ApplicationMentor
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    
    public string UserId { get; set; } = string.Empty;
    
    public string Expertise { get; set; } = string.Empty; // Domain expertise

    public string ExpertiseArea { get; set; } = string.Empty;
    
    public string? Bio { get; set; }
    
    public int YearsOfExperience { get; set; } = 0;

    public string? CurrentCompany { get; set; }

    public string SkillsCsv { get; set; } = string.Empty;

    public string? LinkedInUrl { get; set; }

    public string? AvailabilitySchedule { get; set; }

    public decimal? SessionPrice { get; set; }
    
    public decimal HourlyRate { get; set; } = 0;
    
    public bool IsAvailable { get; set; } = true;
    
    public int TotalSessionCount { get; set; } = 0;
    
    public double AvgRating { get; set; } = 0;

    public bool IsApproved { get; set; } = false;

    public string? ApprovedByAdminId { get; set; }

    public DateTime? ApprovedAt { get; set; }

    public string? RejectionReason { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    public ApplicationUser? User { get; set; }
    public ICollection<MentorRequest> MentorRequests { get; set; } = new List<MentorRequest>();
    public ICollection<MentorSession> Sessions { get; set; } = new List<MentorSession>();
    public ICollection<MentorSessionPriceHistory> SessionPriceHistory { get; set; } = new List<MentorSessionPriceHistory>();
    public ICollection<SessionFeedback> Feedbacks { get; set; } = new List<SessionFeedback>();
    public ICollection<MentorshipProgress> MentorshipProgressEntries { get; set; } = new List<MentorshipProgress>();
}

/// <summary>
/// Mentor request from student
/// </summary>
public class MentorRequest
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    
    public string StudentId { get; set; } = string.Empty;
    
    public string MentorId { get; set; } = string.Empty;
    
    public string Topic { get; set; } = string.Empty;
    
    public string Message { get; set; } = string.Empty;
    
    public string Status { get; set; } = "Pending"; // Pending, Accepted, Rejected, Cancelled
    
    public DateTime? ScheduledAt { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    public ApplicationUser? Student { get; set; }
    public ApplicationMentor? Mentor { get; set; }
}
