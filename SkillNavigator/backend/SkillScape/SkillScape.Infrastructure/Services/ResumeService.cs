using Microsoft.EntityFrameworkCore;
using SkillScape.Application.DTOs;
using SkillScape.Application.Interfaces;
using SkillScape.Infrastructure.Data;

namespace SkillScape.Infrastructure.Services;

public class ResumeService : IResumeService
{
    private readonly ApplicationDbContext _context;

    public ResumeService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ResumePreviewDto> GetResumePreviewAsync(string userId)
    {
        var user = await _context.Users
            .Include(u => u.UserSkills)
                .ThenInclude(us => us.Skill)
                    .ThenInclude(s => s.CareerDomain)
            .Include(u => u.UserBadges)
                .ThenInclude(ub => ub.Badge)
            .FirstOrDefaultAsync(u => u.Id == userId)
            ?? throw new InvalidOperationException("User not found");

        // Get completed skills
        var completedSkills = user.UserSkills
            .Where(us => us.IsCompleted)
            .ToList();

        // Group skills by domain
        var domainSkills = completedSkills
            .GroupBy(us => us.Skill.CareerDomain)
            .Select(g => new DomainSkillsDto
            {
                DomainName = g.Key?.Name ?? "General",
                CompletedSkills = g.Select(us => us.Skill.Name).ToList(),
                TotalSkillsCompleted = g.Count()
            })
            .ToList();

        // Get all unique skill names
        var allSkills = completedSkills
            .Select(us => us.Skill.Name)
            .Distinct()
            .OrderBy(s => s)
            .ToList();

        // Get earned badges (certifications)
        var certifications = user.UserBadges
            .Select(ub => new BadgeDto
            {
                Id = ub.BadgeId,
                Name = ub.Badge.Name,
                Description = ub.Badge.Description,
                IconUrl = ub.Badge.IconUrl,
                Rarity = ub.Badge.Rarity,
                Earned = true,
                EarnedAt = ub.EarnedAt
            })
            .ToList();

        // Generate a summary
        var summary = GenerateSummary(user, completedSkills.Count, domainSkills.Count);

        return new ResumePreviewDto
        {
            PersonalInfo = new PersonalInfoDto
            {
                Name = user.FullName,
                Email = user.Email,
                Bio = user.Bio ?? string.Empty,
                Level = user.Level,
                TotalXP = user.TotalXP
            },
            Summary = summary,
            Skills = allSkills,
            DomainSkills = domainSkills,
            Certifications = certifications
        };
    }

    private string GenerateSummary(Domain.Entities.ApplicationUser user, int completedSkills, int domains)
    {
        var level = user.Level;
        var xp = user.TotalXP;

        return $"Motivated learner at Level {level} with {xp} XP earned through SkillScape. " +
               $"Completed {completedSkills} skills across {domains} domains. " +
               $"Passionate about continuous learning and skill development in technology.";
    }
}
