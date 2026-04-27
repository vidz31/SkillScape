using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SkillScape.Application.DTOs;
using SkillScape.Application.Interfaces;
using SkillScape.Domain.Entities;
using SkillScape.Infrastructure.Data;
using System.Security.Claims;

namespace SkillScape.API.Controllers;

[ApiController]
[Route("api/session")]
[Authorize]
public class SessionController : ControllerBase
{
    private readonly IMentorService _mentorService;
    private readonly ApplicationDbContext _context;

    public SessionController(IMentorService mentorService, ApplicationDbContext context)
    {
        _mentorService = mentorService;
        _context = context;
    }

    public class ReportSessionComplaintRequest
    {
        public string SessionId { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
    }

    [HttpPost("book")]
    public async Task<ActionResult<ApiResponse<MentorSessionDto>>> BookSession([FromBody] BookSessionDto request)
    {
        try
        {
            var studentId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(studentId))
                return Unauthorized(ApiResponse<MentorSessionDto>.ErrorResponse("User not authenticated"));

            var result = await _mentorService.BookSessionAsync(studentId, request);
            return Ok(ApiResponse<MentorSessionDto>.SuccessResponse(result, "Session booking request created"));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<MentorSessionDto>.ErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<MentorSessionDto>.ErrorResponse(ex.Message));
        }
    }

    [HttpPost("mentor-create")]
    public async Task<ActionResult<ApiResponse<MentorSessionDto>>> CreateSessionForStudent([FromBody] CreateMentorSessionDto request)
    {
        try
        {
            var mentorUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(mentorUserId))
                return Unauthorized(ApiResponse<MentorSessionDto>.ErrorResponse("User not authenticated"));

            var result = await _mentorService.CreateSessionForStudentAsync(mentorUserId, request);
            return Ok(ApiResponse<MentorSessionDto>.SuccessResponse(result, "Session created successfully"));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<MentorSessionDto>.ErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<MentorSessionDto>.ErrorResponse(ex.Message));
        }
    }

    [HttpPut("approve")]
    public async Task<ActionResult<ApiResponse<MentorSessionDto>>> ApproveSession([FromBody] ApproveSessionDto request)
    {
        try
        {
            var mentorUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(mentorUserId))
                return Unauthorized(ApiResponse<MentorSessionDto>.ErrorResponse("User not authenticated"));

            var result = await _mentorService.ApproveSessionAsync(mentorUserId, request);
            return Ok(ApiResponse<MentorSessionDto>.SuccessResponse(result, "Session status updated"));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<MentorSessionDto>.ErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<MentorSessionDto>.ErrorResponse(ex.Message));
        }
    }

    [HttpGet("my-sessions")]
    public async Task<ActionResult<ApiResponse<List<MentorSessionDto>>>> GetMySessions()
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(ApiResponse<List<MentorSessionDto>>.ErrorResponse("User not authenticated"));

            var result = await _mentorService.GetMySessionsAsync(userId);
            return Ok(ApiResponse<List<MentorSessionDto>>.SuccessResponse(result, "Sessions retrieved"));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<List<MentorSessionDto>>.ErrorResponse(ex.Message));
        }
    }

    [HttpGet("upcoming")]
    public async Task<ActionResult<ApiResponse<List<MentorSessionDto>>>> GetUpcomingSessions()
    {
        try
        {
            var studentId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(studentId))
                return Unauthorized(ApiResponse<List<MentorSessionDto>>.ErrorResponse("User not authenticated"));

            var result = await _mentorService.GetStudentUpcomingSessionsAsync(studentId);
            return Ok(ApiResponse<List<MentorSessionDto>>.SuccessResponse(result, "Upcoming sessions retrieved"));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<List<MentorSessionDto>>.ErrorResponse(ex.Message));
        }
    }

    [HttpGet("{sessionId}")]
    public async Task<ActionResult<ApiResponse<MentorSessionDto>>> GetSessionById(string sessionId)
    {
        try
        {
            var result = await _mentorService.GetSessionByIdAsync(sessionId);
            return Ok(ApiResponse<MentorSessionDto>.SuccessResponse(result, "Session retrieved"));
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(ApiResponse<MentorSessionDto>.ErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<MentorSessionDto>.ErrorResponse(ex.Message));
        }
    }

    [HttpPost("update/{sessionId}")]
    public async Task<ActionResult<ApiResponse<MentorSessionDto>>> UpdateSession(string sessionId, [FromBody] UpdateSessionDto request)
    {
        try
        {
            var mentorUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(mentorUserId))
                return Unauthorized(ApiResponse<MentorSessionDto>.ErrorResponse("User not authenticated"));

            var result = await _mentorService.UpdateSessionAsync(mentorUserId, sessionId, request);
            return Ok(ApiResponse<MentorSessionDto>.SuccessResponse(result, "Session updated successfully"));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<MentorSessionDto>.ErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<MentorSessionDto>.ErrorResponse(ex.Message));
        }
    }

    [HttpDelete("{sessionId}")]
    public async Task<ActionResult<ApiResponse<bool>>> DeleteSession(string sessionId)
    {
        try
        {
            var mentorUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(mentorUserId))
                return Unauthorized(ApiResponse<bool>.ErrorResponse("User not authenticated"));

            var result = await _mentorService.DeleteSessionAsync(mentorUserId, sessionId);
            return Ok(ApiResponse<bool>.SuccessResponse(result, "Session cancelled successfully"));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<bool>.ErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<bool>.ErrorResponse(ex.Message));
        }
    }

    [HttpPost("complaint")]
    public async Task<ActionResult<ApiResponse<bool>>> ReportComplaint([FromBody] ReportSessionComplaintRequest request)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(ApiResponse<bool>.ErrorResponse("User not authenticated"));

            var sessionExists = await _context.MentorSessions.FindAsync(request.SessionId);
            if (sessionExists == null)
                return BadRequest(ApiResponse<bool>.ErrorResponse("Session not found"));

            var complaint = new SessionComplaint
            {
                Id = Guid.NewGuid().ToString(),
                SessionId = request.SessionId,
                ReportedBy = userId,
                Reason = request.Reason,
                Status = "Open",
                CreatedAt = DateTime.UtcNow
            };

            _context.SessionComplaints.Add(complaint);
            await _context.SaveChangesAsync();

            return Ok(ApiResponse<bool>.SuccessResponse(true, "Complaint submitted"));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<bool>.ErrorResponse(ex.Message));
        }
    }
}
