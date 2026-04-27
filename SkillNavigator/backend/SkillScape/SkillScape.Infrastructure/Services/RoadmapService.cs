using System.Threading.Tasks;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using SkillScape.Application.DTOs;
using SkillScape.Application.Interfaces;
using SkillScape.Infrastructure.Data;
using SkillScape.Domain.Entities;

namespace SkillScape.Infrastructure.Services;

public class RoadmapService : IRoadmapService
{
    private readonly ApplicationDbContext _context;

    public RoadmapService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<RoadmapDto> GetMyRoadmapAsync(string userId)
    {
        var latestResult = await _context.QuizResults
            .Where(r => r.UserId == userId)
            .OrderByDescending(r => r.CompletedAt)
            .FirstOrDefaultAsync();

        if (latestResult == null)
            throw new InvalidOperationException("No career interest predicted yet. Please take the quiz first.");

        return await BuildRoadmapAsync(userId, latestResult.RecommendedDomainId);
    }

    public async Task<List<RoadmapOptionDto>> GetRoadmapOptionsAsync(string userId)
    {
        var latestResult = await _context.QuizResults
            .Where(r => r.UserId == userId)
            .OrderByDescending(r => r.CompletedAt)
            .FirstOrDefaultAsync();

        var recommendedDomainId = latestResult?.RecommendedDomainId;

        var domains = await _context.CareerDomains
            .Where(d => d.IsActive)
            .OrderBy(d => d.Name)
            .Select(d => new { d.Id, d.Name })
            .ToListAsync();

        var userProgressByDomain = await _context.UserProgressions
            .Where(p => p.UserId == userId)
            .ToDictionaryAsync(p => p.CareerDomainId, p => p.ProgressPercentage);

        return domains
            .Select(d => new RoadmapOptionDto
            {
                DomainId = d.Id,
                DomainName = d.Name,
                ProgressPercentage = userProgressByDomain.TryGetValue(d.Id, out var progress) ? progress : 0,
                IsRecommended = !string.IsNullOrEmpty(recommendedDomainId) && d.Id == recommendedDomainId
            })
            .ToList();
    }

    public async Task<RoadmapDto> GetRoadmapByDomainAsync(string userId, string domainId)
    {
        if (string.IsNullOrWhiteSpace(domainId))
            throw new InvalidOperationException("Domain is required.");

        return await BuildRoadmapAsync(userId, domainId);
    }

    private async Task<RoadmapDto> BuildRoadmapAsync(string userId, string domainId)
    {
        var domain = await _context.CareerDomains
            .Where(d => d.Id == domainId && d.IsActive)
            .FirstOrDefaultAsync();

        if (domain == null)
            throw new InvalidOperationException("Domain not found.");

        var currentProgress = await _context.UserProgressions
            .FirstOrDefaultAsync(p => p.UserId == userId && p.CareerDomainId == domainId);

        var progressPercentage = currentProgress?.ProgressPercentage ?? 0;

        var skills = await _context.Skills
            .Where(s => s.CareerDomainId == domainId && s.IsActive)
            .OrderBy(s => s.DisplayOrder)
            .ToListAsync();

        var userSkills = await _context.UserSkills
            .Where(us => us.UserId == userId)
            .ToListAsync();

        var modules = await _context.RoadmapSteps
            .Include(r => r.Topics)
            .Where(r => r.CareerDomainId == domainId && r.IsActive)
            .OrderBy(r => r.StepNumber)
            .ToListAsync();

        var progressionsList = await _context.UserModuleProgressions
            .Where(ump => ump.UserId == userId && ump.IsCompleted)
            .Select(ump => ump.ModuleId)
            .ToListAsync();

        var moduleProgressions = progressionsList.ToHashSet();

        var roadmapDto = new RoadmapDto
        {
            DomainId = domainId,
            DomainName = domain.Name,
            ProgressPercentage = progressPercentage,
            Skills = new List<SkillRoadmapDto>()
        };

        foreach (var skill in skills)
        {
            var skillUserProgress = userSkills.FirstOrDefault(us => us.SkillId == skill.Id);

            var skillDto = new SkillRoadmapDto
            {
                SkillId = skill.Id,
                SkillName = skill.Name,
                SkillDescription = skill.Description,
                IsCompleted = skillUserProgress?.IsCompleted ?? false,
                Modules = new List<RoadmapModuleDto>()
            };

            var skillModules = modules.Where(m => m.SkillId == skill.Id).ToList();

            foreach (var mod in skillModules)
            {
                var modDto = new RoadmapModuleDto
                {
                    ModuleId = mod.Id,
                    Title = mod.Title,
                    Description = mod.Description,
                    StepNumber = mod.StepNumber,
                    EstimatedHours = mod.EstimatedHours,
                    IsCompleted = moduleProgressions.Contains(mod.Id),
                    Topics = mod.Topics.OrderBy(t => t.DisplayOrder).Select(t => new RoadmapTopicDto
                    {
                        TopicId = t.Id,
                        Title = t.Title,
                        ResourceUrl = t.ResourceUrl
                    }).ToList()
                };
                skillDto.Modules.Add(modDto);
            }

            roadmapDto.Skills.Add(skillDto);
        }

        return roadmapDto;
    }

    public async Task<bool> MarkStepCompleteAsync(string userId, string stepId)
    {
        // stepId here refers to the ModuleId (RoadmapStep.Id) from the frontend
        var module = await _context.RoadmapSteps.FindAsync(stepId);
        if (module == null) throw new InvalidOperationException("Module not found");

        var domainId = module.CareerDomainId;
        var skillId = module.SkillId;

        // 1. Mark UserModuleProgress as Complete
        var moduleProgress = await _context.UserModuleProgressions
            .FirstOrDefaultAsync(ump => ump.UserId == userId && ump.ModuleId == stepId);

        if (moduleProgress == null)
        {
            moduleProgress = new UserModuleProgress
            {
                UserId = userId,
                ModuleId = stepId,
                IsCompleted = true,
                CompletedAt = DateTime.UtcNow
            };
            _context.UserModuleProgressions.Add(moduleProgress);
        }
        else if (!moduleProgress.IsCompleted)
        {
            moduleProgress.IsCompleted = true;
            moduleProgress.CompletedAt = DateTime.UtcNow;
        }
        else
        {
            // Already completed
            return true;
        }
        
        // Save initial progression to evaluate skill state next
        await _context.SaveChangesAsync();

        // 2. Check if all Modules for this Skill are now completed
        var allModulesForSkill = await _context.RoadmapSteps
            .Where(r => r.SkillId == skillId && r.IsActive)
            .Select(r => r.Id)
            .ToListAsync();
            
        var completedModulesForSkill = await _context.UserModuleProgressions
            .Where(ump => ump.UserId == userId && allModulesForSkill.Contains(ump.ModuleId) && ump.IsCompleted)
            .Select(ump => ump.ModuleId)
            .ToListAsync();
            
        bool isSkillFullyCompleted = (allModulesForSkill.Count > 0 && completedModulesForSkill.Count == allModulesForSkill.Count);

        // --- GLOBAL XP, LEVEL, AND BADGE UPDATES ---
        var user = await _context.Users
            .Include(u => u.UserSkills)
            .FirstOrDefaultAsync(u => u.Id == userId);
            
        if (user != null)
        {
            // Award global XP based on estimated hours of the module (e.g. 1 hour = 10 XP)
            int xpReward = module.EstimatedHours * 10;
            user.TotalXP += xpReward;
            
            // Check for Level up (100 XP per level)
            int newLevel = (int)(user.TotalXP / 100) + 1;
            if (newLevel > user.Level)
            {
                user.Level = newLevel;
            }
            
            user.UpdatedAt = DateTime.UtcNow;
            
            // Check for badge unlocks
            var completedSkillsCount = user.UserSkills.Count(us => us.IsCompleted);
            // Include this skill if it just completed in this transaction but isn't saved yet
            if (isSkillFullyCompleted && !user.UserSkills.Any(us => us.SkillId == skillId && us.IsCompleted)) 
            {
                completedSkillsCount++;
            }

            var unlockedBadges = await _context.Badges
                .Where(b => b.IsActive && (b.XPRequired <= user.TotalXP || b.SkillsCompletedRequired <= completedSkillsCount))
                .ToListAsync();

            foreach (var badge in unlockedBadges)
            {
                var userBadge = await _context.UserBadges
                    .FirstOrDefaultAsync(ub => ub.UserId == userId && ub.BadgeId == badge.Id);

                if (userBadge == null)
                {
                    _context.UserBadges.Add(new UserBadge
                    {
                        Id = Guid.NewGuid().ToString(),
                        UserId = userId,
                        BadgeId = badge.Id,
                        EarnedAt = DateTime.UtcNow
                    });
                }
            }
        }
        // -------------------------------------------


        // 3. Update UserSkill appropriately
        var userSkill = await _context.UserSkills
            .FirstOrDefaultAsync(us => us.UserId == userId && us.SkillId == skillId);

        if (userSkill == null)
        {
            userSkill = new UserSkill
            {
                UserId = userId,
                SkillId = skillId,
                IsCompleted = isSkillFullyCompleted,
                IsCleared = isSkillFullyCompleted,
                CompletedAt = isSkillFullyCompleted ? DateTime.UtcNow : null,
                ProgressPercentage = allModulesForSkill.Count > 0 ? (int)(((double)completedModulesForSkill.Count / allModulesForSkill.Count) * 100) : 100
            };
            _context.UserSkills.Add(userSkill);
        }
        else
        {
            userSkill.ProgressPercentage = allModulesForSkill.Count > 0 ? (int)(((double)completedModulesForSkill.Count / allModulesForSkill.Count) * 100) : 100;
            if (isSkillFullyCompleted && !userSkill.IsCompleted)
            {
                userSkill.IsCompleted = true;
                userSkill.IsCleared = true; // Auto-clear from active view 
                userSkill.CompletedAt = DateTime.UtcNow;
            }
        }

        // 4. Update the aggregate UserProgress for the entire Domain
        var userProgress = await _context.UserProgressions
            .FirstOrDefaultAsync(up => up.UserId == userId && up.CareerDomainId == domainId);

        // Calculate total domain steps vs completed domain steps
        int totalDomainModules = await _context.RoadmapSteps
            .CountAsync(r => r.CareerDomainId == domainId && r.IsActive);
            
        var allDomainModuleIds = await _context.RoadmapSteps
            .Where(r => r.CareerDomainId == domainId && r.IsActive)
            .Select(r => r.Id)
            .ToListAsync();
            
        int completedDomainModules = await _context.UserModuleProgressions
            .CountAsync(ump => ump.UserId == userId && allDomainModuleIds.Contains(ump.ModuleId) && ump.IsCompleted);

        if (userProgress == null)
        {
            userProgress = new UserProgress
            {
                UserId = userId,
                CareerDomainId = domainId,
                SkillsCompleted = isSkillFullyCompleted ? 1 : 0, 
                TotalSkills = totalDomainModules, // Re-purposing TotalSkills to track modules for now to feed the Dashboard nicely
                XPInDomain = module.EstimatedHours * 10,
                ProgressPercentage = totalDomainModules > 0 ? ((double)completedDomainModules / totalDomainModules) * 100 : 100
            };
            _context.UserProgressions.Add(userProgress);
        }
        else
        {
            // If the skill just completed, bump the stats
            if (isSkillFullyCompleted && !userSkill.IsCompleted) 
            {
                userProgress.SkillsCompleted += 1;
            }
            userProgress.TotalSkills = totalDomainModules;
            userProgress.XPInDomain += module.EstimatedHours * 10;
            userProgress.ProgressPercentage = totalDomainModules > 0 ? ((double)completedDomainModules / totalDomainModules) * 100 : 100;
            
            userProgress.UpdatedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();
        
        return true;
    }
}
