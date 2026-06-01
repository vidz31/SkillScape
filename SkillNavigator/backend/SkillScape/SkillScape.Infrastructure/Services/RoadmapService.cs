using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Text.Json;
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
        try
        {
            // 1. Check if user has an accepted custom career profile
            var profile = await _context.UserCareerProfiles
                .FirstOrDefaultAsync(p => p.UserId == userId);

            if (profile != null && !string.IsNullOrEmpty(profile.RecommendedCareerId))
            {
                var path = await _context.CareerPaths.FindAsync(profile.RecommendedCareerId);
                if (path != null)
                {
                    var customRoadmap = await BuildCustomRoadmapAsync(userId, profile.RoadmapJson, path);
                    if (customRoadmap != null) return customRoadmap;
                }
            }

            // Fallback: legacy domain quiz results
            var latestResult = await _context.QuizResults
                .Where(r => r.UserId == userId)
                .OrderByDescending(r => r.CompletedAt)
                .FirstOrDefaultAsync();

            if (latestResult == null || string.IsNullOrEmpty(latestResult.RecommendedDomainId))
            {
                // No quiz taken yet - return null instead of throwing
                return null;
            }

            // Check if the recommended domain exists and is active
            var domain = await _context.CareerDomains
                .FirstOrDefaultAsync(d => d.Id == latestResult.RecommendedDomainId && d.IsActive);
            
            if (domain == null)
            {
                // Domain doesn't exist or is inactive - return null
                return null;
            }

            return await BuildRoadmapAsync(userId, latestResult.RecommendedDomainId);
        }
        catch (Exception ex)
        {
            // Log and return null on any error
            System.Console.WriteLine($"Error in GetMyRoadmapAsync: {ex.Message}");
            return null;
        }
    }

    public async Task<List<RoadmapOptionDto>> GetRoadmapOptionsAsync(string userId)
    {
        var options = new List<RoadmapOptionDto>();

        // 1. If user has accepted a career path, prepend it as the recommended option!
        var profile = await _context.UserCareerProfiles
            .FirstOrDefaultAsync(p => p.UserId == userId);

        string? activeCareerId = null;
        if (profile != null && !string.IsNullOrEmpty(profile.RecommendedCareerId))
        {
            var path = await _context.CareerPaths.FindAsync(profile.RecommendedCareerId);
            if (path != null)
            {
                activeCareerId = path.Id;
                var progress = await _context.UserProgressions
                    .FirstOrDefaultAsync(up => up.UserId == userId && up.CareerDomainId == path.Id);
                
                options.Add(new RoadmapOptionDto
                {
                    DomainId = path.Id,
                    DomainName = path.Title,
                    ProgressPercentage = progress?.ProgressPercentage ?? 0,
                    IsRecommended = true
                });
            }
        }

        // 2. Fetch legacy recommended domain from quiz results (fallback recommendation if no accepted path)
        var latestResult = await _context.QuizResults
            .Where(r => r.UserId == userId)
            .OrderByDescending(r => r.CompletedAt)
            .FirstOrDefaultAsync();

        var recommendedDomainId = latestResult?.RecommendedDomainId;

        // If there's an active custom career path, we don't mark the legacy domain as recommended
        if (!string.IsNullOrEmpty(activeCareerId))
        {
            recommendedDomainId = null; 
        }

        // 3. Load active legacy domains
        var domains = await _context.CareerDomains
            .Where(d => d.IsActive)
            .OrderBy(d => d.Name)
            .Select(d => new { d.Id, d.Name })
            .ToListAsync();

        var userProgressByDomain = await _context.UserProgressions
            .Where(p => p.UserId == userId)
            .ToDictionaryAsync(p => p.CareerDomainId, p => p.ProgressPercentage);

        foreach (var d in domains)
        {
            if (options.Any(o => o.DomainId == d.Id)) continue;

            options.Add(new RoadmapOptionDto
            {
                DomainId = d.Id,
                DomainName = d.Name,
                ProgressPercentage = userProgressByDomain.TryGetValue(d.Id, out var progress) ? progress : 0,
                IsRecommended = !string.IsNullOrEmpty(recommendedDomainId) && d.Id == recommendedDomainId
            });
        }

        return options;
    }

    public async Task<RoadmapDto> GetRoadmapByDomainAsync(string userId, string domainId)
    {
        if (string.IsNullOrWhiteSpace(domainId))
            throw new InvalidOperationException("Domain is required.");

        // 1. Check if domainId is a customized CareerPath
        var path = await _context.CareerPaths.FindAsync(domainId);
        if (path != null)
        {
            var profile = await _context.UserCareerProfiles
                .FirstOrDefaultAsync(p => p.UserId == userId && p.RecommendedCareerId == domainId);
            
            string json = profile != null ? profile.RoadmapJson : path.RoadmapJson;
            var customRoadmap = await BuildCustomRoadmapAsync(userId, json, path);
            if (customRoadmap != null) return customRoadmap;
        }

        // Fallback to legacy Domain
        return await BuildRoadmapAsync(userId, domainId);
    }

    private async Task<RoadmapDto?> BuildCustomRoadmapAsync(string userId, string roadmapJson, CareerPath path)
    {
        if (string.IsNullOrEmpty(roadmapJson) || roadmapJson == "[]")
        {
            roadmapJson = path.RoadmapJson;
        }

        List<RoadmapMilestoneDto> milestones;
        try
        {
            milestones = JsonSerializer.Deserialize<List<RoadmapMilestoneDto>>(roadmapJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                          ?? new List<RoadmapMilestoneDto>();
        }
        catch
        {
            milestones = new List<RoadmapMilestoneDto>();
        }

        var completedModuleIds = await _context.UserModuleProgressions
            .Where(ump => ump.UserId == userId && ump.IsCompleted)
            .Select(ump => ump.ModuleId)
            .ToListAsync();
        var completedCustomModuleIds = await _context.UserCustomModuleProgressions
            .Where(ump => ump.UserId == userId && ump.IsCompleted)
            .Select(ump => ump.ModuleId)
            .ToListAsync();
        var completedSet = completedModuleIds
            .Concat(completedCustomModuleIds)
            .ToHashSet();

        var roadmapDto = new RoadmapDto
        {
            DomainId = path.Id,
            DomainName = path.Title,
            ProgressPercentage = 0,
            Skills = new List<SkillRoadmapDto>()
        };

        int totalModules = 0;
        int completedModules = 0;

        for (int pIdx = 0; pIdx < milestones.Count; pIdx++)
        {
            var m = milestones[pIdx];
            var skillDto = new SkillRoadmapDto
            {
                SkillId = $"{path.Id}_phase_{pIdx}",
                SkillName = m.Phase,
                SkillDescription = m.Duration,
                IsCompleted = false,
                Modules = new List<RoadmapModuleDto>()
            };

            int skillTotal = 0;
            int skillCompleted = 0;

            for (int tIdx = 0; tIdx < m.Topics.Count; tIdx++)
            {
                var topic = m.Topics[tIdx];
                var modId = $"{path.Id}_phase_{pIdx}_topic_{tIdx}";
                bool isCompleted = completedSet.Contains(modId);

                skillTotal++;
                totalModules++;
                if (isCompleted)
                {
                    skillCompleted++;
                    completedModules++;
                }

                skillDto.Modules.Add(new RoadmapModuleDto
                {
                    ModuleId = modId,
                    Title = topic,
                    Description = $"Study details and master concepts for {topic}.",
                    StepNumber = tIdx + 1,
                    EstimatedHours = 10,
                    IsCompleted = isCompleted,
                    Topics = new List<RoadmapTopicDto>
                    {
                        new RoadmapTopicDto
                        {
                            TopicId = $"{modId}_t0",
                            Title = $"Introduction & Documentation on {topic}",
                            ResourceUrl = "https://www.google.com/search?q=" + Uri.EscapeDataString(topic + " documentation tutorial")
                        }
                    }
                });
            }

            if (!string.IsNullOrEmpty(m.ProjectSuggestion))
            {
                var modId = $"{path.Id}_phase_{pIdx}_project";
                bool isCompleted = completedSet.Contains(modId);

                skillTotal++;
                totalModules++;
                if (isCompleted)
                {
                    skillCompleted++;
                    completedModules++;
                }

                skillDto.Modules.Add(new RoadmapModuleDto
                {
                    ModuleId = modId,
                    Title = $"Project: {m.ProjectSuggestion}",
                    Description = "Apply the concepts learned in this phase to build this practical project milestone.",
                    StepNumber = m.Topics.Count + 1,
                    EstimatedHours = 20,
                    IsCompleted = isCompleted,
                    Topics = new List<RoadmapTopicDto>()
                });
            }

            skillDto.IsCompleted = skillTotal > 0 && skillCompleted == skillTotal;
            roadmapDto.Skills.Add(skillDto);
        }

        roadmapDto.ProgressPercentage = totalModules > 0 ? ((double)completedModules / totalModules) * 100 : 0;
         
        var userProgress = await _context.UserProgressions
            .FirstOrDefaultAsync(up => up.UserId == userId && up.CareerDomainId == path.Id);
        if (userProgress != null)
        {
            userProgress.SkillsCompleted = roadmapDto.Skills.Count(s => s.IsCompleted);
            userProgress.TotalSkills = roadmapDto.Skills.Count;
            userProgress.ProgressPercentage = roadmapDto.ProgressPercentage;
            userProgress.UpdatedAt = DateTime.UtcNow;
            _context.UserProgressions.Update(userProgress);
            await _context.SaveChangesAsync();
        }

        return roadmapDto;
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
        if (stepId.Contains("_phase_"))
        {
            var parts = stepId.Split(new[] { "_phase_" }, StringSplitOptions.None);
            var pathId = parts[0];

            var moduleProgress = await _context.UserCustomModuleProgressions
                .FirstOrDefaultAsync(ump => ump.UserId == userId && ump.ModuleId == stepId);

            if (moduleProgress == null)
            {
                int hours = stepId.EndsWith("_project") ? 20 : 10;
                moduleProgress = new UserCustomModuleProgress
                {
                    UserId = userId,
                    ModuleId = stepId,
                    IsCompleted = true,
                    CompletedAt = DateTime.UtcNow,
                    EstimatedHours = hours
                };
                _context.UserCustomModuleProgressions.Add(moduleProgress);
            }
            else if (!moduleProgress.IsCompleted)
            {
                moduleProgress.IsCompleted = true;
                moduleProgress.CompletedAt = DateTime.UtcNow;
            }
            else
            {
                return true;
            }

            await _context.SaveChangesAsync();

            var user = await _context.Users
                .Include(u => u.UserSkills)
                .FirstOrDefaultAsync(u => u.Id == userId);
            
            int rewardHours = stepId.EndsWith("_project") ? 20 : 10;
            int xpReward = rewardHours * 10;

            if (user != null)
            {
                user.TotalXP += xpReward;
                int newLevel = (int)(user.TotalXP / 100) + 1;
                if (newLevel > user.Level)
                {
                    user.Level = newLevel;
                }
                user.UpdatedAt = DateTime.UtcNow;

                var customCompletedSkills = await _context.UserProgressions
                    .Where(up => up.UserId == userId && up.CareerDomainId != "frontend" && up.CareerDomainId != "backend" && up.CareerDomainId != "fullstack" && up.CareerDomainId != "data" && up.CareerDomainId != "devops" && up.CareerDomainId != "ml" && up.CareerDomainId != "business" && up.CareerDomainId != "design" && up.CareerDomainId != "healthcare" && up.CareerDomainId != "education")
                    .SumAsync(up => up.SkillsCompleted);

                var legacyCompletedSkills = user.UserSkills.Count(us => us.IsCompleted);
                int totalCompletedSkills = legacyCompletedSkills + customCompletedSkills;

                var unlockedBadges = await _context.Badges
                    .Where(b => b.IsActive && (b.XPRequired <= user.TotalXP || b.SkillsCompletedRequired <= totalCompletedSkills))
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

            var path = await _context.CareerPaths.FindAsync(pathId);
            var profile = await _context.UserCareerProfiles
                .FirstOrDefaultAsync(p => p.UserId == userId && p.RecommendedCareerId == pathId);

            if (path != null)
            {
                string json = profile != null ? profile.RoadmapJson : path.RoadmapJson;
                await BuildCustomRoadmapAsync(userId, json, path);
            }

            await _context.SaveChangesAsync();
            return true;
        }

        var module = await _context.RoadmapSteps.FindAsync(stepId);
        if (module == null) throw new InvalidOperationException("Module not found");

        var domainId = module.CareerDomainId;
        var skillId = module.SkillId;

        var legacyModuleProgress = await _context.UserModuleProgressions
            .FirstOrDefaultAsync(ump => ump.UserId == userId && ump.ModuleId == stepId);

        if (legacyModuleProgress == null)
        {
            legacyModuleProgress = new UserModuleProgress
            {
                UserId = userId,
                ModuleId = stepId,
                IsCompleted = true,
                CompletedAt = DateTime.UtcNow
            };
            _context.UserModuleProgressions.Add(legacyModuleProgress);
        }
        else if (!legacyModuleProgress.IsCompleted)
        {
            legacyModuleProgress.IsCompleted = true;
            legacyModuleProgress.CompletedAt = DateTime.UtcNow;
        }
        else
        {
            return true;
        }
        
        await _context.SaveChangesAsync();

        var allModulesForSkill = await _context.RoadmapSteps
            .Where(r => r.SkillId == skillId && r.IsActive)
            .Select(r => r.Id)
            .ToListAsync();
            
        var completedModulesForSkill = await _context.UserModuleProgressions
            .Where(ump => ump.UserId == userId && allModulesForSkill.Contains(ump.ModuleId) && ump.IsCompleted)
            .Select(ump => ump.ModuleId)
            .ToListAsync();
            
        bool isSkillFullyCompleted = (allModulesForSkill.Count > 0 && completedModulesForSkill.Count == allModulesForSkill.Count);

        var legacyUser = await _context.Users
            .Include(u => u.UserSkills)
            .FirstOrDefaultAsync(u => u.Id == userId);
            
        if (legacyUser != null)
        {
            int xpReward = module.EstimatedHours * 10;
            legacyUser.TotalXP += xpReward;
            
            int newLevel = (int)(legacyUser.TotalXP / 100) + 1;
            if (newLevel > legacyUser.Level)
            {
                legacyUser.Level = newLevel;
            }
            
            legacyUser.UpdatedAt = DateTime.UtcNow;
            
            var completedSkillsCount = legacyUser.UserSkills.Count(us => us.IsCompleted);
            if (isSkillFullyCompleted && !legacyUser.UserSkills.Any(us => us.SkillId == skillId && us.IsCompleted)) 
            {
                completedSkillsCount++;
            }

            var customCompletedSkills = await _context.UserProgressions
                .Where(up => up.UserId == userId && up.CareerDomainId != "frontend" && up.CareerDomainId != "backend" && up.CareerDomainId != "fullstack" && up.CareerDomainId != "data" && up.CareerDomainId != "devops" && up.CareerDomainId != "ml" && up.CareerDomainId != "business" && up.CareerDomainId != "design" && up.CareerDomainId != "healthcare" && up.CareerDomainId != "education")
                .SumAsync(up => up.SkillsCompleted);

            int totalCompletedSkills = completedSkillsCount + customCompletedSkills;

            var unlockedBadges = await _context.Badges
                .Where(b => b.IsActive && (b.XPRequired <= legacyUser.TotalXP || b.SkillsCompletedRequired <= totalCompletedSkills))
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
                userSkill.IsCleared = true; 
                userSkill.CompletedAt = DateTime.UtcNow;
            }
        }

        var userProgress = await _context.UserProgressions
            .FirstOrDefaultAsync(up => up.UserId == userId && up.CareerDomainId == domainId);

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
                TotalSkills = totalDomainModules, 
                XPInDomain = module.EstimatedHours * 10,
                ProgressPercentage = totalDomainModules > 0 ? ((double)completedDomainModules / totalDomainModules) * 100 : 100
            };
            _context.UserProgressions.Add(userProgress);
        }
        else
        {
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
