namespace SkillScape.Domain.Entities;

public class PeerRoomTask
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string RoomId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? AssignedToUserId { get; set; }
    public bool IsCompleted { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }

    public PeerLearningRoom? Room { get; set; }
    public ApplicationUser? AssignedToUser { get; set; }
}

