using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using SkillScape.Application.DTOs;
using SkillScape.Application.Interfaces;

namespace SkillScape.API.Controllers;

[ApiController]
[Route("api/mentors")]
[Route("api/mentor")]
[Authorize]
public class MentorsController : ControllerBase
{
    private readonly IMentorService _mentorService;

    public MentorsController(IMentorService mentorService)
    {
        _mentorService = mentorService;
    }

    [HttpPost("create-profile")]
    public async Task<ActionResult<ApiResponse<MentorDto>>> CreateProfile([FromBody] CreateMentorProfileDto request)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(ApiResponse<MentorDto>.ErrorResponse("User not authenticated"));

            var result = await _mentorService.CreateOrUpdateMentorProfileAsync(userId, request);
            return Ok(ApiResponse<MentorDto>.SuccessResponse(result, "Mentor profile saved"));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<MentorDto>.ErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<MentorDto>.ErrorResponse(ex.Message));
        }
    }

    [HttpGet("my-profile")]
    public async Task<ActionResult<ApiResponse<MentorDto>>> GetMyProfile()
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(ApiResponse<MentorDto>.ErrorResponse("User not authenticated"));

            var result = await _mentorService.GetMentorByUserIdAsync(userId);
            return Ok(ApiResponse<MentorDto>.SuccessResponse(result, "Mentor profile retrieved"));
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(ApiResponse<MentorDto>.ErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<MentorDto>.ErrorResponse(ex.Message));
        }
    }

    [HttpGet("all")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<List<MentorDto>>>> GetAllMentorsAlias()
    {
        try
        {
            var result = await _mentorService.GetAllMentorsAsync();
            return Ok(ApiResponse<List<MentorDto>>.SuccessResponse(result, "Mentors retrieved"));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<List<MentorDto>>.ErrorResponse(ex.Message));
        }
    }

    [HttpGet("users/detailed")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<List<MentorDto>>>> GetMentorsDetailedForUsers()
    {
        try
        {
            var result = await _mentorService.GetAllMentorsDetailedForUsersAsync();
            return Ok(ApiResponse<List<MentorDto>>.SuccessResponse(result, "Detailed mentors retrieved"));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<List<MentorDto>>.ErrorResponse(ex.Message));
        }
    }

    [HttpGet("discover")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<List<MentorDto>>>> DiscoverMentors(
        [FromQuery] string? domain,
        [FromQuery] double? minRating,
        [FromQuery] decimal? maxPrice,
        [FromQuery] int? minYearsOfExperience,
        [FromQuery] bool? freeOnly,
        [FromQuery] bool? availableToday)
    {
        try
        {
            var filter = new MentorFilterDto
            {
                Domain = domain,
                MinRating = minRating,
                MaxPrice = maxPrice,
                MinYearsOfExperience = minYearsOfExperience,
                FreeOnly = freeOnly,
                AvailableToday = availableToday
            };

            var result = await _mentorService.GetMentorsAsync(filter);
            return Ok(ApiResponse<List<MentorDto>>.SuccessResponse(result, "Filtered mentors retrieved"));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<List<MentorDto>>.ErrorResponse(ex.Message));
        }
    }

    [HttpGet("recommended")]
    public async Task<ActionResult<ApiResponse<List<MentorDto>>>> GetRecommendedMentors([FromQuery] string? predictedPath)
    {
        try
        {
            var studentId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(studentId))
                return Unauthorized(ApiResponse<List<MentorDto>>.ErrorResponse("User not authenticated"));

            var result = await _mentorService.GetRecommendedMentorsAsync(studentId, predictedPath);
            return Ok(ApiResponse<List<MentorDto>>.SuccessResponse(result, "Recommended mentors retrieved"));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<List<MentorDto>>.ErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<List<MentorDto>>.ErrorResponse(ex.Message));
        }
    }

    [HttpPut("{mentorId}/approve")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<MentorDto>>> ApproveMentor(string mentorId)
    {
        try
        {
            var result = await _mentorService.ApproveMentorProfileAsync(mentorId);
            return Ok(ApiResponse<MentorDto>.SuccessResponse(result, "Mentor approved"));
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(ApiResponse<MentorDto>.ErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<MentorDto>.ErrorResponse(ex.Message));
        }
    }

    /// <summary>
    /// Get all available mentors
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<List<MentorDto>>>> GetAllMentors()
    {
        try
        {
            var result = await _mentorService.GetAllMentorsAsync();
            return Ok(ApiResponse<List<MentorDto>>.SuccessResponse(result, "Mentors retrieved"));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<List<MentorDto>>.ErrorResponse(ex.Message));
        }
    }

    /// <summary>
    /// Get mentor by ID
    /// </summary>
    [HttpGet("{id}")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<MentorDto>>> GetMentorById(string id)
    {
        try
        {
            var result = await _mentorService.GetMentorByIdAsync(id);
            return Ok(ApiResponse<MentorDto>.SuccessResponse(result, "Mentor retrieved"));
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(ApiResponse<MentorDto>.ErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<MentorDto>.ErrorResponse(ex.Message));
        }
    }

    /// <summary>
    /// Request a mentor session
    /// </summary>
    [HttpPost("request")]
    public async Task<ActionResult<ApiResponse<MentorRequestDto>>> CreateMentorRequest([FromBody] CreateMentorRequestDto request)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(ApiResponse<MentorRequestDto>.ErrorResponse("User not authenticated"));

            var result = await _mentorService.CreateMentorRequestAsync(userId, request);
            return Ok(ApiResponse<MentorRequestDto>.SuccessResponse(result, "Mentor request created"));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<MentorRequestDto>.ErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<MentorRequestDto>.ErrorResponse(ex.Message));
        }
    }

    /// <summary>
    /// Get pending mentor requests for current mentor
    /// </summary>
    [HttpGet("requests/pending")]
    public async Task<ActionResult<ApiResponse<List<MentorRequestDto>>>> GetPendingRequests()
    {
        try
        {
            var mentorId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(mentorId))
                return Unauthorized(ApiResponse<List<MentorRequestDto>>.ErrorResponse("User not authenticated"));

            var result = await _mentorService.GetPendingRequestsAsync(mentorId);
            return Ok(ApiResponse<List<MentorRequestDto>>.SuccessResponse(result, "Pending requests retrieved"));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<List<MentorRequestDto>>.ErrorResponse(ex.Message));
        }
    }

    /// <summary>
    /// Accept or reject mentor request
    /// </summary>
    [HttpPatch("requests/{requestId}")]
    public async Task<ActionResult<ApiResponse<MentorRequestDto>>> UpdateRequestStatus(string requestId, [FromBody] UpdateMentorRequestStatusDto request)
    {
        try
        {
            var mentorUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(mentorUserId))
                return Unauthorized(ApiResponse<MentorRequestDto>.ErrorResponse("User not authenticated"));

            var result = await _mentorService.UpdateRequestStatusAsync(mentorUserId, requestId, request);
            return Ok(ApiResponse<MentorRequestDto>.SuccessResponse(result, "Request status updated"));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<MentorRequestDto>.ErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<MentorRequestDto>.ErrorResponse(ex.Message));
        }
    }

    /// <summary>
    /// Apply to become a mentor
    /// </summary>
    [HttpPost("apply")]
    public async Task<ActionResult<ApiResponse<MentorDto>>> ApplyAsMentor([FromBody] ApplyMentorDto request)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(ApiResponse<MentorDto>.ErrorResponse("User not authenticated"));

            var result = await _mentorService.ApplyAsMentorAsync(userId, request);
            return Ok(ApiResponse<MentorDto>.SuccessResponse(result, "Successfully registered as a mentor"));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<MentorDto>.ErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<MentorDto>.ErrorResponse(ex.Message));
        }
    }

    [HttpPost("feedback/add")]
    [Route("/api/feedback/add")]
    public async Task<ActionResult<ApiResponse<SessionFeedbackDto>>> AddFeedback([FromBody] AddFeedbackDto request)
    {
        try
        {
            var studentId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(studentId))
                return Unauthorized(ApiResponse<SessionFeedbackDto>.ErrorResponse("User not authenticated"));

            var result = await _mentorService.AddFeedbackAsync(studentId, request);
            return Ok(ApiResponse<SessionFeedbackDto>.SuccessResponse(result, "Feedback added"));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<SessionFeedbackDto>.ErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<SessionFeedbackDto>.ErrorResponse(ex.Message));
        }
    }

    [HttpGet("feedback/my")]
    [Route("/api/feedback/my")]
    public async Task<ActionResult<ApiResponse<List<SessionFeedbackSummaryDto>>>> GetMyFeedback()
    {
        try
        {
            var studentId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(studentId))
                return Unauthorized(ApiResponse<List<SessionFeedbackSummaryDto>>.ErrorResponse("User not authenticated"));

            var result = await _mentorService.GetMySessionFeedbackAsync(studentId);
            return Ok(ApiResponse<List<SessionFeedbackSummaryDto>>.SuccessResponse(result, "Feedback retrieved"));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<List<SessionFeedbackSummaryDto>>.ErrorResponse(ex.Message));
        }
    }

    [HttpPost("progress/upsert")]
    public async Task<ActionResult<ApiResponse<MentorshipProgressDto>>> UpsertProgress([FromBody] UpsertMentorshipProgressDto request)
    {
        try
        {
            var mentorUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(mentorUserId))
                return Unauthorized(ApiResponse<MentorshipProgressDto>.ErrorResponse("User not authenticated"));

            var result = await _mentorService.UpsertMentorshipProgressAsync(mentorUserId, request);
            return Ok(ApiResponse<MentorshipProgressDto>.SuccessResponse(result, "Mentorship progress updated"));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<MentorshipProgressDto>.ErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<MentorshipProgressDto>.ErrorResponse(ex.Message));
        }
    }

    [HttpGet("notifications")]
    public async Task<ActionResult<ApiResponse<List<NotificationDto>>>> GetMyNotifications()
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(ApiResponse<List<NotificationDto>>.ErrorResponse("User not authenticated"));

            var result = await _mentorService.GetMyNotificationsAsync(userId);
            return Ok(ApiResponse<List<NotificationDto>>.SuccessResponse(result, "Notifications retrieved"));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<List<NotificationDto>>.ErrorResponse(ex.Message));
        }
    }

    [HttpPost("notifications/{notificationId}/read")]
    public async Task<ActionResult<ApiResponse<bool>>> MarkNotificationAsRead(string notificationId)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(ApiResponse<bool>.ErrorResponse("User not authenticated"));

            await _mentorService.MarkNotificationAsReadAsync(userId, notificationId);
            return Ok(ApiResponse<bool>.SuccessResponse(true, "Notification marked as read"));
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(ApiResponse<bool>.ErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<bool>.ErrorResponse(ex.Message));
        }
    }

    [HttpPost("notifications/read-all")]
    public async Task<ActionResult<ApiResponse<bool>>> MarkAllNotificationsAsRead()
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(ApiResponse<bool>.ErrorResponse("User not authenticated"));

            await _mentorService.MarkAllNotificationsAsReadAsync(userId);
            return Ok(ApiResponse<bool>.SuccessResponse(true, "All notifications marked as read"));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<bool>.ErrorResponse(ex.Message));
        }
    }

    /// <summary>
    /// Enroll a student with a mentor
    /// </summary>
    [HttpPost("{mentorId}/enroll")]
    public async Task<ActionResult<ApiResponse<MentorshipProgressDto>>> EnrollWithMentor(string mentorId)
    {
        try
        {
            var studentId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(studentId))
                return Unauthorized(ApiResponse<MentorshipProgressDto>.ErrorResponse("User not authenticated"));

            var result = await _mentorService.EnrollStudentWithMentorAsync(studentId, mentorId);
            return Ok(ApiResponse<MentorshipProgressDto>.SuccessResponse(result, "Successfully enrolled with mentor"));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<MentorshipProgressDto>.ErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<MentorshipProgressDto>.ErrorResponse(ex.Message));
        }
    }

    /// <summary>
    /// Get student's enrolled mentors
    /// </summary>
    [HttpGet("student/enrolled")]
    public async Task<ActionResult<ApiResponse<List<MentorDto>>>> GetEnrolledMentorsForStudent()
    {
        try
        {
            var studentId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(studentId))
                return Unauthorized(ApiResponse<List<MentorDto>>.ErrorResponse("User not authenticated"));

            var result = await _mentorService.GetEnrolledMentorsAsync(studentId);
            return Ok(ApiResponse<List<MentorDto>>.SuccessResponse(result, "Enrolled mentors retrieved"));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<List<MentorDto>>.ErrorResponse(ex.Message));
        }
    }

    /// <summary>
    /// Check if student is enrolled with a mentor
    /// </summary>
    [HttpGet("{mentorId}/is-enrolled")]
    public async Task<ActionResult<ApiResponse<MentorshipProgressDto?>>> IsEnrolledWithMentor(string mentorId)
    {
        try
        {
            var studentId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(studentId))
                return Unauthorized(ApiResponse<MentorshipProgressDto?>.ErrorResponse("User not authenticated"));

            var result = await _mentorService.IsStudentEnrolledAsync(studentId, mentorId);
            return Ok(ApiResponse<MentorshipProgressDto?>.SuccessResponse(result, "Enrollment status retrieved"));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<MentorshipProgressDto?>.ErrorResponse(ex.Message));
        }
    }

    /// <summary>
    /// Get mentor dashboard data
    /// </summary>
    [HttpGet("dashboard")]
    public async Task<ActionResult<ApiResponse<MentorDashboardDto>>> GetMentorDashboard()
    {
        try
        {
            var mentorUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(mentorUserId))
                return Unauthorized(ApiResponse<MentorDashboardDto>.ErrorResponse("User not authenticated"));

            var result = await _mentorService.GetMentorDashboardAsync(mentorUserId);
            return Ok(ApiResponse<MentorDashboardDto>.SuccessResponse(result, "Dashboard data retrieved"));
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(ApiResponse<MentorDashboardDto>.ErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<MentorDashboardDto>.ErrorResponse(ex.Message));
        }
    }

    /// <summary>
    /// Approve or reject enrollment request
    /// </summary>
    [HttpPost("enrollments/{enrollmentId}/approve")]
    public async Task<ActionResult<ApiResponse<EnrollmentApprovalDto>>> ApproveEnrollment(
        string enrollmentId,
        [FromBody] ApproveEnrollmentDto request)
    {
        try
        {
            var mentorUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(mentorUserId))
                return Unauthorized(ApiResponse<EnrollmentApprovalDto>.ErrorResponse("User not authenticated"));

            request.EnrollmentId = enrollmentId;
            var result = await _mentorService.ApproveEnrollmentAsync(mentorUserId, request);
            return Ok(ApiResponse<EnrollmentApprovalDto>.SuccessResponse(result, "Enrollment approved/rejected"));
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(ApiResponse<EnrollmentApprovalDto>.ErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<EnrollmentApprovalDto>.ErrorResponse(ex.Message));
        }
    }

    /// <summary>
    /// Process payment for enrollment
    /// </summary>
    [HttpPost("enrollments/{enrollmentId}/pay")]
    public async Task<ActionResult<ApiResponse<PaymentResponseDto>>> ProcessPayment(
        string enrollmentId,
        [FromBody] PaymentRequestDto request)
    {
        try
        {
            var studentId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(studentId))
                return Unauthorized(ApiResponse<PaymentResponseDto>.ErrorResponse("User not authenticated"));

            request.EnrollmentId = enrollmentId;
            var result = await _mentorService.ProcessPaymentAsync(studentId, request);
            return Ok(ApiResponse<PaymentResponseDto>.SuccessResponse(result, "Payment processed successfully"));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<PaymentResponseDto>.ErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<PaymentResponseDto>.ErrorResponse(ex.Message));
        }
    }

    /// <summary>
    /// Get student wallet balance
    /// </summary>
    [HttpGet("student-wallet")]
    public async Task<ActionResult<ApiResponse<StudentWalletDto>>> GetStudentWallet()
    {
        try
        {
            var studentId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(studentId))
                return Unauthorized(ApiResponse<StudentWalletDto>.ErrorResponse("User not authenticated"));

            var result = await _mentorService.GetStudentWalletAsync(studentId);
            return Ok(ApiResponse<StudentWalletDto>.SuccessResponse(result, "Wallet balance retrieved"));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<StudentWalletDto>.ErrorResponse(ex.Message));
        }
    }
}
