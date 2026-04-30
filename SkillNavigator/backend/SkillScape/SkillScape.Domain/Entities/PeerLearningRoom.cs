namespace SkillScape.Domain.Entities;

public class PeerLearningRoom
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string CollaborationType { get; set; } = "Study Group";
    public int MaxMembers { get; set; } = 6;
    public string CreatorId { get; set; } = string.Empty;
    public string Status { get; set; } = "Active";
    public DateTime? ScheduledAt { get; set; }
    public int DurationMinutes { get; set; } = 60;
    public string SharedNotes { get; set; } = string.Empty;
    public string WhiteboardState { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ApplicationUser? Creator { get; set; }
    public ICollection<PeerRoomParticipant> Participants { get; set; } = new List<PeerRoomParticipant>();
    public ICollection<PeerRoomMessage> Messages { get; set; } = new List<PeerRoomMessage>();
    public ICollection<PeerRoomTask> Tasks { get; set; } = new List<PeerRoomTask>();
}

