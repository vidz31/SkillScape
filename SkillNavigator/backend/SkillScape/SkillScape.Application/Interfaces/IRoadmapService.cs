using System.Threading.Tasks;
using SkillScape.Application.DTOs;

namespace SkillScape.Application.Interfaces;

public interface IRoadmapService
{
    /// <summary>
    /// Gets the roadmap specifically tailored to the user's ML-predicted Career Domain.
    /// Includes completion status for each step based on UserSkill progress.
    /// </summary>
    Task<RoadmapDto> GetMyRoadmapAsync(string userId);

    /// <summary>
    /// Gets available roadmap domains for the user to switch between.
    /// </summary>
    Task<List<RoadmapOptionDto>> GetRoadmapOptionsAsync(string userId);

    /// <summary>
    /// Gets roadmap for a specific selected career domain.
    /// </summary>
    Task<RoadmapDto> GetRoadmapByDomainAsync(string userId, string domainId);

    /// <summary>
    /// Marks a specific roadmap step (Skill) as Complete, updating UserSkill and UserProgress.
    /// </summary>
    Task<bool> MarkStepCompleteAsync(string userId, string stepId);
}
