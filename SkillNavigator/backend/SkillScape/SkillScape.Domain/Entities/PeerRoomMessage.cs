namespace SkillScape.Domain.Entities;

public class PeerRoomMessage
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string RoomId { get; set; } = string.Empty;
    public string SenderId { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTime SentAt { get; set; } = DateTime.UtcNow;

    public PeerLearningRoom? Room { get; set; }
    public ApplicationUser? Sender { get; set; }
}

