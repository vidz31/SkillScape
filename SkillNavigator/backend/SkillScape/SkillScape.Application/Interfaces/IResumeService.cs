using SkillScape.Application.DTOs;

namespace SkillScape.Application.Interfaces;

/// <summary>
/// Resume service interface
/// </summary>
public interface IResumeService
{
    Task<ResumePreviewDto> GetResumePreviewAsync(string userId);
}
