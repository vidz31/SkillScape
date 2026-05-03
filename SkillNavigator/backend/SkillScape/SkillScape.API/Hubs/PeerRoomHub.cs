using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using SkillScape.Application.DTOs;
using SkillScape.Domain.Entities;
using SkillScape.Infrastructure.Data;
using System.Security.Claims;

namespace SkillScape.API.Hubs;

[Authorize]
public class PeerRoomHub : Hub
{
    private readonly ApplicationDbContext _context;

    public PeerRoomHub(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task JoinRoom(string roomId)
    {
        var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId)) return;

        var participant = await _context.PeerRoomParticipants
            .Include(p => p.User)
            .FirstOrDefaultAsync(p => p.RoomId == roomId && p.UserId == userId && p.Status == "Approved");

        if (participant == null) return;

        participant.LastSeenAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        await Groups.AddToGroupAsync(Context.ConnectionId, roomId);
        await Clients.Group(roomId).SendAsync("ParticipantOnline", new
        {
            participant.UserId,
            Name = participant.User?.FullName ?? "Unknown",
            participant.LastSeenAt
        });
    }

    public async Task LeaveRoom(string roomId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, roomId);
    }

    public async Task SendRoomMessage(string roomId, string content)
    {
        var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId) || string.IsNullOrWhiteSpace(content)) return;

        var isMember = await _context.PeerRoomParticipants
            .AnyAsync(p => p.RoomId == roomId && p.UserId == userId && p.Status == "Approved");

        if (!isMember) return;

        var message = new PeerRoomMessage
        {
            RoomId = roomId,
            SenderId = userId,
            Content = content.Trim()
        };

        _context.PeerRoomMessages.Add(message);
        await _context.SaveChangesAsync();
        await _context.Entry(message).Reference(m => m.Sender).LoadAsync();

        await Clients.Group(roomId).SendAsync("ReceiveRoomMessage", new PeerRoomMessageDto
        {
            Id = message.Id,
            SenderId = message.SenderId,
            SenderName = message.Sender?.FullName ?? "Unknown",
            Content = message.Content,
            SentAt = message.SentAt
        });
    }

    public async Task BroadcastWorkspaceUpdate(string roomId, string sharedNotes, string whiteboardState)
    {
        var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId)) return;

        var room = await _context.PeerLearningRooms
            .Include(r => r.Participants)
            .FirstOrDefaultAsync(r => r.Id == roomId);

        if (room == null || !room.Participants.Any(p => p.UserId == userId && p.Status == "Approved")) return;

        room.SharedNotes = sharedNotes ?? string.Empty;
        room.WhiteboardState = whiteboardState ?? string.Empty;
        room.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        await Clients.OthersInGroup(roomId).SendAsync("WorkspaceUpdated", new
        {
            SharedNotes = room.SharedNotes,
            WhiteboardState = room.WhiteboardState,
            UpdatedBy = userId
        });
    }

    public async Task SendWebRtcSignal(string roomId, string signalType, string payload)
    {
        var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId)) return;

        var isMember = await _context.PeerRoomParticipants
            .AnyAsync(p => p.RoomId == roomId && p.UserId == userId && p.Status == "Approved");

        if (!isMember) return;

        await Clients.OthersInGroup(roomId).SendAsync("ReceiveWebRtcSignal", new
        {
            SenderId = userId,
            SignalType = signalType,
            Payload = payload,
            SentAt = DateTime.UtcNow
        });
    }
}
