using SkillScape.Application.DTOs;

namespace SkillScape.Application.Interfaces;

/// <summary>
/// Mentor service interface
/// </summary>
public interface IMentorService
{
    Task<MentorDto> CreateOrUpdateMentorProfileAsync(string userId, CreateMentorProfileDto request);
    Task<MentorDto> ApproveMentorProfileAsync(string mentorId);
    Task<List<MentorDto>> GetAllMentorsAsync();
    Task<List<MentorDto>> GetAllMentorsDetailedForUsersAsync();
    Task<List<MentorDto>> GetMentorsAsync(MentorFilterDto filter);
    Task<List<MentorDto>> GetRecommendedMentorsAsync(string studentId, string? predictedPath);
    Task<MentorDto> GetMentorByUserIdAsync(string userId);
    Task<MentorDto> GetMentorByIdAsync(string mentorId);
    Task<MentorRequestDto> CreateMentorRequestAsync(string studentId, CreateMentorRequestDto request);
    Task<List<MentorRequestDto>> GetPendingRequestsAsync(string mentorUserId);
    Task<MentorRequestDto> UpdateRequestStatusAsync(string mentorUserId, string requestId, UpdateMentorRequestStatusDto request);
    Task<MentorDto> ApplyAsMentorAsync(string userId, ApplyMentorDto request);
    Task<MentorSessionDto> BookSessionAsync(string studentId, BookSessionDto request);
    Task<MentorSessionDto> CreateSessionForStudentAsync(string mentorUserId, CreateMentorSessionDto request);
    Task<MentorSessionDto> ApproveSessionAsync(string mentorUserId, ApproveSessionDto request);
    Task<MentorSessionDto> UpdateSessionAsync(string mentorUserId, string sessionId, UpdateSessionDto request);
    Task<bool> DeleteSessionAsync(string mentorUserId, string sessionId);
    Task<MentorSessionDto> GetSessionByIdAsync(string sessionId);
    Task<List<MentorSessionDto>> GetMySessionsAsync(string userId);
    Task<List<MentorSessionDto>> GetStudentUpcomingSessionsAsync(string studentId);
    Task<SessionFeedbackDto> AddFeedbackAsync(string studentId, AddFeedbackDto request);
    Task<List<SessionFeedbackSummaryDto>> GetMySessionFeedbackAsync(string studentId);
    Task<MentorshipProgressDto> UpsertMentorshipProgressAsync(string mentorUserId, UpsertMentorshipProgressDto request);
    Task<List<NotificationDto>> GetMyNotificationsAsync(string userId);
    Task MarkNotificationAsReadAsync(string userId, string notificationId);
    Task MarkAllNotificationsAsReadAsync(string userId);
    Task<MentorshipProgressDto> EnrollStudentWithMentorAsync(string studentId, string mentorId);
    Task<List<MentorDto>> GetEnrolledMentorsAsync(string studentId);
    Task<MentorshipProgressDto?> IsStudentEnrolledAsync(string studentId, string mentorId);
    Task<MentorDashboardDto> GetMentorDashboardAsync(string mentorId);
    Task<EnrollmentApprovalDto> ApproveEnrollmentAsync(string mentorId, ApproveEnrollmentDto request);
    Task<PaymentResponseDto> ProcessPaymentAsync(string studentId, PaymentRequestDto request);
    Task<StudentWalletDto> GetStudentWalletAsync(string studentId);
}
