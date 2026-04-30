using Microsoft.EntityFrameworkCore;
using SkillScape.Application.DTOs;
using SkillScape.Application.Interfaces;
using SkillScape.Domain.Entities;
using SkillScape.Infrastructure.Data;

namespace SkillScape.Infrastructure.Services;

public class ProgressService : IProgressService
{
    private readonly ApplicationDbContext _context;
    private const int XP_PER_SKILL = 10;
    private const long XP_PER_LEVEL = 100;

    public ProgressService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<UserStatsDto> GetUserStatsAsync(string userId)
    {
        var user = await _context.Users
            .Include(u => u.UserSkills)
            .FirstOrDefaultAsync(u => u.Id == userId)
            ?? throw new InvalidOperationException("User not found");

        var utcToday = DateTime.UtcNow.Date;
        var utcTomorrow = utcToday.AddDays(1);
        var weekStart = utcToday.AddDays(-((7 + (int)utcToday.DayOfWeek - (int)DayOfWeek.Monday) % 7));

        var currentStreak = await CalculateLearningStreakAsync(userId, DateTime.UtcNow);
        if (user.CurrentStreak != currentStreak)
        {
            user.CurrentStreak = currentStreak;
            user.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }

        var xpEarnedToday = await _context.UserSkills
            .Where(us => us.UserId == userId && us.IsCompleted && us.CompletedAt.HasValue
                && us.CompletedAt.Value >= utcToday
                && us.CompletedAt.Value < utcTomorrow)
            .Join(
                _context.Skills,
                userSkill => userSkill.SkillId,
                skill => skill.Id,
                (_, skill) => (long)skill.XPReward
            )
            .SumAsync();

        var skillsCompletedThisWeek = await _context.UserSkills
            .CountAsync(us => us.UserId == userId && us.IsCompleted && us.CompletedAt.HasValue
                && us.CompletedAt.Value >= weekStart
                && us.CompletedAt.Value < utcTomorrow);

        // Calculate domain progressions dynamically from all domains
        var allDomains = await _context.CareerDomains
            .Include(d => d.Skills)
            .ToListAsync();

        var userCompletedSkills = await _context.UserSkills
            .Include(us => us.Skill)
            .Where(us => us.UserId == userId && us.IsCompleted)
            .Select(us => new { us.SkillId, us.Skill.CareerDomainId, us.Skill.XPReward })
            .ToListAsync();

        var progressions = allDomains
            .Where(domain => domain.Skills.Count > 0)
            .Select(domain =>
            {
                var totalSkillsInDomain = domain.Skills.Count;
                var completedSkillsInDomain = userCompletedSkills
                    .Count(us => us.CareerDomainId == domain.Id);
                var xpInDomain = userCompletedSkills
                    .Where(us => us.CareerDomainId == domain.Id)
                    .Sum(us => (long)us.XPReward);
                var progressPercentage = totalSkillsInDomain > 0
                    ? (completedSkillsInDomain / (double)totalSkillsInDomain) * 100
                    : 0;

                return new UserProgressDto
                {
                    Id = domain.Id,
                    DomainId = domain.Id,
                    DomainName = domain.Name,
                    XPInDomain = xpInDomain,
                    SkillsCompleted = completedSkillsInDomain,
                    TotalSkills = totalSkillsInDomain,
                    ProgressPercentage = progressPercentage
                };
            })
            .Where(p => p.SkillsCompleted > 0)
            .OrderByDescending(p => p.ProgressPercentage)
            .ToList();

        var xpToNextLevel = (user.Level * XP_PER_LEVEL) - user.TotalXP;

        return new UserStatsDto
        {
            Level = user.Level,
            TotalXP = user.TotalXP,
            XPToNextLevel = xpToNextLevel,
            CurrentStreak = currentStreak,
            CompletedSkills = (int)user.UserSkills.Count(us => us.IsCompleted),
            XpEarnedToday = xpEarnedToday,
            SkillsCompletedThisWeek = skillsCompletedThisWeek,
            DomainProgressions = progressions
        };
    }

    private async Task<int> CalculateLearningStreakAsync(string userId, DateTime utcNow)
    {
        var today = utcNow.Date;

        var skillActivityDays = await _context.UserSkills
            .Where(us => us.UserId == userId && us.IsCompleted && us.CompletedAt.HasValue)
            .Select(us => us.CompletedAt!.Value.Date)
            .ToListAsync();

        var moduleActivityDays = await _context.UserModuleProgressions
            .Where(ump => ump.UserId == userId && ump.IsCompleted && ump.CompletedAt.HasValue)
            .Select(ump => ump.CompletedAt!.Value.Date)
            .ToListAsync();

        var quizActivityDays = await _context.QuizResponses
            .Where(qr => qr.UserId == userId)
            .Select(qr => qr.AnsweredAt.Date)
            .ToListAsync();

        var activityDays = skillActivityDays
            .Concat(moduleActivityDays)
            .Concat(quizActivityDays)
            .Distinct()
            .ToHashSet();

        if (activityDays.Count == 0)
        {
            return 0;
        }

        var startDate = activityDays.Contains(today)
            ? today
            : activityDays.Contains(today.AddDays(-1))
                ? today.AddDays(-1)
                : DateTime.MinValue;

        if (startDate == DateTime.MinValue)
        {
            return 0;
        }

        var streak = 0;
        var cursor = startDate;
        while (activityDays.Contains(cursor))
        {
            streak++;
            cursor = cursor.AddDays(-1);
        }

        return streak;
    }

    public async Task CompleteSkillAsync(string userId, string skillId)
    {
        var user = await _context.Users.FindAsync(userId)
            ?? throw new InvalidOperationException("User not found");

        var skill = await _context.Skills
            .Include(s => s.CareerDomain)
            .FirstOrDefaultAsync(s => s.Id == skillId)
            ?? throw new InvalidOperationException("Skill not found");

        var userSkill = await _context.UserSkills
            .FirstOrDefaultAsync(us => us.UserId == userId && us.SkillId == skillId);

        if (userSkill == null)
        {
            userSkill = new UserSkill
            {
                Id = Guid.NewGuid().ToString(),
                UserId = userId,
                SkillId = skillId,
                IsCompleted = true,
                CompletedAt = DateTime.UtcNow,
                ProgressPercentage = 100,
                StartedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.UserSkills.Add(userSkill);
        }
        else if (!userSkill.IsCompleted)
        {
            userSkill.IsCompleted = true;
            userSkill.CompletedAt = DateTime.UtcNow;
            userSkill.ProgressPercentage = 100;
            _context.UserSkills.Update(userSkill);
        }
        else
        {
            throw new InvalidOperationException("Skill already completed");
        }

        // Award XP
        var xpReward = skill.XPReward;
        user.TotalXP += xpReward;

        // Update level
        var oldLevel = user.Level;
        var newLevel = (int)(user.TotalXP / XP_PER_LEVEL) + 1;
        if (newLevel > user.Level)
        {
            user.Level = newLevel;
            // Notify user about level up
            await CreateNotificationAsync(userId, "LevelUp", "Level Up!", $"Congratulations! You've reached Level {newLevel}!");
        }

        // Update domain progress
        var domainProgress = await _context.UserProgressions
            .FirstOrDefaultAsync(up => up.UserId == userId && up.CareerDomainId == skill.CareerDomainId);

        if (domainProgress == null)
        {
            var totalSkillsInDomain = await _context.Skills
                .CountAsync(s => s.CareerDomainId == skill.CareerDomainId);

            domainProgress = new UserProgress
            {
                Id = Guid.NewGuid().ToString(),
                UserId = userId,
                CareerDomainId = skill.CareerDomainId,
                XPInDomain = xpReward,
                SkillsCompleted = 1,
                TotalSkills = totalSkillsInDomain,
                ProgressPercentage = (1.0 / totalSkillsInDomain) * 100,
                StartedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.UserProgressions.Add(domainProgress);
        }
        else
        {
            domainProgress.XPInDomain += xpReward;
            domainProgress.SkillsCompleted += 1;
            domainProgress.ProgressPercentage = (domainProgress.SkillsCompleted / (double)domainProgress.TotalSkills) * 100;
            domainProgress.UpdatedAt = DateTime.UtcNow;
            _context.UserProgressions.Update(domainProgress);
        }

        user.UpdatedAt = DateTime.UtcNow;
        _context.Users.Update(user);

        // Check for badge unlocks
        await CheckAndUnlockBadgesAsync(userId, user);

        await _context.SaveChangesAsync();
    }

    public async Task<List<BadgeDto>> GetUserBadgesAsync(string userId)
    {
        var allBadges = await _context.Badges.ToListAsync();
        var earnedBadges = await _context.UserBadges
            .Where(ub => ub.UserId == userId)
            .Select(ub => ub.BadgeId)
            .ToListAsync();

        return allBadges
            .Select(b => new BadgeDto
            {
                Id = b.Id,
                Name = b.Name,
                Description = b.Description,
                IconUrl = b.IconUrl,
                Rarity = b.Rarity,
                Earned = earnedBadges.Contains(b.Id),
                EarnedAt = earnedBadges.Contains(b.Id)
                    ? _context.UserBadges
                        .Where(ub => ub.UserId == userId && ub.BadgeId == b.Id)
                        .FirstOrDefault()?.EarnedAt
                    : null
            })
            .ToList();
    }

    private async Task CheckAndUnlockBadgesAsync(string userId, ApplicationUser user)
    {
        var unlockedBadges = await _context.Badges
            .Where(b => b.IsActive && (b.XPRequired <= user.TotalXP || b.SkillsCompletedRequired <= user.UserSkills.Count(us => us.IsCompleted)))
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
                
                // Notify user about badge earned
                await CreateNotificationAsync(userId, "Badge", "New Badge Earned!", $"Congratulations! You've earned the \"{badge.Name}\" badge!");
            }
        }
    }

    public async Task<List<LeaderboardUserDto>> GetLeaderboardAsync(string currentUserId, int limit = 10)
    {
        var topUsers = await _context.Users
            .OrderByDescending(u => u.TotalXP)
            .Take(limit)
            .ToListAsync();

        var leaderboard = new List<LeaderboardUserDto>();
        int rank = 1;
        
        // Fallback avatars for users without profile images
        var fallbackAvatars = new[] { "👨‍💻", "👩‍💻", "👨", "👩‍🎓", "🎯", "🌟", "🚀", "💡" };

        foreach (var user in topUsers)
        {
            leaderboard.Add(new LeaderboardUserDto
            {
                Rank = rank,
                Name = string.IsNullOrWhiteSpace(user.FullName) ? "Anonymous Learner" : user.FullName,
                Xp = user.TotalXP,
                Level = user.Level,
                IsUser = user.Id == currentUserId,
                Avatar = !string.IsNullOrWhiteSpace(user.ProfileImageUrl) 
                    ? user.ProfileImageUrl 
                    : fallbackAvatars[(rank - 1) % fallbackAvatars.Length]
            });
            rank++;
        }

        // If current user is not in top 10, append them at the end with true rank
        if (!leaderboard.Any(l => l.IsUser))
        {
            var currentUser = await _context.Users.FindAsync(currentUserId);
            if (currentUser != null)
            {
                var userRank = await _context.Users.CountAsync(u => u.TotalXP > currentUser.TotalXP) + 1;
                leaderboard.Add(new LeaderboardUserDto
                {
                    Rank = userRank,
                    Name = string.IsNullOrWhiteSpace(currentUser.FullName) ? "You" : currentUser.FullName,
                    Xp = currentUser.TotalXP,
                    Level = currentUser.Level,
                    IsUser = true,
                    Avatar = !string.IsNullOrWhiteSpace(currentUser.ProfileImageUrl) 
                        ? currentUser.ProfileImageUrl 
                        : fallbackAvatars[(userRank - 1) % fallbackAvatars.Length]
                });
            }
        }

        return leaderboard;
    }

    // Helper method to create notifications
    private async Task CreateNotificationAsync(string userId, string type, string title, string message)
    {
        var notification = new Notification
        {
            UserId = userId,
            Type = type,
            Title = title,
            Message = message,
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        };

        _context.Notifications.Add(notification);
        await _context.SaveChangesAsync();
    }
}
