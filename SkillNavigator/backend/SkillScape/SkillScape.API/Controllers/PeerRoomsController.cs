using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SkillScape.Application.DTOs;
using SkillScape.Domain.Entities;
using SkillScape.Infrastructure.Data;
using System.Security.Claims;

namespace SkillScape.API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class PeerRoomsController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public PeerRoomsController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<PeerRoomDto>>>> GetRooms()
    {
        var userId = GetCurrentUserId();
        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        var rooms = await _context.PeerLearningRooms
            .Include(r => r.Creator)
            .Include(r => r.Participants)
            .Where(r => r.Status == "Active")
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();

        return Ok(ApiResponse<List<PeerRoomDto>>.SuccessResponse(
            rooms.Select(r => ToRoomDto(r, userId)).ToList(),
            "Peer rooms retrieved"));
    }

    [HttpGet("{roomId}")]
    public async Task<ActionResult<ApiResponse<PeerRoomDetailsDto>>> GetRoom(string roomId)
    {
        var userId = GetCurrentUserId();
        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        var room = await _context.PeerLearningRooms
            .Include(r => r.Creator)
            .Include(r => r.Participants).ThenInclude(p => p.User)
            .Include(r => r.Messages).ThenInclude(m => m.Sender)
            .Include(r => r.Tasks).ThenInclude(t => t.AssignedToUser)
            .FirstOrDefaultAsync(r => r.Id == roomId);

        if (room == null)
            return NotFound(ApiResponse<PeerRoomDetailsDto>.ErrorResponse("Room not found"));

        if (!CanViewRoom(room, userId))
            return Forbid();

        return Ok(ApiResponse<PeerRoomDetailsDto>.SuccessResponse(ToDetailsDto(room, userId), "Peer room retrieved"));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<PeerRoomDto>>> CreateRoom([FromBody] CreatePeerRoomRequest request)
    {
        var userId = GetCurrentUserId();
        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        if (string.IsNullOrWhiteSpace(request.Title) || string.IsNullOrWhiteSpace(request.Description))
            return BadRequest(ApiResponse<PeerRoomDto>.ErrorResponse("Title and description are required"));

        var room = new PeerLearningRoom
        {
            Title = request.Title.Trim(),
            Description = request.Description.Trim(),
            CollaborationType = string.IsNullOrWhiteSpace(request.CollaborationType) ? "Study Group" : request.CollaborationType.Trim(),
            MaxMembers = Math.Clamp(request.MaxMembers, 2, 30),
            CreatorId = userId,
            ScheduledAt = request.ScheduledAt,
            DurationMinutes = Math.Clamp(request.DurationMinutes, 15, 480)
        };

        room.Participants.Add(new PeerRoomParticipant
        {
            RoomId = room.Id,
            UserId = userId,
            Status = "Approved",
            Role = "Creator",
            ApprovedAt = DateTime.UtcNow
        });

        _context.PeerLearningRooms.Add(room);
        await _context.SaveChangesAsync();

        await _context.Entry(room).Reference(r => r.Creator).LoadAsync();

        return Ok(ApiResponse<PeerRoomDto>.SuccessResponse(ToRoomDto(room, userId), "Peer room created"));
    }

    [HttpPost("{roomId}/join")]
    public async Task<ActionResult<ApiResponse<PeerRoomDto>>> RequestToJoin(string roomId)
    {
        var userId = GetCurrentUserId();
        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        var room = await _context.PeerLearningRooms
            .Include(r => r.Creator)
            .Include(r => r.Participants)
            .FirstOrDefaultAsync(r => r.Id == roomId);

        if (room == null)
            return NotFound(ApiResponse<PeerRoomDto>.ErrorResponse("Room not found"));

        var existing = room.Participants.FirstOrDefault(p => p.UserId == userId);
        if (existing == null)
        {
            room.Participants.Add(new PeerRoomParticipant
            {
                RoomId = roomId,
                UserId = userId,
                Status = room.CreatorId == userId ? "Approved" : "Pending",
                Role = room.CreatorId == userId ? "Creator" : "Member",
                ApprovedAt = room.CreatorId == userId ? DateTime.UtcNow : null
            });
        }
        else if (existing.Status == "Rejected")
        {
            existing.Status = "Pending";
            existing.RequestedAt = DateTime.UtcNow;
            existing.ApprovedAt = null;
        }

        await _context.SaveChangesAsync();
        return Ok(ApiResponse<PeerRoomDto>.SuccessResponse(ToRoomDto(room, userId), "Join request submitted"));
    }

    [HttpPut("{roomId}/participants/{participantId}/approve")]
    public async Task<ActionResult<ApiResponse<PeerRoomParticipantDto>>> ApproveParticipant(string roomId, string participantId)
    {
        return await UpdateParticipantStatus(roomId, participantId, "Approved");
    }

    [HttpPut("{roomId}/participants/{participantId}/reject")]
    public async Task<ActionResult<ApiResponse<PeerRoomParticipantDto>>> RejectParticipant(string roomId, string participantId)
    {
        return await UpdateParticipantStatus(roomId, participantId, "Rejected");
    }

    [HttpPost("{roomId}/tasks")]
    public async Task<ActionResult<ApiResponse<PeerRoomTaskDto>>> CreateTask(string roomId, [FromBody] UpsertPeerRoomTaskRequest request)
    {
        var userId = GetCurrentUserId();
        if (string.IsNullOrEmpty(userId)) return Unauthorized();
        if (string.IsNullOrWhiteSpace(request.Title))
            return BadRequest(ApiResponse<PeerRoomTaskDto>.ErrorResponse("Task title is required"));

        var room = await _context.PeerLearningRooms
            .Include(r => r.Participants)
            .FirstOrDefaultAsync(r => r.Id == roomId);

        if (room == null) return NotFound(ApiResponse<PeerRoomTaskDto>.ErrorResponse("Room not found"));
        if (!IsApprovedMember(room, userId)) return Forbid();

        var task = new PeerRoomTask
        {
            RoomId = roomId,
            Title = request.Title.Trim(),
            AssignedToUserId = request.AssignedToUserId
        };

        _context.PeerRoomTasks.Add(task);
        await _context.SaveChangesAsync();
        await _context.Entry(task).Reference(t => t.AssignedToUser).LoadAsync();

        return Ok(ApiResponse<PeerRoomTaskDto>.SuccessResponse(ToTaskDto(task), "Task created"));
    }

    [HttpPut("{roomId}/tasks/{taskId}/toggle")]
    public async Task<ActionResult<ApiResponse<PeerRoomTaskDto>>> ToggleTask(string roomId, string taskId)
    {
        var userId = GetCurrentUserId();
        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        var room = await _context.PeerLearningRooms.Include(r => r.Participants).FirstOrDefaultAsync(r => r.Id == roomId);
        if (room == null) return NotFound(ApiResponse<PeerRoomTaskDto>.ErrorResponse("Room not found"));
        if (!IsApprovedMember(room, userId)) return Forbid();

        var task = await _context.PeerRoomTasks
            .Include(t => t.AssignedToUser)
            .FirstOrDefaultAsync(t => t.Id == taskId && t.RoomId == roomId);

        if (task == null) return NotFound(ApiResponse<PeerRoomTaskDto>.ErrorResponse("Task not found"));

        task.IsCompleted = !task.IsCompleted;
        task.CompletedAt = task.IsCompleted ? DateTime.UtcNow : null;
        await _context.SaveChangesAsync();

        return Ok(ApiResponse<PeerRoomTaskDto>.SuccessResponse(ToTaskDto(task), "Task updated"));
    }

    [HttpPut("{roomId}/notes")]
    public async Task<ActionResult<ApiResponse<bool>>> UpdateNotes(string roomId, [FromBody] UpdatePeerRoomNotesRequest request)
    {
        var userId = GetCurrentUserId();
        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        var room = await _context.PeerLearningRooms.Include(r => r.Participants).FirstOrDefaultAsync(r => r.Id == roomId);
        if (room == null) return NotFound(ApiResponse<bool>.ErrorResponse("Room not found"));
        if (!IsApprovedMember(room, userId)) return Forbid();

        room.SharedNotes = request.SharedNotes ?? string.Empty;
        room.WhiteboardState = request.WhiteboardState ?? string.Empty;
        room.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return Ok(ApiResponse<bool>.SuccessResponse(true, "Workspace saved"));
    }

    private async Task<ActionResult<ApiResponse<PeerRoomParticipantDto>>> UpdateParticipantStatus(string roomId, string participantId, string status)
    {
        var userId = GetCurrentUserId();
        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        var room = await _context.PeerLearningRooms
            .Include(r => r.Participants).ThenInclude(p => p.User)
            .FirstOrDefaultAsync(r => r.Id == roomId);

        if (room == null)
            return NotFound(ApiResponse<PeerRoomParticipantDto>.ErrorResponse("Room not found"));

        if (room.CreatorId != userId && User.FindFirst(ClaimTypes.Role)?.Value != "Admin")
            return Forbid();

        var participant = room.Participants.FirstOrDefault(p => p.Id == participantId);
        if (participant == null)
            return NotFound(ApiResponse<PeerRoomParticipantDto>.ErrorResponse("Participant not found"));

        participant.Status = status;
        participant.ApprovedAt = status == "Approved" ? DateTime.UtcNow : null;
        await _context.SaveChangesAsync();

        return Ok(ApiResponse<PeerRoomParticipantDto>.SuccessResponse(ToParticipantDto(participant), $"Participant {status.ToLower()}"));
    }

    private string? GetCurrentUserId()
    {
        return User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    }

    private static bool CanViewRoom(PeerLearningRoom room, string userId)
    {
        return room.CreatorId == userId || room.Participants.Any(p => p.UserId == userId && p.Status == "Approved");
    }

    private static bool IsApprovedMember(PeerLearningRoom room, string userId)
    {
        return room.Participants.Any(p => p.UserId == userId && p.Status == "Approved");
    }

    private static PeerRoomDto ToRoomDto(PeerLearningRoom room, string userId)
    {
        var participant = room.Participants.FirstOrDefault(p => p.UserId == userId);
        return new PeerRoomDto
        {
            Id = room.Id,
            Title = room.Title,
            Description = room.Description,
            CollaborationType = room.CollaborationType,
            MaxMembers = room.MaxMembers,
            CreatorId = room.CreatorId,
            CreatorName = room.Creator?.FullName ?? "Unknown",
            Status = room.Status,
            ScheduledAt = room.ScheduledAt,
            DurationMinutes = room.DurationMinutes,
            ApprovedMembers = room.Participants.Count(p => p.Status == "Approved"),
            CurrentUserStatus = participant?.Status ?? "None",
            CanManage = room.CreatorId == userId
        };
    }

    private static PeerRoomDetailsDto ToDetailsDto(PeerLearningRoom room, string userId)
    {
        var dto = new PeerRoomDetailsDto
        {
            SharedNotes = room.SharedNotes,
            WhiteboardState = room.WhiteboardState,
            Participants = room.Participants.OrderBy(p => p.Status).ThenBy(p => p.User!.FullName).Select(ToParticipantDto).ToList(),
            Messages = room.Messages.OrderBy(m => m.SentAt).TakeLast(80).Select(ToMessageDto).ToList(),
            Tasks = room.Tasks.OrderBy(t => t.IsCompleted).ThenByDescending(t => t.CreatedAt).Select(ToTaskDto).ToList()
        };

        var summary = ToRoomDto(room, userId);
        dto.Id = summary.Id;
        dto.Title = summary.Title;
        dto.Description = summary.Description;
        dto.CollaborationType = summary.CollaborationType;
        dto.MaxMembers = summary.MaxMembers;
        dto.CreatorId = summary.CreatorId;
        dto.CreatorName = summary.CreatorName;
        dto.Status = summary.Status;
        dto.ScheduledAt = summary.ScheduledAt;
        dto.DurationMinutes = summary.DurationMinutes;
        dto.ApprovedMembers = summary.ApprovedMembers;
        dto.CurrentUserStatus = summary.CurrentUserStatus;
        dto.CanManage = summary.CanManage;
        return dto;
    }

    private static PeerRoomParticipantDto ToParticipantDto(PeerRoomParticipant participant)
    {
        return new PeerRoomParticipantDto
        {
            Id = participant.Id,
            UserId = participant.UserId,
            Name = participant.User?.FullName ?? "Unknown",
            Avatar = participant.User?.ProfileImageUrl,
            Status = participant.Status,
            Role = participant.Role,
            RequestedAt = participant.RequestedAt,
            LastSeenAt = participant.LastSeenAt
        };
    }

    private static PeerRoomMessageDto ToMessageDto(PeerRoomMessage message)
    {
        return new PeerRoomMessageDto
        {
            Id = message.Id,
            SenderId = message.SenderId,
            SenderName = message.Sender?.FullName ?? "Unknown",
            Content = message.Content,
            SentAt = message.SentAt
        };
    }

    private static PeerRoomTaskDto ToTaskDto(PeerRoomTask task)
    {
        return new PeerRoomTaskDto
        {
            Id = task.Id,
            Title = task.Title,
            AssignedToUserId = task.AssignedToUserId,
            AssignedToName = task.AssignedToUser?.FullName,
            IsCompleted = task.IsCompleted
        };
    }
}

