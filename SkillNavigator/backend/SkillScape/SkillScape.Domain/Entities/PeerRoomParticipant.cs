namespace SkillScape.Domain.Entities;

public class PeerRoomParticipant
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string RoomId { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string Status { get; set; } = "Pending";
    public string Role { get; set; } = "Member";
    public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ApprovedAt { get; set; }
    public DateTime? LastSeenAt { get; set; }

    public PeerLearningRoom? Room { get; set; }
    public ApplicationUser? User { get; set; }
}

