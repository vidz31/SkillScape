using SkillScape.Application.DTOs;

namespace SkillScape.Application.Interfaces;

/// <summary>
/// Progress and XP service interface
/// </summary>
public interface IProgressService
{
    Task<UserStatsDto> GetUserStatsAsync(string userId);
    Task CompleteSkillAsync(string userId, string skillId);
    Task<List<BadgeDto>> GetUserBadgesAsync(string userId);
    Task<List<LeaderboardUserDto>> GetLeaderboardAsync(string currentUserId, int limit = 10);
}
