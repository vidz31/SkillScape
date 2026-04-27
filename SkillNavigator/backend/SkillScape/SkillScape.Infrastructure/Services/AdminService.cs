using Microsoft.EntityFrameworkCore;
using SkillScape.Application.DTOs;
using SkillScape.Application.Interfaces;
using SkillScape.Domain.Entities;
using SkillScape.Infrastructure.Data;
using System.Text.Json;

namespace SkillScape.Infrastructure.Services;

public class AdminService : IAdminService
{
    private readonly ApplicationDbContext _context;

    public AdminService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<AdminDashboardStatsDto> GetDashboardAsync()
    {
        var totalUsers = await _context.Users.CountAsync();
        var totalMentors = await _context.Mentors.CountAsync();
        var pendingMentorApprovals = await _context.Mentors.CountAsync(m => !m.IsApproved);
        var totalSessions = await _context.MentorSessions.CountAsync();
        var completedSessions = await _context.MentorSessions.CountAsync(s => s.Status == "Completed");
        var activeStudents = await _context.Users.CountAsync(u => u.Role == "Student" && !u.IsBlocked);
        var totalRevenue = await _context.MentorSessions
            .Where(s => s.Status == "Completed")
            .Join(_context.Mentors, s => s.MentorId, m => m.Id, (s, m) => m.SessionPrice ?? m.HourlyRate)
            .SumAsync();

        var averageMentorRating = await _context.Mentors.AnyAsync()
            ? await _context.Mentors.AverageAsync(m => m.AvgRating)
            : 0;

        return new AdminDashboardStatsDto
        {
            TotalUsers = totalUsers,
            TotalMentors = totalMentors,
            PendingMentorApprovals = pendingMentorApprovals,
            TotalSessions = totalSessions,
            CompletedSessions = completedSessions,
            ActiveStudents = activeStudents,
            TotalRevenue = totalRevenue,
            AverageMentorRating = Math.Round(averageMentorRating, 2)
        };
    }

    public async Task<List<AdminUserDto>> GetUsersAsync(string? role, bool? isBlocked)
    {
        var query = _context.Users.AsQueryable();

        if (!string.IsNullOrWhiteSpace(role))
            query = query.Where(u => u.Role == role);

        if (isBlocked.HasValue)
            query = query.Where(u => u.IsBlocked == isBlocked.Value);

        return await query
            .OrderByDescending(u => u.CreatedAt)
            .Select(u => new AdminUserDto
            {
                Id = u.Id,
                Email = u.Email,
                FullName = u.FullName,
                Role = u.Role,
                IsBlocked = u.IsBlocked,
                BlockedReason = u.BlockedReason,
                ProfileCompleted = u.ProfileCompleted,
                CreatedAt = u.CreatedAt
            })
            .ToListAsync();
    }

    public async Task BlockOrUnblockUserAsync(string adminId, BlockUserDto request)
    {
        var user = await _context.Users.FindAsync(request.UserId)
            ?? throw new InvalidOperationException("User not found");

        if (user.Role == "Admin" && request.IsBlocked)
            throw new InvalidOperationException("Admin account cannot be blocked");

        user.IsBlocked = request.IsBlocked;
        user.BlockedReason = request.IsBlocked ? request.Reason : null;
        user.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        await AddAuditLog(adminId, request.IsBlocked ? "BlockUser" : "UnblockUser", "User", user.Id);
    }

    public async Task UpdateUserRoleAsync(string adminId, UpdateUserRoleDto request)
    {
        var user = await _context.Users.FindAsync(request.UserId)
            ?? throw new InvalidOperationException("User not found");

        var normalizedRole = request.Role?.Trim();
        if (normalizedRole is not ("Student" or "Mentor" or "Admin"))
            throw new InvalidOperationException("Role must be Student, Mentor, or Admin");

        user.Role = normalizedRole;
        user.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        await AddAuditLog(adminId, "UpdateUserRole", "User", user.Id);
    }

    public async Task<List<MentorDto>> GetPendingMentorsAsync()
    {
        var mentors = await _context.Mentors
            .Include(m => m.User)
            .Where(m => !m.IsApproved)
            .OrderByDescending(m => m.CreatedAt)
            .ToListAsync();

        return mentors.Select(MapMentor).ToList();
    }

    public async Task<MentorDto> ApproveMentorAsync(string adminId, string mentorId)
    {
        var mentor = await _context.Mentors
            .Include(m => m.User)
            .FirstOrDefaultAsync(m => m.Id == mentorId)
            ?? throw new InvalidOperationException("Mentor not found");

        mentor.IsApproved = true;
        mentor.IsAvailable = true;
        mentor.ApprovedByAdminId = adminId;
        mentor.ApprovedAt = DateTime.UtcNow;
        mentor.RejectionReason = null;

        await _context.SaveChangesAsync();
        await CreateNotification(mentor.UserId, "Your mentor profile has been approved.");
        await AddAuditLog(adminId, "ApproveMentor", "Mentor", mentor.Id);

        return MapMentor(mentor);
    }

    public async Task<MentorDto> RejectMentorAsync(string adminId, string mentorId, string reason)
    {
        var mentor = await _context.Mentors
            .Include(m => m.User)
            .FirstOrDefaultAsync(m => m.Id == mentorId)
            ?? throw new InvalidOperationException("Mentor not found");

        mentor.IsApproved = false;
        mentor.IsAvailable = false;
        mentor.ApprovedByAdminId = null;
        mentor.ApprovedAt = null;
        mentor.RejectionReason = reason;

        await _context.SaveChangesAsync();
        await CreateNotification(mentor.UserId, $"Your mentor profile was rejected. Reason: {reason}");
        await AddAuditLog(adminId, "RejectMentor", "Mentor", mentor.Id);

        return MapMentor(mentor);
    }

    public async Task<List<AdminSessionDto>> GetSessionsAsync(string? status)
    {
        var query = _context.MentorSessions
            .Include(s => s.Student)
            .Include(s => s.Mentor)
                .ThenInclude(m => m!.User)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(s => s.Status == status);

        var sessions = await query
            .OrderByDescending(s => s.ScheduledDateTime)
            .ToListAsync();

        return sessions.Select(s => new AdminSessionDto
        {
            Id = s.Id,
            StudentName = s.Student?.FullName ?? string.Empty,
            MentorName = s.Mentor?.User?.FullName ?? string.Empty,
            ScheduledDateTime = s.ScheduledDateTime,
            Status = s.Status,
            DurationMinutes = s.DurationMinutes,
            SessionPrice = s.Mentor?.SessionPrice ?? s.Mentor?.HourlyRate ?? 0
        }).ToList();
    }

    public async Task<PlatformAnalyticsDto> GetAnalyticsAsync()
    {
        var bestPath = await _context.QuizResults
            .GroupBy(q => q.RecommendedDomainId)
            .Select(g => new { Domain = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .FirstOrDefaultAsync();

        var mostSelectedPathName = "N/A";
        if (!string.IsNullOrWhiteSpace(bestPath?.Domain))
        {
            mostSelectedPathName = await _context.CareerDomains
                .Where(d => d.Id == bestPath.Domain)
                .Select(d => d.Name)
                .FirstOrDefaultAsync() ?? bestPath.Domain;
        }

        var weakestCategory = await _context.QuizQuestions
            .Select(q => new
            {
                q.Category,
                Attempts = q.Responses.Count
            })
            .OrderBy(x => x.Attempts)
            .Select(x => x.Category)
            .FirstOrDefaultAsync();

        var rawScoreJson = await _context.QuizResults
            .Where(q => !string.IsNullOrWhiteSpace(q.ScoresJson))
            .Select(q => q.ScoresJson)
            .ToListAsync();

        var parsedScores = new List<double>();
        foreach (var json in rawScoreJson)
        {
            try
            {
                var scoreMap = JsonSerializer.Deserialize<Dictionary<string, double>>(json!);
                if (scoreMap != null && scoreMap.Count > 0)
                {
                    parsedScores.Add(scoreMap.Values.Average());
                }
            }
            catch
            {
                // Ignore malformed score json entries
            }
        }

        var averageQuizScore = parsedScores.Count > 0
            ? Math.Round(Math.Clamp(parsedScores.Average(), 0, 100), 1)
            : 0.0;

        var today = DateTime.UtcNow.Date;
        var monthStart = new DateTime(today.Year, today.Month, 1);
        var thirtyDaysAgo = today.AddDays(-30);

        var dailySignups = await _context.Users.CountAsync(u => u.CreatedAt >= today);
        var activeUsersLast30Days = await _context.Users.CountAsync(u => u.UpdatedAt >= thirtyDaysAgo);
        var monthlySessions = await _context.MentorSessions.CountAsync(s => s.CreatedAt >= monthStart);

        var mentorSessionStats = await _context.MentorSessions
            .Where(s => s.Status == "Approved" || s.Status == "Completed")
            .GroupBy(s => s.MentorId)
            .Select(g => new
            {
                MentorId = g.Key,
                SessionsCount = g.Count()
            })
            .ToListAsync();

        var mentorIds = mentorSessionStats.Select(s => s.MentorId).ToList();

        var topMentorsRaw = await _context.Mentors
            .Include(m => m.User)
            .Where(m => mentorIds.Contains(m.Id))
            .ToListAsync();

        var mentorSessionCountMap = mentorSessionStats.ToDictionary(x => x.MentorId, x => x.SessionsCount);

        var topMentors = topMentorsRaw
            .Select(mentor =>
            {
                var sessionsCount = mentorSessionCountMap.TryGetValue(mentor.Id, out var count) ? count : 0;
                var sessionPrice = mentor.SessionPrice ?? mentor.HourlyRate;
                var revenue = sessionsCount * sessionPrice;

                return new MentorPerformanceDto
                {
                    MentorId = mentor.Id,
                    MentorName = mentor.User?.FullName ?? string.Empty,
                    CompletedSessions = sessionsCount,
                    AverageRating = mentor.AvgRating,
                    Revenue = revenue
                };
            })
            .OrderByDescending(m => m.CompletedSessions)
            .ThenByDescending(m => m.AverageRating)
            .Take(5)
            .ToList();

        return new PlatformAnalyticsDto
        {
            MostSelectedCareerPath = mostSelectedPathName,
            WeakestQuizCategory = weakestCategory ?? "N/A",
            AverageQuizScore = averageQuizScore,
            DailySignups = dailySignups,
            ActiveUsersLast30Days = activeUsersLast30Days,
            MonthlySessions = monthlySessions,
            TopMentors = topMentors
        };
    }

    public async Task BroadcastAnnouncementAsync(string adminId, string message)
    {
        if (string.IsNullOrWhiteSpace(message))
            throw new InvalidOperationException("Message is required");

        var userIds = await _context.Users
            .Where(u => !u.IsBlocked)
            .Select(u => u.Id)
            .ToListAsync();

        var notifications = userIds.Select(uid => new Notification
        {
            Id = Guid.NewGuid().ToString(),
            UserId = uid,
            Message = message,
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        });

        _context.Notifications.AddRange(notifications);
        await _context.SaveChangesAsync();
        await AddAuditLog(adminId, "BroadcastAnnouncement", "Notification", null);
    }

    public async Task<List<SessionComplaintDto>> GetComplaintsAsync(string? status)
    {
        var query = _context.SessionComplaints
            .Include(c => c.Reporter)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(c => c.Status == status);

        return await query
            .OrderByDescending(c => c.CreatedAt)
            .Select(c => new SessionComplaintDto
            {
                Id = c.Id,
                SessionId = c.SessionId,
                ReportedByName = c.Reporter!.FullName,
                Reason = c.Reason,
                Status = c.Status,
                CreatedAt = c.CreatedAt,
                ResolutionNote = c.ResolutionNote
            })
            .ToListAsync();
    }

    public async Task<SessionComplaintDto> ResolveComplaintAsync(string adminId, string complaintId, string resolutionNote)
    {
        var complaint = await _context.SessionComplaints
            .Include(c => c.Reporter)
            .FirstOrDefaultAsync(c => c.Id == complaintId)
            ?? throw new InvalidOperationException("Complaint not found");

        complaint.Status = "Resolved";
        complaint.ResolutionNote = resolutionNote;
        complaint.ResolvedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        await AddAuditLog(adminId, "ResolveComplaint", "SessionComplaint", complaint.Id);

        return new SessionComplaintDto
        {
            Id = complaint.Id,
            SessionId = complaint.SessionId,
            ReportedByName = complaint.Reporter?.FullName ?? string.Empty,
            Reason = complaint.Reason,
            Status = complaint.Status,
            CreatedAt = complaint.CreatedAt,
            ResolutionNote = complaint.ResolutionNote
        };
    }

    public async Task<List<AdminQuizQuestionCrudDto>> GetQuizQuestionsAsync()
    {
        var questions = await _context.QuizQuestions
            .Include(q => q.CareerDomain)
            .Include(q => q.Options)
            .OrderBy(q => q.DisplayOrder)
            .ToListAsync();

        return questions.Select(MapQuizQuestion).ToList();
    }

    public async Task<AdminQuizQuestionCrudDto> CreateQuizQuestionAsync(string adminId, UpsertAdminQuizQuestionDto request)
    {
        var domain = await _context.CareerDomains.FindAsync(request.CareerDomainId)
            ?? throw new InvalidOperationException("Career domain not found");

        if (request.Options == null || request.Options.Count == 0)
            throw new InvalidOperationException("At least one quiz option is required");

        var question = new QuizQuestion
        {
            Id = Guid.NewGuid().ToString(),
            CareerDomainId = request.CareerDomainId,
            Text = request.Text,
            Category = request.Category,
            DisplayOrder = request.DisplayOrder,
            IsActive = request.IsActive,
            CreatedAt = DateTime.UtcNow,
            Options = request.Options.Select(option => new QuizOption
            {
                Id = Guid.NewGuid().ToString(),
                Text = option.Text,
                DisplayOrder = option.DisplayOrder,
                DomainWeightJson = JsonSerializer.Serialize(option.DomainWeights ?? new Dictionary<string, int>())
            }).ToList()
        };

        _context.QuizQuestions.Add(question);
        await _context.SaveChangesAsync();
        await AddAuditLog(adminId, "CreateQuizQuestion", "QuizQuestion", question.Id);

        question.CareerDomain = domain;
        return MapQuizQuestion(question);
    }

    public async Task<AdminQuizQuestionCrudDto> UpdateQuizQuestionAsync(string adminId, string questionId, UpsertAdminQuizQuestionDto request)
    {
        var question = await _context.QuizQuestions
            .Include(q => q.CareerDomain)
            .Include(q => q.Options)
            .FirstOrDefaultAsync(q => q.Id == questionId)
            ?? throw new InvalidOperationException("Quiz question not found");

        var domain = await _context.CareerDomains.FindAsync(request.CareerDomainId)
            ?? throw new InvalidOperationException("Career domain not found");

        question.CareerDomainId = request.CareerDomainId;
        question.Text = request.Text;
        question.Category = request.Category;
        question.DisplayOrder = request.DisplayOrder;
        question.IsActive = request.IsActive;

        _context.QuizOptions.RemoveRange(question.Options);
        question.Options = request.Options.Select(option => new QuizOption
        {
            Id = string.IsNullOrWhiteSpace(option.Id) ? Guid.NewGuid().ToString() : option.Id!,
            QuizQuestionId = question.Id,
            Text = option.Text,
            DisplayOrder = option.DisplayOrder,
            DomainWeightJson = JsonSerializer.Serialize(option.DomainWeights ?? new Dictionary<string, int>())
        }).ToList();

        await _context.SaveChangesAsync();
        await AddAuditLog(adminId, "UpdateQuizQuestion", "QuizQuestion", question.Id);

        question.CareerDomain = domain;
        return MapQuizQuestion(question);
    }

    public async Task DeleteQuizQuestionAsync(string adminId, string questionId)
    {
        var question = await _context.QuizQuestions
            .Include(q => q.Options)
            .FirstOrDefaultAsync(q => q.Id == questionId)
            ?? throw new InvalidOperationException("Quiz question not found");

        _context.QuizQuestions.Remove(question);
        await _context.SaveChangesAsync();
        await AddAuditLog(adminId, "DeleteQuizQuestion", "QuizQuestion", questionId);
    }

    public async Task<List<AdminRoadmapModuleCrudDto>> GetRoadmapModulesAsync(string? domainId)
    {
        var query = _context.RoadmapSteps
            .Include(r => r.CareerDomain)
            .Include(r => r.Skill)
            .Include(r => r.Topics)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(domainId))
            query = query.Where(r => r.CareerDomainId == domainId);

        var modules = await query
            .OrderBy(r => r.CareerDomain!.DisplayOrder)
            .ThenBy(r => r.Skill!.DisplayOrder)
            .ThenBy(r => r.StepNumber)
            .ToListAsync();

        return modules.Select(MapRoadmapModule).ToList();
    }

    public async Task<AdminRoadmapModuleCrudDto> CreateRoadmapModuleAsync(string adminId, UpsertAdminRoadmapModuleDto request)
    {
        var domain = await _context.CareerDomains.FindAsync(request.CareerDomainId)
            ?? throw new InvalidOperationException("Career domain not found");

        var skill = await _context.Skills.FirstOrDefaultAsync(s => s.Id == request.SkillId && s.CareerDomainId == request.CareerDomainId)
            ?? throw new InvalidOperationException("Skill not found for selected domain");

        var module = new RoadmapStep
        {
            Id = Guid.NewGuid().ToString(),
            CareerDomainId = request.CareerDomainId,
            SkillId = request.SkillId,
            Title = request.Title,
            Description = request.Description,
            StepNumber = request.StepNumber,
            EstimatedHours = request.EstimatedHours,
            IsActive = request.IsActive,
            Topics = request.Topics.Select(topic => new RoadmapTopic
            {
                Id = Guid.NewGuid().ToString(),
                Title = topic.Title,
                ResourceUrl = topic.ResourceUrl,
                DisplayOrder = topic.DisplayOrder
            }).ToList()
        };

        _context.RoadmapSteps.Add(module);
        await _context.SaveChangesAsync();
        await AddAuditLog(adminId, "CreateRoadmapModule", "RoadmapStep", module.Id);

        module.CareerDomain = domain;
        module.Skill = skill;
        return MapRoadmapModule(module);
    }

    public async Task<AdminRoadmapModuleCrudDto> UpdateRoadmapModuleAsync(string adminId, string moduleId, UpsertAdminRoadmapModuleDto request)
    {
        var module = await _context.RoadmapSteps
            .Include(r => r.CareerDomain)
            .Include(r => r.Skill)
            .Include(r => r.Topics)
            .FirstOrDefaultAsync(r => r.Id == moduleId)
            ?? throw new InvalidOperationException("Roadmap module not found");

        var domain = await _context.CareerDomains.FindAsync(request.CareerDomainId)
            ?? throw new InvalidOperationException("Career domain not found");

        var skill = await _context.Skills.FirstOrDefaultAsync(s => s.Id == request.SkillId && s.CareerDomainId == request.CareerDomainId)
            ?? throw new InvalidOperationException("Skill not found for selected domain");

        module.CareerDomainId = request.CareerDomainId;
        module.SkillId = request.SkillId;
        module.Title = request.Title;
        module.Description = request.Description;
        module.StepNumber = request.StepNumber;
        module.EstimatedHours = request.EstimatedHours;
        module.IsActive = request.IsActive;

        _context.RoadmapTopics.RemoveRange(module.Topics);
        module.Topics = request.Topics.Select(topic => new RoadmapTopic
        {
            Id = string.IsNullOrWhiteSpace(topic.Id) ? Guid.NewGuid().ToString() : topic.Id!,
            ModuleId = module.Id,
            Title = topic.Title,
            ResourceUrl = topic.ResourceUrl,
            DisplayOrder = topic.DisplayOrder
        }).ToList();

        await _context.SaveChangesAsync();
        await AddAuditLog(adminId, "UpdateRoadmapModule", "RoadmapStep", module.Id);

        module.CareerDomain = domain;
        module.Skill = skill;
        return MapRoadmapModule(module);
    }

    public async Task DeleteRoadmapModuleAsync(string adminId, string moduleId)
    {
        var module = await _context.RoadmapSteps.FindAsync(moduleId)
            ?? throw new InvalidOperationException("Roadmap module not found");

        _context.RoadmapSteps.Remove(module);
        await _context.SaveChangesAsync();
        await AddAuditLog(adminId, "DeleteRoadmapModule", "RoadmapStep", moduleId);
    }

    public async Task<List<AdminMentorCrudDto>> GetAllMentorsAsync()
    {
        var mentors = await _context.Mentors
            .Include(m => m.User)
            .OrderByDescending(m => m.CreatedAt)
            .ToListAsync();

        return mentors.Select(mentor => new AdminMentorCrudDto
        {
            Id = mentor.Id,
            UserId = mentor.UserId,
            UserName = mentor.User?.FullName ?? string.Empty,
            Email = mentor.User?.Email ?? string.Empty,
            IsApproved = mentor.IsApproved,
            IsBlocked = mentor.User?.IsBlocked ?? false,
            BlockedReason = mentor.User?.BlockedReason,
            ExpertiseArea = mentor.ExpertiseArea,
            YearsOfExperience = mentor.YearsOfExperience,
            SessionPrice = mentor.SessionPrice ?? mentor.HourlyRate,
            IsAvailable = mentor.IsAvailable,
            CreatedAt = mentor.CreatedAt
        }).ToList();
    }

    public async Task<AdminMentorCrudDto> CreateMentorAsync(string adminId, CreateAdminMentorDto request)
    {
        var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
        if (existingUser != null)
            throw new InvalidOperationException("Email already exists");

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid().ToString(),
            Email = request.Email,
            FullName = request.FullName,
            Role = "Mentor",
            Bio = request.Bio,
            ProfileCompleted = true,
            IsBlocked = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            IsActive = true
        };

        var mentor = new ApplicationMentor
        {
            Id = Guid.NewGuid().ToString(),
            UserId = user.Id,
            ExpertiseArea = request.ExpertiseArea,
            Expertise = request.ExpertiseArea,
            CurrentCompany = request.CurrentCompany,
            Bio = request.Bio,
            SkillsCsv = string.Join(',', request.Skills.Where(s => !string.IsNullOrWhiteSpace(s)).Select(s => s.Trim())),
            LinkedInUrl = request.LinkedInUrl,
            AvailabilitySchedule = request.AvailabilitySchedule,
            YearsOfExperience = request.YearsOfExperience,
            SessionPrice = request.SessionPrice,
            HourlyRate = request.SessionPrice,
            IsAvailable = true,
            IsApproved = true,
            ApprovedByAdminId = adminId,
            ApprovedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };

        _context.Users.Add(user);
        _context.Mentors.Add(mentor);
        await _context.SaveChangesAsync();

        await AddAuditLog(adminId, "CreateMentor", "Mentor", mentor.Id);

        return new AdminMentorCrudDto
        {
            Id = mentor.Id,
            UserId = mentor.UserId,
            UserName = user.FullName,
            Email = user.Email,
            IsApproved = mentor.IsApproved,
            IsBlocked = false,
            BlockedReason = null,
            ExpertiseArea = mentor.ExpertiseArea,
            YearsOfExperience = mentor.YearsOfExperience,
            SessionPrice = mentor.SessionPrice ?? mentor.HourlyRate,
            IsAvailable = mentor.IsAvailable,
            CreatedAt = mentor.CreatedAt
        };
    }

    public async Task BlockOrUnblockMentorAsync(string adminId, string mentorId, AdminBlockMentorDto request)
    {
        var mentor = await _context.Mentors
            .Include(m => m.User)
            .FirstOrDefaultAsync(m => m.Id == mentorId)
            ?? throw new InvalidOperationException("Mentor not found");

        if (mentor.User == null)
            throw new InvalidOperationException("Mentor user not found");

        mentor.User.IsBlocked = request.IsBlocked;
        mentor.User.BlockedReason = request.IsBlocked ? request.Reason : null;
        mentor.User.UpdatedAt = DateTime.UtcNow;

        if (request.IsBlocked)
            mentor.IsAvailable = false;

        await _context.SaveChangesAsync();
        await AddAuditLog(adminId, request.IsBlocked ? "BlockMentor" : "UnblockMentor", "Mentor", mentorId);
    }

    public async Task DeleteMentorAsync(string adminId, string mentorId)
    {
        var mentor = await _context.Mentors
            .Include(m => m.User)
            .FirstOrDefaultAsync(m => m.Id == mentorId)
            ?? throw new InvalidOperationException("Mentor not found");

        _context.Mentors.Remove(mentor);

        if (mentor.User != null)
        {
            mentor.User.Role = "Student";
            mentor.User.UpdatedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();
        await AddAuditLog(adminId, "DeleteMentor", "Mentor", mentorId);
    }

    public async Task<List<AdminResumeSummaryDto>> GetResumeSummariesAsync()
    {
        var users = await _context.Users
            .Include(u => u.UserSkills)
            .Include(u => u.UserBadges)
            .OrderByDescending(u => u.UpdatedAt)
            .ToListAsync();

        var summaries = users
            .Select(user => new AdminResumeSummaryDto
            {
                UserId = user.Id,
                Name = user.FullName,
                Email = user.Email,
                Role = user.Role,
                CompletedSkills = user.UserSkills.Count(s => s.IsCompleted),
                Certifications = user.UserBadges.Count,
                TotalXP = user.TotalXP,
                Level = user.Level,
                UpdatedAt = user.UpdatedAt
            })
            .Where(summary => summary.CompletedSkills > 0 || summary.Certifications > 0)
            .ToList();

        return summaries;
    }

    public async Task<ResumePreviewDto> GetResumeByUserIdAsync(string userId)
    {
        var user = await _context.Users
            .Include(u => u.UserSkills)
                .ThenInclude(us => us.Skill)
                    .ThenInclude(s => s.CareerDomain)
            .Include(u => u.UserBadges)
                .ThenInclude(ub => ub.Badge)
            .FirstOrDefaultAsync(u => u.Id == userId)
            ?? throw new InvalidOperationException("User not found");

        var completedSkills = user.UserSkills
            .Where(us => us.IsCompleted)
            .ToList();

        var domainSkills = completedSkills
            .GroupBy(us => us.Skill.CareerDomain)
            .Select(group => new DomainSkillsDto
            {
                DomainName = group.Key?.Name ?? "General",
                CompletedSkills = group.Select(us => us.Skill.Name).ToList(),
                TotalSkillsCompleted = group.Count()
            })
            .ToList();

        var allSkills = completedSkills
            .Select(us => us.Skill.Name)
            .Distinct()
            .OrderBy(skill => skill)
            .ToList();

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
            Summary = $"Motivated learner at Level {user.Level} with {user.TotalXP} XP earned through SkillScape. Completed {completedSkills.Count} skills across {domainSkills.Count} domains.",
            Skills = allSkills,
            DomainSkills = domainSkills,
            Certifications = certifications
        };
    }

    public async Task<List<AdminUserProgressSnapshotDto>> GetUserProgressSnapshotAsync()
    {
        var users = await _context.Users
            .Include(u => u.UserSkills)
            .Include(u => u.UserProgressions)
            .OrderByDescending(u => u.UpdatedAt)
            .ToListAsync();

        return users.Select(user => new AdminUserProgressSnapshotDto
        {
            UserId = user.Id,
            FullName = user.FullName,
            Email = user.Email,
            Role = user.Role,
            Level = user.Level,
            TotalXP = user.TotalXP,
            CompletedSkills = user.UserSkills.Count(skill => skill.IsCompleted),
            AverageDomainProgress = Math.Round(user.UserProgressions.Any() ? user.UserProgressions.Average(progress => progress.ProgressPercentage) : 0, 2),
            UpdatedAt = user.UpdatedAt
        }).ToList();
    }

    private AdminQuizQuestionCrudDto MapQuizQuestion(QuizQuestion question)
    {
        return new AdminQuizQuestionCrudDto
        {
            Id = question.Id,
            CareerDomainId = question.CareerDomainId,
            CareerDomainName = question.CareerDomain?.Name ?? string.Empty,
            Text = question.Text,
            Category = question.Category,
            DisplayOrder = question.DisplayOrder,
            IsActive = question.IsActive,
            Options = question.Options
                .OrderBy(option => option.DisplayOrder)
                .Select(option => new AdminQuizOptionCrudDto
                {
                    Id = option.Id,
                    Text = option.Text,
                    DisplayOrder = option.DisplayOrder,
                    DomainWeights = ParseWeights(option.DomainWeightJson)
                })
                .ToList()
        };
    }

    private AdminRoadmapModuleCrudDto MapRoadmapModule(RoadmapStep module)
    {
        return new AdminRoadmapModuleCrudDto
        {
            Id = module.Id,
            CareerDomainId = module.CareerDomainId,
            CareerDomainName = module.CareerDomain?.Name ?? string.Empty,
            SkillId = module.SkillId,
            SkillName = module.Skill?.Name ?? string.Empty,
            Title = module.Title,
            Description = module.Description,
            StepNumber = module.StepNumber,
            EstimatedHours = module.EstimatedHours,
            IsActive = module.IsActive,
            Topics = module.Topics
                .OrderBy(topic => topic.DisplayOrder)
                .Select(topic => new AdminRoadmapTopicCrudDto
                {
                    Id = topic.Id,
                    Title = topic.Title,
                    ResourceUrl = topic.ResourceUrl,
                    DisplayOrder = topic.DisplayOrder
                })
                .ToList()
        };
    }

    private Dictionary<string, int> ParseWeights(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return new Dictionary<string, int>();

        try
        {
            return JsonSerializer.Deserialize<Dictionary<string, int>>(json) ?? new Dictionary<string, int>();
        }
        catch
        {
            return new Dictionary<string, int>();
        }
    }

    private MentorDto MapMentor(ApplicationMentor mentor)
    {
        var skills = string.IsNullOrWhiteSpace(mentor.SkillsCsv)
            ? new List<string>()
            : mentor.SkillsCsv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();

        return new MentorDto
        {
            Id = mentor.Id,
            UserId = mentor.UserId,
            UserName = mentor.User?.FullName ?? string.Empty,
            Expertise = mentor.Expertise,
            ExpertiseArea = mentor.ExpertiseArea,
            CurrentCompany = mentor.CurrentCompany,
            Bio = mentor.Bio,
            Skills = skills,
            LinkedInUrl = mentor.LinkedInUrl,
            AvailabilitySchedule = mentor.AvailabilitySchedule,
            YearsOfExperience = mentor.YearsOfExperience,
            HourlyRate = mentor.HourlyRate,
            SessionPrice = mentor.SessionPrice,
            IsAvailable = mentor.IsAvailable,
            IsApproved = mentor.IsApproved,
            ApprovedByAdminId = mentor.ApprovedByAdminId,
            ApprovedAt = mentor.ApprovedAt,
            RejectionReason = mentor.RejectionReason,
            TotalStudentsAssigned = 0
        };
    }

    private async Task AddAuditLog(string adminId, string action, string targetEntity, string? targetEntityId)
    {
        _context.AdminAuditLogs.Add(new AdminAuditLog
        {
            Id = Guid.NewGuid().ToString(),
            AdminId = adminId,
            Action = action,
            TargetEntity = targetEntity,
            TargetEntityId = targetEntityId,
            Timestamp = DateTime.UtcNow
        });

        await _context.SaveChangesAsync();
    }

    private async Task CreateNotification(string userId, string message)
    {
        _context.Notifications.Add(new Notification
        {
            Id = Guid.NewGuid().ToString(),
            UserId = userId,
            Message = message,
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        });

        await _context.SaveChangesAsync();
    }
}
