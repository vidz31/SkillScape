using SkillScape.Application.DTOs;

namespace SkillScape.Application.Interfaces;

public interface IAdminService
{
    Task<AdminDashboardStatsDto> GetDashboardAsync();
    Task<List<AdminUserDto>> GetUsersAsync(string? role, bool? isBlocked);
    Task BlockOrUnblockUserAsync(string adminId, BlockUserDto request);
    Task UpdateUserRoleAsync(string adminId, UpdateUserRoleDto request);
    Task<List<MentorDto>> GetPendingMentorsAsync();
    Task<MentorDto> ApproveMentorAsync(string adminId, string mentorId);
    Task<MentorDto> RejectMentorAsync(string adminId, string mentorId, string reason);
    Task<List<AdminSessionDto>> GetSessionsAsync(string? status);
    Task<PlatformAnalyticsDto> GetAnalyticsAsync();
    Task BroadcastAnnouncementAsync(string adminId, string message);
    Task<List<SessionComplaintDto>> GetComplaintsAsync(string? status);
    Task<SessionComplaintDto> ResolveComplaintAsync(string adminId, string complaintId, string resolutionNote);
    Task<List<AdminQuizQuestionCrudDto>> GetQuizQuestionsAsync();
    Task<AdminQuizQuestionCrudDto> CreateQuizQuestionAsync(string adminId, UpsertAdminQuizQuestionDto request);
    Task<AdminQuizQuestionCrudDto> UpdateQuizQuestionAsync(string adminId, string questionId, UpsertAdminQuizQuestionDto request);
    Task DeleteQuizQuestionAsync(string adminId, string questionId);
    Task<List<AdminRoadmapModuleCrudDto>> GetRoadmapModulesAsync(string? domainId);
    Task<AdminRoadmapModuleCrudDto> CreateRoadmapModuleAsync(string adminId, UpsertAdminRoadmapModuleDto request);
    Task<AdminRoadmapModuleCrudDto> UpdateRoadmapModuleAsync(string adminId, string moduleId, UpsertAdminRoadmapModuleDto request);
    Task DeleteRoadmapModuleAsync(string adminId, string moduleId);
    Task<List<AdminMentorCrudDto>> GetAllMentorsAsync();
    Task<AdminMentorCrudDto> CreateMentorAsync(string adminId, CreateAdminMentorDto request);
    Task BlockOrUnblockMentorAsync(string adminId, string mentorId, AdminBlockMentorDto request);
    Task DeleteMentorAsync(string adminId, string mentorId);
    Task<List<AdminResumeSummaryDto>> GetResumeSummariesAsync();
    Task<ResumePreviewDto> GetResumeByUserIdAsync(string userId);
    Task<List<AdminUserProgressSnapshotDto>> GetUserProgressSnapshotAsync();
}
