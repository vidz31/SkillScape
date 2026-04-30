using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SkillScape.Application.DTOs;
using SkillScape.Application.Interfaces;
using System.Security.Claims;

namespace SkillScape.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ResumeController : ControllerBase
{
    private readonly IResumeService _resumeService;

    public ResumeController(IResumeService resumeService)
    {
        _resumeService = resumeService;
    }

    /// <summary>
    /// Get resume preview data for the authenticated user
    /// </summary>
    [HttpGet("preview")]
    public async Task<ActionResult<ApiResponse<ResumePreviewDto>>> GetPreview()
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new ApiResponse<ResumePreviewDto>
                {
                    Success = false,
                    Message = "User not authenticated"
                });
            }

            var preview = await _resumeService.GetResumePreviewAsync(userId);

            return Ok(new ApiResponse<ResumePreviewDto>
            {
                Success = true,
                Message = "Resume preview retrieved successfully",
                Data = preview
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new ApiResponse<ResumePreviewDto>
            {
                Success = false,
                Message = ex.Message
            });
        }
    }

    /// <summary>
    /// Generate/download resume PDF (placeholder - would require PDF library)
    /// </summary>
    [HttpGet("generate")]
    public async Task<IActionResult> GeneratePdf()
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var preview = await _resumeService.GetResumePreviewAsync(userId);

            // TODO: Implement PDF generation using QuestPDF or similar library
            // For now, return a placeholder message
            var message = $"Resume for {preview.PersonalInfo.Name}\n\n" +
                         $"Summary: {preview.Summary}\n\n" +
                         $"Skills: {string.Join(", ", preview.Skills)}\n\n" +
                         $"Certifications: {string.Join(", ", preview.Certifications.Select(c => c.Name))}";

            var bytes = System.Text.Encoding.UTF8.GetBytes(message);
            return File(bytes, "text/plain", "resume.txt");
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Get available resume templates (placeholder)
    /// </summary>
    [HttpGet("templates")]
    public ActionResult<ApiResponse<List<string>>> GetTemplates()
    {
        var templates = new List<string> { "Modern", "Classic", "Minimal", "Professional" };

        return Ok(new ApiResponse<List<string>>
        {
            Success = true,
            Message = "Templates retrieved successfully",
            Data = templates
        });
    }
}
