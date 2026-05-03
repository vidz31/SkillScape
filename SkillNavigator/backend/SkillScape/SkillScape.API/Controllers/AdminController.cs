using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SkillScape.Application.DTOs;
using SkillScape.Application.Interfaces;
using SkillScape.Domain.Entities;
using SkillScape.Infrastructure.Data;
using System.Security.Claims;

namespace SkillScape.API.Controllers;

[ApiController]
[Route("api/admin")]
[Authorize(Roles = "Admin")]
public class AdminController : ControllerBase
{
    private readonly IAdminService _adminService;
    private readonly ApplicationDbContext _context;

    public AdminController(IAdminService adminService, ApplicationDbContext context)
    {
        _adminService = adminService;
        _context = context;
    }

    [HttpGet("dashboard")]
    public async Task<ActionResult<ApiResponse<AdminDashboardStatsDto>>> GetDashboard()
    {
        var result = await _adminService.GetDashboardAsync();
        return Ok(ApiResponse<AdminDashboardStatsDto>.SuccessResponse(result, "Dashboard stats retrieved"));
    }

    [HttpGet("users")]
    public async Task<ActionResult<ApiResponse<List<AdminUserDto>>>> GetUsers([FromQuery] string? role, [FromQuery] bool? isBlocked)
    {
        var result = await _adminService.GetUsersAsync(role, isBlocked);
        return Ok(ApiResponse<List<AdminUserDto>>.SuccessResponse(result, "Users retrieved"));
    }

    [HttpPut("block-user")]
    public async Task<ActionResult<ApiResponse<bool>>> BlockUser([FromBody] BlockUserDto request)
    {
        try
        {
            var adminId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty;
            await _adminService.BlockOrUnblockUserAsync(adminId, request);
            return Ok(ApiResponse<bool>.SuccessResponse(true, request.IsBlocked ? "User blocked" : "User unblocked"));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<bool>.ErrorResponse(ex.Message));
        }
    }

    [HttpPut("users/role")]
    public async Task<ActionResult<ApiResponse<bool>>> UpdateUserRole([FromBody] UpdateUserRoleDto request)
    {
        try
        {
            var adminId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty;
            await _adminService.UpdateUserRoleAsync(adminId, request);
            return Ok(ApiResponse<bool>.SuccessResponse(true, "Role updated"));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<bool>.ErrorResponse(ex.Message));
        }
    }

    [HttpGet("mentors/pending")]
    public async Task<ActionResult<ApiResponse<List<MentorDto>>>> GetPendingMentors()
    {
        var result = await _adminService.GetPendingMentorsAsync();
        return Ok(ApiResponse<List<MentorDto>>.SuccessResponse(result, "Pending mentors retrieved"));
    }

    [HttpPut("mentor/approve/{mentorId}")]
    public async Task<ActionResult<ApiResponse<MentorDto>>> ApproveMentor(string mentorId)
    {
        try
        {
            var adminId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty;
            var result = await _adminService.ApproveMentorAsync(adminId, mentorId);
            return Ok(ApiResponse<MentorDto>.SuccessResponse(result, "Mentor approved"));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<MentorDto>.ErrorResponse(ex.Message));
        }
    }

    [HttpPut("mentor/reject/{mentorId}")]
    public async Task<ActionResult<ApiResponse<MentorDto>>> RejectMentor(string mentorId, [FromBody] RejectMentorDto request)
    {
        try
        {
            var adminId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty;
            var result = await _adminService.RejectMentorAsync(adminId, mentorId, request.Reason);
            return Ok(ApiResponse<MentorDto>.SuccessResponse(result, "Mentor rejected"));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<MentorDto>.ErrorResponse(ex.Message));
        }
    }

    [HttpGet("sessions")]
    public async Task<ActionResult<ApiResponse<List<AdminSessionDto>>>> GetSessions([FromQuery] string? status)
    {
        var result = await _adminService.GetSessionsAsync(status);
        return Ok(ApiResponse<List<AdminSessionDto>>.SuccessResponse(result, "Sessions retrieved"));
    }

    [HttpGet("analytics")]
    public async Task<ActionResult<ApiResponse<PlatformAnalyticsDto>>> GetAnalytics()
    {
        var result = await _adminService.GetAnalyticsAsync();
        return Ok(ApiResponse<PlatformAnalyticsDto>.SuccessResponse(result, "Analytics retrieved"));
    }

    [HttpPost("announcement")]
    public async Task<ActionResult<ApiResponse<bool>>> BroadcastAnnouncement([FromBody] AdminAnnouncementDto request)
    {
        try
        {
            var adminId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty;
            await _adminService.BroadcastAnnouncementAsync(adminId, request.Message);
            return Ok(ApiResponse<bool>.SuccessResponse(true, "Announcement broadcasted"));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<bool>.ErrorResponse(ex.Message));
        }
    }

    [HttpGet("sessions/complaints")]
    public async Task<ActionResult<ApiResponse<List<SessionComplaintDto>>>> GetComplaints([FromQuery] string? status)
    {
        var result = await _adminService.GetComplaintsAsync(status);
        return Ok(ApiResponse<List<SessionComplaintDto>>.SuccessResponse(result, "Complaints retrieved"));
    }

    [HttpPut("sessions/complaints/{complaintId}/resolve")]
    public async Task<ActionResult<ApiResponse<SessionComplaintDto>>> ResolveComplaint(string complaintId, [FromBody] ResolveComplaintDto request)
    {
        try
        {
            var adminId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty;
            var result = await _adminService.ResolveComplaintAsync(adminId, complaintId, request.ResolutionNote);
            return Ok(ApiResponse<SessionComplaintDto>.SuccessResponse(result, "Complaint resolved"));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<SessionComplaintDto>.ErrorResponse(ex.Message));
        }
    }

    [HttpGet("quiz/questions")]
    public async Task<ActionResult<ApiResponse<List<AdminQuizQuestionCrudDto>>>> GetQuizQuestions()
    {
        var result = await _adminService.GetQuizQuestionsAsync();
        return Ok(ApiResponse<List<AdminQuizQuestionCrudDto>>.SuccessResponse(result, "Quiz questions retrieved"));
    }

    [HttpPost("quiz/questions")]
    public async Task<ActionResult<ApiResponse<AdminQuizQuestionCrudDto>>> CreateQuizQuestion([FromBody] UpsertAdminQuizQuestionDto request)
    {
        try
        {
            var adminId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty;
            var result = await _adminService.CreateQuizQuestionAsync(adminId, request);
            return Ok(ApiResponse<AdminQuizQuestionCrudDto>.SuccessResponse(result, "Quiz question created"));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<AdminQuizQuestionCrudDto>.ErrorResponse(ex.Message));
        }
    }

    [HttpPut("quiz/questions/{questionId}")]
    public async Task<ActionResult<ApiResponse<AdminQuizQuestionCrudDto>>> UpdateQuizQuestion(string questionId, [FromBody] UpsertAdminQuizQuestionDto request)
    {
        try
        {
            var adminId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty;
            var result = await _adminService.UpdateQuizQuestionAsync(adminId, questionId, request);
            return Ok(ApiResponse<AdminQuizQuestionCrudDto>.SuccessResponse(result, "Quiz question updated"));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<AdminQuizQuestionCrudDto>.ErrorResponse(ex.Message));
        }
    }

    [HttpDelete("quiz/questions/{questionId}")]
    public async Task<ActionResult<ApiResponse<bool>>> DeleteQuizQuestion(string questionId)
    {
        try
        {
            var adminId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty;
            await _adminService.DeleteQuizQuestionAsync(adminId, questionId);
            return Ok(ApiResponse<bool>.SuccessResponse(true, "Quiz question deleted"));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<bool>.ErrorResponse(ex.Message));
        }
    }

    [HttpGet("roadmap/modules")]
    public async Task<ActionResult<ApiResponse<List<AdminRoadmapModuleCrudDto>>>> GetRoadmapModules([FromQuery] string? domainId)
    {
        var result = await _adminService.GetRoadmapModulesAsync(domainId);
        return Ok(ApiResponse<List<AdminRoadmapModuleCrudDto>>.SuccessResponse(result, "Roadmap modules retrieved"));
    }

    [HttpPost("roadmap/modules")]
    public async Task<ActionResult<ApiResponse<AdminRoadmapModuleCrudDto>>> CreateRoadmapModule([FromBody] UpsertAdminRoadmapModuleDto request)
    {
        try
        {
            var adminId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty;
            var result = await _adminService.CreateRoadmapModuleAsync(adminId, request);
            return Ok(ApiResponse<AdminRoadmapModuleCrudDto>.SuccessResponse(result, "Roadmap module created"));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<AdminRoadmapModuleCrudDto>.ErrorResponse(ex.Message));
        }
    }

    [HttpPut("roadmap/modules/{moduleId}")]
    public async Task<ActionResult<ApiResponse<AdminRoadmapModuleCrudDto>>> UpdateRoadmapModule(string moduleId, [FromBody] UpsertAdminRoadmapModuleDto request)
    {
        try
        {
            var adminId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty;
            var result = await _adminService.UpdateRoadmapModuleAsync(adminId, moduleId, request);
            return Ok(ApiResponse<AdminRoadmapModuleCrudDto>.SuccessResponse(result, "Roadmap module updated"));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<AdminRoadmapModuleCrudDto>.ErrorResponse(ex.Message));
        }
    }

    [HttpDelete("roadmap/modules/{moduleId}")]
    public async Task<ActionResult<ApiResponse<bool>>> DeleteRoadmapModule(string moduleId)
    {
        try
        {
            var adminId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty;
            await _adminService.DeleteRoadmapModuleAsync(adminId, moduleId);
            return Ok(ApiResponse<bool>.SuccessResponse(true, "Roadmap module deleted"));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<bool>.ErrorResponse(ex.Message));
        }
    }

    [HttpGet("mentors")]
    public async Task<ActionResult<ApiResponse<List<AdminMentorCrudDto>>>> GetMentors()
    {
        var result = await _adminService.GetAllMentorsAsync();
        return Ok(ApiResponse<List<AdminMentorCrudDto>>.SuccessResponse(result, "Mentors retrieved"));
    }

    [HttpPost("mentors")]
    public async Task<ActionResult<ApiResponse<AdminMentorCrudDto>>> CreateMentor([FromBody] CreateAdminMentorDto request)
    {
        try
        {
            var adminId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty;
            var result = await _adminService.CreateMentorAsync(adminId, request);
            return Ok(ApiResponse<AdminMentorCrudDto>.SuccessResponse(result, "Mentor created"));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<AdminMentorCrudDto>.ErrorResponse(ex.Message));
        }
    }

    [HttpPut("mentors/{mentorId}/block")]
    public async Task<ActionResult<ApiResponse<bool>>> BlockMentor(string mentorId, [FromBody] AdminBlockMentorDto request)
    {
        try
        {
            var adminId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty;
            await _adminService.BlockOrUnblockMentorAsync(adminId, mentorId, request);
            return Ok(ApiResponse<bool>.SuccessResponse(true, request.IsBlocked ? "Mentor blocked" : "Mentor unblocked"));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<bool>.ErrorResponse(ex.Message));
        }
    }

    [HttpDelete("mentors/{mentorId}")]
    public async Task<ActionResult<ApiResponse<bool>>> DeleteMentor(string mentorId)
    {
        try
        {
            var adminId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty;
            await _adminService.DeleteMentorAsync(adminId, mentorId);
            return Ok(ApiResponse<bool>.SuccessResponse(true, "Mentor removed"));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<bool>.ErrorResponse(ex.Message));
        }
    }

    [HttpGet("resumes")]
    public async Task<ActionResult<ApiResponse<List<AdminResumeSummaryDto>>>> GetResumes()
    {
        var result = await _adminService.GetResumeSummariesAsync();
        return Ok(ApiResponse<List<AdminResumeSummaryDto>>.SuccessResponse(result, "Resume summaries retrieved"));
    }

    [HttpGet("resumes/{userId}")]
    public async Task<ActionResult<ApiResponse<ResumePreviewDto>>> GetResumeByUserId(string userId)
    {
        try
        {
            var result = await _adminService.GetResumeByUserIdAsync(userId);
            return Ok(ApiResponse<ResumePreviewDto>.SuccessResponse(result, "Resume retrieved"));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<ResumePreviewDto>.ErrorResponse(ex.Message));
        }
    }

    [HttpGet("progress/snapshot")]
    public async Task<ActionResult<ApiResponse<List<AdminUserProgressSnapshotDto>>>> GetProgressSnapshot()
    {
        var result = await _adminService.GetUserProgressSnapshotAsync();
        return Ok(ApiResponse<List<AdminUserProgressSnapshotDto>>.SuccessResponse(result, "User progress snapshot retrieved"));
    }

    [HttpGet("peer-rooms")]
    public async Task<ActionResult<ApiResponse<List<PeerRoomDto>>>> GetPeerRooms()
    {
        var rooms = await _context.PeerLearningRooms
            .Include(r => r.Creator)
            .Include(r => r.Participants)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();

        var result = rooms.Select(ToAdminRoomDto).ToList();
        return Ok(ApiResponse<List<PeerRoomDto>>.SuccessResponse(result, "Peer learning rooms retrieved"));
    }

    [HttpGet("peer-rooms/{roomId}")]
    public async Task<ActionResult<ApiResponse<PeerRoomDetailsDto>>> GetPeerRoomDetails(string roomId)
    {
        var room = await _context.PeerLearningRooms
            .Include(r => r.Creator)
            .Include(r => r.Participants).ThenInclude(p => p.User)
            .Include(r => r.Messages).ThenInclude(m => m.Sender)
            .Include(r => r.Tasks).ThenInclude(t => t.AssignedToUser)
            .FirstOrDefaultAsync(r => r.Id == roomId);

        if (room == null)
        {
            return NotFound(ApiResponse<PeerRoomDetailsDto>.ErrorResponse("Peer room not found"));
        }

        return Ok(ApiResponse<PeerRoomDetailsDto>.SuccessResponse(ToAdminRoomDetailsDto(room), "Peer learning room details retrieved"));
    }

    private static PeerRoomDto ToAdminRoomDto(PeerLearningRoom room)
    {
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
            CurrentUserStatus = "Admin",
            CanManage = true
        };
    }

    private static PeerRoomDetailsDto ToAdminRoomDetailsDto(PeerLearningRoom room)
    {
        var summary = ToAdminRoomDto(room);
        return new PeerRoomDetailsDto
        {
            Id = summary.Id,
            Title = summary.Title,
            Description = summary.Description,
            CollaborationType = summary.CollaborationType,
            MaxMembers = summary.MaxMembers,
            CreatorId = summary.CreatorId,
            CreatorName = summary.CreatorName,
            Status = summary.Status,
            ScheduledAt = summary.ScheduledAt,
            DurationMinutes = summary.DurationMinutes,
            ApprovedMembers = summary.ApprovedMembers,
            CurrentUserStatus = summary.CurrentUserStatus,
            CanManage = summary.CanManage,
            SharedNotes = room.SharedNotes,
            WhiteboardState = room.WhiteboardState,
            Participants = room.Participants
                .OrderBy(p => p.Status)
                .ThenBy(p => p.User?.FullName)
                .Select(ToAdminParticipantDto)
                .ToList(),
            Messages = room.Messages
                .OrderByDescending(m => m.SentAt)
                .Take(100)
                .OrderBy(m => m.SentAt)
                .Select(ToAdminMessageDto)
                .ToList(),
            Tasks = room.Tasks
                .OrderBy(t => t.IsCompleted)
                .ThenByDescending(t => t.CreatedAt)
                .Select(ToAdminTaskDto)
                .ToList()
        };
    }

    private static PeerRoomParticipantDto ToAdminParticipantDto(PeerRoomParticipant participant)
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

    private static PeerRoomMessageDto ToAdminMessageDto(PeerRoomMessage message)
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

    private static PeerRoomTaskDto ToAdminTaskDto(PeerRoomTask task)
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
