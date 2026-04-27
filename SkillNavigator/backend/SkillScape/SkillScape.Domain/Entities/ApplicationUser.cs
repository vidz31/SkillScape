namespace SkillScape.Domain.Entities;

/// <summary>
/// User entity - extends ASP.NET Identity User
/// </summary>
public class ApplicationUser
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    
    public string Email { get; set; } = string.Empty;
    
    public string FullName { get; set; } = string.Empty;
    
    public string? Bio { get; set; }
    
    public string? ProfileImageUrl { get; set; }
    
    public string Role { get; set; } = "Student";

    public bool ProfileCompleted { get; set; } = false;
    
    public int Level { get; set; } = 1;
    
    public long TotalXP { get; set; } = 0;
    
    public int CurrentStreak { get; set; } = 0;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    public bool IsActive { get; set; } = true;

    public bool IsBlocked { get; set; } = false;

    public string? BlockedReason { get; set; }

    public string? PasswordResetToken { get; set; }

    public DateTime? PasswordResetTokenExpiry { get; set; }
    
    // Navigation properties
    public ICollection<UserSkill> UserSkills { get; set; } = new List<UserSkill>();
    public ICollection<UserProgress> UserProgressions { get; set; } = new List<UserProgress>();
    public ICollection<UserBadge> UserBadges { get; set; } = new List<UserBadge>();
    public ICollection<QuizResponse> QuizResponses { get; set; } = new List<QuizResponse>();
    public ICollection<MentorRequest> SentRequests { get; set; } = new List<MentorRequest>();
    public ICollection<MentorSession> StudentSessions { get; set; } = new List<MentorSession>();
    public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
    public ICollection<MentorshipProgress> MentorshipProgressEntries { get; set; } = new List<MentorshipProgress>();
    public ApplicationMentor? MentorProfile { get; set; }
}
