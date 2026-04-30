using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using SkillScape.Application.DTOs;
using SkillScape.Application.Interfaces;

namespace SkillScape.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ProgressController : ControllerBase
{
    private readonly IProgressService _progressService;

    public ProgressController(IProgressService progressService)
    {
        _progressService = progressService;
    }

    /// <summary>
    /// Get current user stats and progress
    /// </summary>
    [HttpGet("me")]
    public async Task<ActionResult<ApiResponse<UserStatsDto>>> GetUserStats()
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(ApiResponse<UserStatsDto>.ErrorResponse("User not authenticated"));

            var result = await _progressService.GetUserStatsAsync(userId);
            return Ok(ApiResponse<UserStatsDto>.SuccessResponse(result, "User stats retrieved"));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<UserStatsDto>.ErrorResponse(ex.Message));
        }
    }

    /// <summary>
    /// Mark a skill as completed
    /// </summary>
    [HttpPost("complete-skill")]
    public async Task<ActionResult<ApiResponse<string>>> CompleteSkill([FromBody] CompleteSkillRequest request)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(ApiResponse<string>.ErrorResponse("User not authenticated"));

            await _progressService.CompleteSkillAsync(userId, request.SkillId);
            return Ok(ApiResponse<string>.SuccessResponse("", "Skill completed successfully"));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<string>.ErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<string>.ErrorResponse(ex.Message));
        }
    }

    /// <summary>
    /// Get user's earned badges
    /// </summary>
    [HttpGet("badges")]
    public async Task<ActionResult<ApiResponse<List<BadgeDto>>>> GetUserBadges()
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(ApiResponse<List<BadgeDto>>.ErrorResponse("User not authenticated"));

            var result = await _progressService.GetUserBadgesAsync(userId);
            return Ok(ApiResponse<List<BadgeDto>>.SuccessResponse(result, "Badges retrieved"));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<List<BadgeDto>>.ErrorResponse(ex.Message));
        }
    }

    /// <summary>
    /// Get Leaderboard
    /// </summary>
    [HttpGet("leaderboard")]
    public async Task<ActionResult<ApiResponse<List<LeaderboardUserDto>>>> GetLeaderboard([FromQuery] int limit = 10)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(ApiResponse<List<LeaderboardUserDto>>.ErrorResponse("User not authenticated"));

            var result = await _progressService.GetLeaderboardAsync(userId, limit);
            return Ok(ApiResponse<List<LeaderboardUserDto>>.SuccessResponse(result, "Leaderboard retrieved"));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<List<LeaderboardUserDto>>.ErrorResponse(ex.Message));
        }
    }
}
