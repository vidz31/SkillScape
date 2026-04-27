namespace SkillScape.Domain.Entities;

/// <summary>
/// User's progress in a career domain
/// </summary>
public class UserProgress
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    
    public string UserId { get; set; } = string.Empty;
    
    public string CareerDomainId { get; set; } = string.Empty;
    
    public long XPInDomain { get; set; } = 0;
    
    public int SkillsCompleted { get; set; } = 0;
    
    public int TotalSkills { get; set; } = 0;
    
    public double ProgressPercentage { get; set; } = 0;
    
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    public ApplicationUser? User { get; set; }
    public CareerDomain? CareerDomain { get; set; }
}
