using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using SkillScape.Application.DTOs;
using SkillScape.Application.Interfaces;
using Microsoft.AspNetCore.SignalR;
using SkillScape.API.Hubs;

namespace SkillScape.API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class RoadmapController : ControllerBase
{
    private readonly IRoadmapService _roadmapService;
    private readonly IHubContext<ChatHub> _hubContext;

    public RoadmapController(IRoadmapService roadmapService, IHubContext<ChatHub> hubContext)
    {
        _roadmapService = roadmapService;
        _hubContext = hubContext;
    }

    [HttpGet("my-interest")]
    public async Task<ActionResult<ApiResponse<RoadmapDto>>> GetMyRoadmap()
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(ApiResponse<RoadmapDto>.ErrorResponse("User not authenticated"));

            var result = await _roadmapService.GetMyRoadmapAsync(userId);
            return Ok(ApiResponse<RoadmapDto>.SuccessResponse(result, "Roadmap retrieved successfully"));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<RoadmapDto>.ErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<RoadmapDto>.ErrorResponse(ex.Message));
        }
    }

    [HttpGet("options")]
    public async Task<ActionResult<ApiResponse<List<RoadmapOptionDto>>>> GetRoadmapOptions()
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(ApiResponse<List<RoadmapOptionDto>>.ErrorResponse("User not authenticated"));

            var result = await _roadmapService.GetRoadmapOptionsAsync(userId);
            return Ok(ApiResponse<List<RoadmapOptionDto>>.SuccessResponse(result, "Roadmap options retrieved successfully"));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<List<RoadmapOptionDto>>.ErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<List<RoadmapOptionDto>>.ErrorResponse(ex.Message));
        }
    }

    [HttpGet("domain/{domainId}")]
    public async Task<ActionResult<ApiResponse<RoadmapDto>>> GetRoadmapByDomain(string domainId)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(ApiResponse<RoadmapDto>.ErrorResponse("User not authenticated"));

            var result = await _roadmapService.GetRoadmapByDomainAsync(userId, domainId);
            return Ok(ApiResponse<RoadmapDto>.SuccessResponse(result, "Roadmap retrieved successfully"));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<RoadmapDto>.ErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<RoadmapDto>.ErrorResponse(ex.Message));
        }
    }

    [HttpPost("mark-done/{stepId}")]
    public async Task<ActionResult<ApiResponse<bool>>> MarkStepComplete(string stepId)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(ApiResponse<bool>.ErrorResponse("User not authenticated"));

            var result = await _roadmapService.MarkStepCompleteAsync(userId, stepId);
            
            // Fire global realtime notification
            if (result)
            {
                await _hubContext.Clients.Group(userId).SendAsync("ReceiveNotification", new 
                {
                    Title = "Step Completed!",
                    Message = "You successfully completed a roadmap step. XP added!",
                    Type = "achievement"
                });
            }
            
            return Ok(ApiResponse<bool>.SuccessResponse(result, "Step marked complete successfully"));
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
}
