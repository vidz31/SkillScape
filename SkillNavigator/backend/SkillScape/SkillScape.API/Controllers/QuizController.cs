using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using SkillScape.Application.DTOs;
using SkillScape.Application.Interfaces;

namespace SkillScape.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class QuizController : ControllerBase
{
    private readonly IQuizService _quizService;

    public QuizController(IQuizService quizService)
    {
        _quizService = quizService;
    }

    /// <summary>
    /// Get all quiz questions with options
    /// </summary>
    [HttpGet("questions")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<List<QuizQuestionDto>>>> GetAllQuestions()
    {
        try
        {
            var result = await _quizService.GetAllQuestionsAsync();
            return Ok(ApiResponse<List<QuizQuestionDto>>.SuccessResponse(result, "Quiz questions retrieved"));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<List<QuizQuestionDto>>.ErrorResponse(ex.Message));
        }
    }

    /// <summary>
    /// Get the next quiz question dynamically based on current answers
    /// </summary>
    [HttpPost("next")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<QuizQuestionDto?>>> GetNextQuestion([FromBody] SubmitQuizRequest request)
    {
        try
        {
            var result = await _quizService.GetNextQuestionAsync(request.Responses);
            return Ok(ApiResponse<QuizQuestionDto?>.SuccessResponse(result, "Next question retrieved"));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<QuizQuestionDto?>.ErrorResponse(ex.Message));
        }
    }


    /// <summary>
    /// Submit quiz answers and get recommendation (Preview only)
    /// </summary>
    [HttpPost("submit")]
    public async Task<ActionResult<ApiResponse<QuizResultDto>>> SubmitQuiz([FromBody] SubmitQuizRequest request)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(ApiResponse<QuizResultDto>.ErrorResponse("User not authenticated"));

            var result = await _quizService.SubmitQuizAsync(userId, request);
            return Ok(ApiResponse<QuizResultDto>.SuccessResponse(result, "Quiz recommendation generated"));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<QuizResultDto>.ErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<QuizResultDto>.ErrorResponse(ex.Message));
        }
    }

    /// <summary>
    /// Confirm and save the quiz result
    /// </summary>
    [HttpPost("confirm")]
    public async Task<ActionResult<ApiResponse<QuizResultDto>>> ConfirmQuiz([FromBody] ConfirmQuizRequest request)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(ApiResponse<QuizResultDto>.ErrorResponse("User not authenticated"));

            var result = await _quizService.ConfirmQuizAsync(userId, request.RecommendedDomainId);
            return Ok(ApiResponse<QuizResultDto>.SuccessResponse(result, "Quiz result confirmed and saved successfully"));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<QuizResultDto>.ErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<QuizResultDto>.ErrorResponse(ex.Message));
        }
    }

    /// <summary>
    /// Get last quiz result
    /// </summary>
    [HttpGet("result")]
    public async Task<ActionResult<ApiResponse<QuizResultDto>>> GetLastResult()
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(ApiResponse<QuizResultDto>.ErrorResponse("User not authenticated"));

            var result = await _quizService.GetLastResultAsync(userId);
            return Ok(ApiResponse<QuizResultDto>.SuccessResponse(result, "Quiz result retrieved"));
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(ApiResponse<QuizResultDto>.ErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<QuizResultDto>.ErrorResponse(ex.Message));
        }
    }
}
