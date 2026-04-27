namespace SkillScape.Domain.Entities;

public class SessionComplaint
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string SessionId { get; set; } = string.Empty;
    public string ReportedBy { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public string Status { get; set; } = "Open"; // Open / Resolved
    public string? ResolutionNote { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ResolvedAt { get; set; }

    public MentorSession? Session { get; set; }
    public ApplicationUser? Reporter { get; set; }
}

public class AdminAuditLog
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string AdminId { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string TargetEntity { get; set; } = string.Empty;
    public string? TargetEntityId { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    public ApplicationUser? Admin { get; set; }
}
