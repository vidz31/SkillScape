using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SkillScape.Application.DTOs;
using SkillScape.Application.Interfaces;
using System.Security.Claims;

namespace SkillScape.API.Controllers;

[ApiController]
[Route("api/admin")]
[Authorize(Roles = "Admin")]
public class AdminController : ControllerBase
{
    private readonly IAdminService _adminService;

    public AdminController(IAdminService adminService)
    {
        _adminService = adminService;
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
}
