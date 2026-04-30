namespace SkillScape.Application.DTOs;

public class PeerRoomDto
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string CollaborationType { get; set; } = string.Empty;
    public int MaxMembers { get; set; }
    public string CreatorId { get; set; } = string.Empty;
    public string CreatorName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime? ScheduledAt { get; set; }
    public int DurationMinutes { get; set; }
    public int ApprovedMembers { get; set; }
    public string CurrentUserStatus { get; set; } = "None";
    public bool CanManage { get; set; }
}

public class PeerRoomDetailsDto : PeerRoomDto
{
    public string SharedNotes { get; set; } = string.Empty;
    public string WhiteboardState { get; set; } = string.Empty;
    public List<PeerRoomParticipantDto> Participants { get; set; } = new();
    public List<PeerRoomMessageDto> Messages { get; set; } = new();
    public List<PeerRoomTaskDto> Tasks { get; set; } = new();
}

public class PeerRoomParticipantDto
{
    public string Id { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Avatar { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public DateTime RequestedAt { get; set; }
    public DateTime? LastSeenAt { get; set; }
}

public class PeerRoomMessageDto
{
    public string Id { get; set; } = string.Empty;
    public string SenderId { get; set; } = string.Empty;
    public string SenderName { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTime SentAt { get; set; }
}

public class PeerRoomTaskDto
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? AssignedToUserId { get; set; }
    public string? AssignedToName { get; set; }
    public bool IsCompleted { get; set; }
}

public class CreatePeerRoomRequest
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string CollaborationType { get; set; } = "Study Group";
    public int MaxMembers { get; set; } = 6;
    public DateTime? ScheduledAt { get; set; }
    public int DurationMinutes { get; set; } = 60;
}

public class UpsertPeerRoomTaskRequest
{
    public string Title { get; set; } = string.Empty;
    public string? AssignedToUserId { get; set; }
}

public class UpdatePeerRoomNotesRequest
{
    public string SharedNotes { get; set; } = string.Empty;
    public string WhiteboardState { get; set; } = string.Empty;
}

