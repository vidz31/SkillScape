using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SkillScape.Application.DTOs;
using SkillScape.Application.Interfaces;
using SkillScape.Domain.Entities;
using SkillScape.Infrastructure.Data;

namespace SkillScape.Infrastructure.Services;

public class MentorService : IMentorService
{
    private readonly ApplicationDbContext _context;

    public MentorService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<MentorDto> CreateOrUpdateMentorProfileAsync(string userId, CreateMentorProfileDto request)
    {
        var user = await _context.Users.FindAsync(userId)
            ?? throw new InvalidOperationException("User not found");

        var mentor = await _context.Mentors
            .Include(m => m.User)
            .FirstOrDefaultAsync(m => m.UserId == userId);

        var isNewMentorProfile = mentor == null;
        if (mentor == null)
        {
            mentor = new ApplicationMentor
            {
                Id = Guid.NewGuid().ToString(),
                UserId = userId,
                CreatedAt = DateTime.UtcNow,
                IsApproved = false
            };
            _context.Mentors.Add(mentor);
        }

        var previousSessionPrice = mentor.SessionPrice ?? mentor.HourlyRate;
        var updatedSessionPrice = request.SessionPrice ?? 0;

        mentor.ExpertiseArea = request.ExpertiseArea;
        mentor.Expertise = string.IsNullOrWhiteSpace(request.Expertise) ? request.ExpertiseArea : request.Expertise;
        mentor.YearsOfExperience = request.YearsOfExperience;
        mentor.CurrentCompany = request.CurrentCompany;
        mentor.Bio = request.Bio;
        mentor.SkillsCsv = string.Join(',', request.Skills.Where(s => !string.IsNullOrWhiteSpace(s)).Select(s => s.Trim()));
        mentor.LinkedInUrl = request.LinkedInUrl;
        mentor.AvailabilitySchedule = request.AvailabilitySchedule;
        mentor.SessionPrice = request.SessionPrice;
        mentor.HourlyRate = request.SessionPrice ?? 0;
        mentor.IsAvailable = true;

        if (updatedSessionPrice > 0 && (isNewMentorProfile || previousSessionPrice != updatedSessionPrice))
        {
            _context.MentorSessionPriceHistory.Add(new MentorSessionPriceHistory
            {
                MentorId = mentor.Id,
                SessionPrice = updatedSessionPrice,
                EffectiveFrom = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow
            });
        }

        user.Role = "Mentor";
        user.ProfileCompleted = true;
        user.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return MapMentor(mentor, mentor.User ?? user);
    }

    public async Task<MentorDto> GetMentorByUserIdAsync(string userId)
    {
        var mentor = await _context.Mentors
            .Include(m => m.User)
            .Include(m => m.MentorshipProgressEntries)
            .FirstOrDefaultAsync(m => m.UserId == userId)
            ?? throw new InvalidOperationException("Mentor profile not found");

        return MapMentor(mentor, mentor.User);
    }

    public async Task<MentorDto> ApproveMentorProfileAsync(string mentorId)
    {
        var mentor = await _context.Mentors
            .Include(m => m.User)
            .FirstOrDefaultAsync(m => m.Id == mentorId)
            ?? throw new InvalidOperationException("Mentor not found");

        mentor.IsApproved = true;
        mentor.IsAvailable = true;
        mentor.ApprovedAt = DateTime.UtcNow;
        mentor.RejectionReason = null;

        await _context.SaveChangesAsync();
        await CreateNotificationAsync(mentor.UserId, "MentorApproval", "Mentor Approved", "Congratulations! Your mentor application has been approved by the admin.");

        return MapMentor(mentor, mentor.User);
    }

    public async Task<List<MentorDto>> GetAllMentorsAsync()
    {
        var mentors = await _context.Mentors
            .Include(m => m.User)
            .Include(m => m.MentorshipProgressEntries)
            .Where(m => m.IsAvailable && m.IsApproved)
            .OrderByDescending(m => m.AvgRating)
            .ToListAsync();

        return mentors.Select(m => MapMentor(m, m.User)).ToList();
    }

    public async Task<List<MentorDto>> GetAllMentorsDetailedForUsersAsync()
    {
        var mentors = await _context.Mentors
            .Include(m => m.User)
            .Include(m => m.MentorshipProgressEntries)
            .Where(m => m.IsApproved && m.IsAvailable)
            .OrderByDescending(m => m.AvgRating)
            .ThenByDescending(m => m.CreatedAt)
            .ToListAsync();

        return mentors.Select(m => MapMentor(m, m.User)).ToList();
    }

    public async Task<List<MentorDto>> GetMentorsAsync(MentorFilterDto filter)
    {
        var query = _context.Mentors
            .Include(m => m.User)
            .Include(m => m.MentorshipProgressEntries)
            .Where(m => m.IsAvailable && m.IsApproved)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(filter.Domain))
        {
            var domain = filter.Domain.Trim().ToLower();
            query = query.Where(m =>
                m.ExpertiseArea.ToLower().Contains(domain) ||
                m.Expertise.ToLower().Contains(domain) ||
                m.SkillsCsv.ToLower().Contains(domain));
        }

        if (filter.MinRating.HasValue)
            query = query.Where(m => m.AvgRating >= filter.MinRating.Value);

        if (filter.MaxPrice.HasValue)
            query = query.Where(m => (m.SessionPrice ?? m.HourlyRate) <= filter.MaxPrice.Value);

        if (filter.MinYearsOfExperience.HasValue)
            query = query.Where(m => m.YearsOfExperience >= filter.MinYearsOfExperience.Value);

        if (filter.FreeOnly == true)
            query = query.Where(m => (m.SessionPrice ?? m.HourlyRate) <= 0);

        if (filter.AvailableToday == true)
        {
            var today = DateTime.UtcNow.DayOfWeek.ToString();
            query = query.Where(m => m.AvailabilitySchedule != null && m.AvailabilitySchedule.Contains(today));
        }

        var mentors = await query
            .OrderByDescending(m => m.AvgRating)
            .ThenByDescending(m => m.YearsOfExperience)
            .ToListAsync();

        return mentors.Select(m => MapMentor(m, m.User)).ToList();
    }

    public async Task<List<MentorDto>> GetRecommendedMentorsAsync(string studentId, string? predictedPath)
    {
        var student = await _context.Users.FindAsync(studentId)
            ?? throw new InvalidOperationException("Student not found");

        var path = predictedPath?.Trim().ToLower();

        var mentorsQuery = _context.Mentors
            .Include(m => m.User)
            .Include(m => m.MentorshipProgressEntries)
            .Where(m => m.IsAvailable && m.IsApproved)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(path))
        {
            mentorsQuery = mentorsQuery.Where(m =>
                m.ExpertiseArea.ToLower().Contains(path) ||
                m.Expertise.ToLower().Contains(path) ||
                m.SkillsCsv.ToLower().Contains(path));
        }

        var mentors = await mentorsQuery
            .OrderByDescending(m => m.AvgRating)
            .ThenByDescending(m => m.YearsOfExperience)
            .Take(20)
            .ToListAsync();

        return mentors.Select(m => MapMentor(m, m.User)).ToList();
    }

    public async Task<MentorDto> GetMentorByIdAsync(string mentorId)
    {
        var mentor = await _context.Mentors
            .Include(m => m.User)
            .Include(m => m.MentorshipProgressEntries)
            .FirstOrDefaultAsync(m => m.Id == mentorId)
            ?? throw new InvalidOperationException("Mentor not found");

        return MapMentor(mentor, mentor.User);
    }

    public async Task<MentorRequestDto> CreateMentorRequestAsync(string studentId, CreateMentorRequestDto request)
    {
        var student = await _context.Users.FindAsync(studentId)
            ?? throw new InvalidOperationException("Student not found");

        var mentor = await _context.Mentors.FindAsync(request.MentorId)
            ?? throw new InvalidOperationException("Mentor not found");

        var mentorRequest = new MentorRequest
        {
            Id = Guid.NewGuid().ToString(),
            StudentId = studentId,
            MentorId = request.MentorId,
            Topic = request.Topic,
            Message = request.Message,
            Status = "Pending",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.MentorRequests.Add(mentorRequest);
        await _context.SaveChangesAsync();

        await CreateNotificationAsync(mentor.UserId, $"New mentorship request from {student.FullName} on '{request.Topic}'.");

        return new MentorRequestDto
        {
            Id = mentorRequest.Id,
            StudentId = mentorRequest.StudentId,
            StudentName = student.FullName,
            MentorId = mentorRequest.MentorId,
            MentorName = mentor.User?.FullName ?? "",
            Topic = mentorRequest.Topic,
            Status = mentorRequest.Status,
            CreatedAt = mentorRequest.CreatedAt
        };
    }

    public async Task<List<MentorRequestDto>> GetPendingRequestsAsync(string mentorUserId)
    {
        var mentor = await _context.Mentors
            .Include(m => m.User)
            .FirstOrDefaultAsync(m => m.UserId == mentorUserId)
            ?? throw new InvalidOperationException("Mentor profile not found");

        var mentorRequests = await _context.MentorRequests
            .Include(mr => mr.Student)
            .Include(mr => mr.Mentor)
                .ThenInclude(m => m!.User)
            .Where(mr => mr.MentorId == mentor.Id && mr.Status == "Pending")
            .OrderByDescending(mr => mr.CreatedAt)
            .ToListAsync();

        var mentorRequestDtos = mentorRequests.Select(mr => new MentorRequestDto
        {
            Id = mr.Id,
            StudentId = mr.StudentId,
            StudentName = mr.Student?.FullName ?? "Unknown",
            MentorId = mr.MentorId,
            MentorName = mr.Mentor?.User?.FullName ?? mentor.User?.FullName ?? "Unknown",
            Topic = mr.Topic,
            Status = mr.Status,
            ScheduledAt = mr.ScheduledAt,
            CreatedAt = mr.CreatedAt
        }).ToList();

        var enrollmentRequests = await _context.MentorshipProgressEntries
            .Include(mp => mp.Student)
            .Where(mp =>
                mp.MentorId == mentor.Id &&
                !mp.IsApproved &&
                (string.IsNullOrEmpty(mp.ApprovalStatus) || mp.ApprovalStatus == "Pending"))
            .OrderByDescending(mp => mp.UpdatedAt)
            .ToListAsync();

        var enrollmentRequestDtos = enrollmentRequests.Select(mp => new MentorRequestDto
        {
            Id = mp.Id,
            StudentId = mp.StudentId,
            StudentName = mp.Student?.FullName ?? "Unknown",
            MentorId = mp.MentorId,
            MentorName = mentor.User?.FullName ?? "Unknown",
            Topic = $"Enrollment Request ({mp.RoadmapStage})",
            Status = "Pending",
            ScheduledAt = null,
            CreatedAt = mp.UpdatedAt
        }).ToList();

        return mentorRequestDtos
            .Concat(enrollmentRequestDtos)
            .OrderByDescending(r => r.CreatedAt)
            .ToList();
    }

    public async Task<MentorRequestDto> UpdateRequestStatusAsync(string mentorUserId, string requestId, UpdateMentorRequestStatusDto request)
    {
        var mentor = await _context.Mentors
            .Include(m => m.User)
            .FirstOrDefaultAsync(m => m.UserId == mentorUserId)
            ?? throw new InvalidOperationException("Mentor profile not found");

        var mentorRequest = await _context.MentorRequests
            .Include(mr => mr.Mentor)
            .ThenInclude(m => m!.User)
            .Include(mr => mr.Student)
            .FirstOrDefaultAsync(mr => mr.Id == requestId && mr.MentorId == mentor.Id);

        if (mentorRequest == null)
        {
            var enrollmentRequest = await _context.MentorshipProgressEntries
                .Include(mp => mp.Student)
                .FirstOrDefaultAsync(mp => mp.Id == requestId && mp.MentorId == mentor.Id);

            if (enrollmentRequest == null)
                throw new InvalidOperationException("Request not found");

            if (!string.IsNullOrEmpty(enrollmentRequest.ApprovalStatus) && enrollmentRequest.ApprovalStatus != "Pending")
                throw new InvalidOperationException("Request has already been processed");

            var approved = string.Equals(request.Status, "Accepted", StringComparison.OrdinalIgnoreCase);
            enrollmentRequest.IsApproved = approved;
            enrollmentRequest.ApprovalStatus = approved ? "Approved" : "Rejected";
            enrollmentRequest.ApprovedAt = DateTime.UtcNow;
            enrollmentRequest.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            if (!string.IsNullOrEmpty(enrollmentRequest.StudentId))
            {
                await CreateNotificationAsync(
                    enrollmentRequest.StudentId,
                    $"Your enrollment request is now {enrollmentRequest.ApprovalStatus}.");
            }

            return new MentorRequestDto
            {
                Id = enrollmentRequest.Id,
                StudentId = enrollmentRequest.StudentId,
                StudentName = enrollmentRequest.Student?.FullName ?? "",
                MentorId = enrollmentRequest.MentorId,
                MentorName = mentor.User?.FullName ?? "",
                Topic = $"Enrollment Request ({enrollmentRequest.RoadmapStage})",
                Status = enrollmentRequest.ApprovalStatus,
                ScheduledAt = null,
                CreatedAt = enrollmentRequest.UpdatedAt
            };
        }

        if (mentorRequest.Status != "Pending")
            throw new InvalidOperationException("Request has already been processed");

        mentorRequest.Status = request.Status;
        mentorRequest.ScheduledAt = request.ScheduledAt;
        mentorRequest.UpdatedAt = DateTime.UtcNow;

        if (request.Status == "Accepted")
        {
            var existingEnrollment = await _context.MentorshipProgressEntries
                .FirstOrDefaultAsync(mp => mp.StudentId == mentorRequest.StudentId && mp.MentorId == mentor.Id);

            if (existingEnrollment == null)
            {
                var mentorshipProgress = new MentorshipProgress
                {
                    Id = Guid.NewGuid().ToString(),
                    StudentId = mentorRequest.StudentId,
                    MentorId = mentor.Id,
                    RoadmapStage = "Basics",
                    MentorFeedback = $"Welcome! I'm excited to mentor you in {mentor.Expertise}.",
                    CompletedTasks = "Mentor request accepted",
                    NextMilestone = "Complete first guided task",
                    UpdatedAt = DateTime.UtcNow,
                    IsApproved = true,
                    ApprovalStatus = "Approved",
                    ApprovedAt = DateTime.UtcNow,
                    PaymentStatus = "Unpaid"
                };

                _context.MentorshipProgressEntries.Add(mentorshipProgress);
            }
            else
            {
                existingEnrollment.IsApproved = true;
                existingEnrollment.ApprovalStatus = "Approved";
                existingEnrollment.ApprovedAt = DateTime.UtcNow;
                existingEnrollment.UpdatedAt = DateTime.UtcNow;
            }

            mentor.TotalSessionCount++;
            _context.Mentors.Update(mentor);
        }

        _context.MentorRequests.Update(mentorRequest);
        await _context.SaveChangesAsync();

        if (mentorRequest.StudentId != null)
        {
            await CreateNotificationAsync(
                mentorRequest.StudentId,
                $"Your mentor request for '{mentorRequest.Topic}' is now {mentorRequest.Status}.");
        }

        return new MentorRequestDto
        {
            Id = mentorRequest.Id,
            StudentId = mentorRequest.StudentId,
            StudentName = mentorRequest.Student?.FullName ?? "",
            MentorId = mentorRequest.MentorId,
            MentorName = mentorRequest.Mentor?.User?.FullName ?? "",
            Topic = mentorRequest.Topic,
            Status = mentorRequest.Status,
            ScheduledAt = mentorRequest.ScheduledAt,
            CreatedAt = mentorRequest.CreatedAt
        };
    }

    public async Task<MentorDto> ApplyAsMentorAsync(string userId, ApplyMentorDto request)
    {
        var user = await _context.Users.FindAsync(userId)
            ?? throw new InvalidOperationException("User not found");

        var existingMentor = await _context.Mentors.FirstOrDefaultAsync(m => m.UserId == userId);
        if (existingMentor != null)
        {
            throw new InvalidOperationException("User is already registered as a mentor.");
        }

        var mentor = new ApplicationMentor
        {
            Id = Guid.NewGuid().ToString(),
            UserId = userId,
            Expertise = request.Expertise,
            ExpertiseArea = request.ExpertiseArea ?? request.Expertise,
            CurrentCompany = request.CurrentCompany,
            Bio = request.Bio,
            SkillsCsv = string.Join(',', request.Skills.Where(s => !string.IsNullOrWhiteSpace(s)).Select(s => s.Trim())),
            LinkedInUrl = request.LinkedInUrl,
            AvailabilitySchedule = request.AvailabilitySchedule,
            YearsOfExperience = request.YearsOfExperience,
            HourlyRate = request.HourlyRate,
            SessionPrice = request.SessionPrice,
            IsAvailable = true,
            TotalSessionCount = 0,
            AvgRating = 0,
            IsApproved = false,
            CreatedAt = DateTime.UtcNow
        };

        user.Role = "Mentor";
        user.ProfileCompleted = true;
        user.UpdatedAt = DateTime.UtcNow;
        _context.Users.Update(user);

        _context.Mentors.Add(mentor);

        var initialSessionPrice = mentor.SessionPrice ?? mentor.HourlyRate;
        if (initialSessionPrice > 0)
        {
            _context.MentorSessionPriceHistory.Add(new MentorSessionPriceHistory
            {
                MentorId = mentor.Id,
                SessionPrice = initialSessionPrice,
                EffectiveFrom = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow
            });
        }

        await _context.SaveChangesAsync();

        return MapMentor(mentor, user);
    }

    public async Task<MentorSessionDto> BookSessionAsync(string studentId, BookSessionDto request)
    {
        var student = await _context.Users.FindAsync(studentId)
            ?? throw new InvalidOperationException("Student not found");

        var mentor = await _context.Mentors
            .Include(m => m.User)
            .FirstOrDefaultAsync(m => m.Id == request.MentorId)
            ?? throw new InvalidOperationException("Mentor not found");

        var session = new MentorSession
        {
            Id = Guid.NewGuid().ToString(),
            StudentId = studentId,
            MentorId = request.MentorId,
            ScheduledDateTime = request.ScheduledDateTime,
            DurationMinutes = request.DurationMinutes,
            Notes = request.Notes,
            Status = "Pending",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.MentorSessions.Add(session);
        await _context.SaveChangesAsync();

        // Notify mentor about new session request
        await CreateNotificationAsync(mentor.UserId, "Session", "New Session Request", $"{student.FullName} has requested a mentorship session.");

        return await MapSessionAsync(session.Id);
    }

    public async Task<MentorSessionDto> CreateSessionForStudentAsync(string mentorUserId, CreateMentorSessionDto request)
    {
        var mentor = await _context.Mentors
            .Include(m => m.User)
            .FirstOrDefaultAsync(m => m.UserId == mentorUserId)
            ?? throw new InvalidOperationException("Mentor profile not found");

        var student = await _context.Users.FindAsync(request.StudentId)
            ?? throw new InvalidOperationException("Student not found");

        var session = new MentorSession
        {
            Id = Guid.NewGuid().ToString(),
            StudentId = request.StudentId,
            MentorId = mentor.Id,
            ScheduledDateTime = request.ScheduledDateTime,
            DurationMinutes = request.DurationMinutes,
            Notes = request.Notes,
            MeetingLink = request.MeetingLink,
            Status = "Approved",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.MentorSessions.Add(session);
        await _context.SaveChangesAsync();

        await CreateNotificationAsync(student.Id, $"A new session has been scheduled by {mentor.User?.FullName ?? "your mentor"}.");

        return await MapSessionAsync(session.Id);
    }

    public async Task<MentorSessionDto> ApproveSessionAsync(string mentorUserId, ApproveSessionDto request)
    {
        var mentor = await _context.Mentors.FirstOrDefaultAsync(m => m.UserId == mentorUserId)
            ?? throw new InvalidOperationException("Mentor profile not found");

        var session = await _context.MentorSessions
            .FirstOrDefaultAsync(s => s.Id == request.SessionId && s.MentorId == mentor.Id)
            ?? throw new InvalidOperationException("Session not found");

        session.Status = request.Approve ? "Approved" : "Cancelled";
        session.MeetingLink = request.Approve ? request.MeetingLink : null;
        session.Notes = request.Notes ?? session.Notes;
        session.UpdatedAt = DateTime.UtcNow;

        _context.MentorSessions.Update(session);
        await _context.SaveChangesAsync();

        // Notify student about session approval/decline
        if (request.Approve)
        {
            await CreateNotificationAsync(session.StudentId, "Session", "Session Approved", "Your mentorship session has been approved and scheduled!");
        }
        else
        {
            await CreateNotificationAsync(session.StudentId, "Session", "Session Cancelled", "Your session request has been declined or cancelled.");
        }

        return await MapSessionAsync(session.Id);
    }

    public async Task<List<MentorSessionDto>> GetMySessionsAsync(string userId)
    {
        var mentor = await _context.Mentors.FirstOrDefaultAsync(m => m.UserId == userId);

        IQueryable<MentorSession> query = _context.MentorSessions;
        if (mentor != null)
        {
            query = query.Where(s => s.MentorId == mentor.Id);
        }
        else
        {
            query = query.Where(s => s.StudentId == userId);
        }

        var sessionIds = await query
            .OrderByDescending(s => s.ScheduledDateTime)
            .Select(s => s.Id)
            .ToListAsync();

        var mapped = new List<MentorSessionDto>();
        foreach (var sessionId in sessionIds)
        {
            mapped.Add(await MapSessionAsync(sessionId));
        }

        return mapped;
    }

    public async Task<List<MentorSessionDto>> GetStudentUpcomingSessionsAsync(string studentId)
    {
        var now = DateTime.UtcNow;
        
        // Get all mentors the student is enrolled with (approved enrollment)
        var enrolledMentorIds = await _context.MentorshipProgressEntries
            .Where(mp => mp.StudentId == studentId && mp.IsApproved)
            .Select(mp => mp.MentorId)
            .ToListAsync();

        if (!enrolledMentorIds.Any())
            return new List<MentorSessionDto>();

        // Get upcoming sessions with enrolled mentors only
        var sessionIds = await _context.MentorSessions
            .Where(s => s.StudentId == studentId 
                && s.ScheduledDateTime > now
                && enrolledMentorIds.Contains(s.MentorId)
                && s.Status != "Cancelled")
            .OrderBy(s => s.ScheduledDateTime)
            .Select(s => s.Id)
            .ToListAsync();

        var mapped = new List<MentorSessionDto>();
        foreach (var sessionId in sessionIds)
        {
            mapped.Add(await MapSessionAsync(sessionId));
        }

        return mapped;
    }

    public async Task<MentorSessionDto> GetSessionByIdAsync(string sessionId)
    {
        var session = await _context.MentorSessions
            .Include(s => s.Student)
            .Include(s => s.Mentor)
            .FirstOrDefaultAsync(s => s.Id == sessionId)
            ?? throw new InvalidOperationException("Session not found");

        return await MapSessionAsync(sessionId);
    }

    public async Task<MentorSessionDto> UpdateSessionAsync(string mentorUserId, string sessionId, UpdateSessionDto request)
    {
        var mentor = await _context.Mentors.FirstOrDefaultAsync(m => m.UserId == mentorUserId)
            ?? throw new InvalidOperationException("Mentor profile not found");

        var session = await _context.MentorSessions
            .FirstOrDefaultAsync(s => s.Id == sessionId && s.MentorId == mentor.Id)
            ?? throw new InvalidOperationException("Session not found or you don't have permission to edit");

        if (session.Status == "Completed" || session.Status == "Cancelled")
            throw new InvalidOperationException("Cannot update completed or cancelled sessions");

        if (request.ScheduledDateTime.HasValue)
            session.ScheduledDateTime = request.ScheduledDateTime.Value;

        if (request.DurationMinutes.HasValue)
            session.DurationMinutes = request.DurationMinutes.Value;

        if (!string.IsNullOrEmpty(request.Notes))
            session.Notes = request.Notes;

        session.UpdatedAt = DateTime.UtcNow;
        _context.MentorSessions.Update(session);
        await _context.SaveChangesAsync();

        await CreateNotificationAsync(session.StudentId, $"Your mentor has updated the session details");

        return await MapSessionAsync(sessionId);
    }

    public async Task<bool> DeleteSessionAsync(string mentorUserId, string sessionId)
    {
        var mentor = await _context.Mentors.FirstOrDefaultAsync(m => m.UserId == mentorUserId)
            ?? throw new InvalidOperationException("Mentor profile not found");

        var session = await _context.MentorSessions
            .FirstOrDefaultAsync(s => s.Id == sessionId && s.MentorId == mentor.Id)
            ?? throw new InvalidOperationException("Session not found or you don't have permission to delete");

        if (session.Status == "Completed")
            throw new InvalidOperationException("Cannot delete completed sessions");

        session.Status = "Cancelled";
        session.UpdatedAt = DateTime.UtcNow;
        _context.MentorSessions.Update(session);
        await _context.SaveChangesAsync();

        await CreateNotificationAsync(session.StudentId, $"Your session has been cancelled by the mentor");

        return true;
    }

    public async Task<SessionFeedbackDto> AddFeedbackAsync(string studentId, AddFeedbackDto request)
    {
        if (request.Rating < 1 || request.Rating > 5)
            throw new InvalidOperationException("Rating must be between 1 and 5");

        var session = await _context.MentorSessions
            .FirstOrDefaultAsync(s => s.Id == request.SessionId)
            ?? throw new InvalidOperationException("Session not found");

        if (session.StudentId != studentId)
            throw new InvalidOperationException("Only session student can add feedback");

        var existing = await _context.SessionFeedbacks.FirstOrDefaultAsync(sf => sf.SessionId == session.Id);
        if (existing != null)
            throw new InvalidOperationException("Feedback already submitted for this session");

        var feedback = new SessionFeedback
        {
            Id = Guid.NewGuid().ToString(),
            SessionId = session.Id,
            MentorId = session.MentorId,
            Rating = request.Rating,
            ReviewText = request.ReviewText,
            CreatedAt = DateTime.UtcNow
        };

        _context.SessionFeedbacks.Add(feedback);
        session.Status = "Completed";
        session.UpdatedAt = DateTime.UtcNow;
        _context.MentorSessions.Update(session);
        await _context.SaveChangesAsync();

        var mentor = await _context.Mentors.FirstOrDefaultAsync(m => m.Id == session.MentorId)
            ?? throw new InvalidOperationException("Mentor not found");

        var avgRating = await _context.SessionFeedbacks
            .Where(sf => sf.MentorId == mentor.Id)
            .AverageAsync(sf => (double?)sf.Rating) ?? 0;

        mentor.AvgRating = Math.Round(avgRating, 2);
        mentor.TotalSessionCount = await _context.MentorSessions.CountAsync(s => s.MentorId == mentor.Id && s.Status == "Completed");

        _context.Mentors.Update(mentor);
        await _context.SaveChangesAsync();

        await CreateNotificationAsync(mentor.UserId, "You received a new session feedback.");

        return new SessionFeedbackDto
        {
            Id = feedback.Id,
            SessionId = feedback.SessionId,
            MentorId = feedback.MentorId,
            Rating = feedback.Rating,
            ReviewText = feedback.ReviewText,
            CreatedAt = feedback.CreatedAt
        };
    }

    public async Task<List<SessionFeedbackSummaryDto>> GetMySessionFeedbackAsync(string studentId)
    {
        return await _context.SessionFeedbacks
            .Include(sf => sf.Session)
            .Where(sf => sf.Session != null && sf.Session.StudentId == studentId)
            .OrderByDescending(sf => sf.CreatedAt)
            .Select(sf => new SessionFeedbackSummaryDto
            {
                SessionId = sf.SessionId,
                Rating = sf.Rating,
                ReviewText = sf.ReviewText,
                CreatedAt = sf.CreatedAt
            })
            .ToListAsync();
    }

    public async Task<MentorshipProgressDto> UpsertMentorshipProgressAsync(string mentorUserId, UpsertMentorshipProgressDto request)
    {
        var mentor = await _context.Mentors.FirstOrDefaultAsync(m => m.UserId == mentorUserId)
            ?? throw new InvalidOperationException("Mentor profile not found");

        var student = await _context.Users.FindAsync(request.StudentId)
            ?? throw new InvalidOperationException("Student not found");

        var entry = await _context.MentorshipProgressEntries
            .FirstOrDefaultAsync(mp => mp.StudentId == request.StudentId && mp.RoadmapStage == request.RoadmapStage);

        if (entry == null)
        {
            entry = new MentorshipProgress
            {
                Id = Guid.NewGuid().ToString(),
                StudentId = request.StudentId,
                MentorId = mentor.Id,
                RoadmapStage = request.RoadmapStage
            };
            _context.MentorshipProgressEntries.Add(entry);
        }

        entry.MentorFeedback = request.MentorFeedback;
        entry.CompletedTasks = request.CompletedTasks;
        entry.NextMilestone = request.NextMilestone;
        entry.MentorId = mentor.Id;
        entry.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        await CreateNotificationAsync(student.Id, $"Mentor updated your roadmap stage '{request.RoadmapStage}'.");

        return new MentorshipProgressDto
        {
            Id = entry.Id,
            StudentId = entry.StudentId,
            MentorId = entry.MentorId,
            RoadmapStage = entry.RoadmapStage,
            MentorFeedback = entry.MentorFeedback,
            CompletedTasks = entry.CompletedTasks,
            NextMilestone = entry.NextMilestone,
            UpdatedAt = entry.UpdatedAt
        };
    }

    public async Task<List<NotificationDto>> GetMyNotificationsAsync(string userId)
    {
        return await _context.Notifications
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.CreatedAt)
            .Select(n => new NotificationDto
            {
                Id = n.Id,
                UserId = n.UserId,
                Type = n.Type,
                Title = n.Title,
                Message = n.Message,
                IsRead = n.IsRead,
                CreatedAt = n.CreatedAt
            })
            .ToListAsync();
    }

    public async Task MarkNotificationAsReadAsync(string userId, string notificationId)
    {
        var notification = await _context.Notifications
            .FirstOrDefaultAsync(n => n.Id == notificationId && n.UserId == userId)
            ?? throw new InvalidOperationException("Notification not found");

        notification.IsRead = true;
        await _context.SaveChangesAsync();
    }

    public async Task MarkAllNotificationsAsReadAsync(string userId)
    {
        var unreadNotifications = await _context.Notifications
            .Where(n => n.UserId == userId && !n.IsRead)
            .ToListAsync();

        if (unreadNotifications.Count == 0)
        {
            return;
        }

        foreach (var notification in unreadNotifications)
        {
            notification.IsRead = true;
        }

        await _context.SaveChangesAsync();
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

    private async Task<MentorSessionDto> MapSessionAsync(string sessionId)
    {
        var session = await _context.MentorSessions
            .Include(s => s.Student)
            .Include(s => s.Mentor)
                .ThenInclude(m => m!.User)
            .FirstOrDefaultAsync(s => s.Id == sessionId)
            ?? throw new InvalidOperationException("Session not found");

        var (sessionFee, isPaid, paymentStatus) = await GetSessionPaymentSummaryAsync(session);

        return new MentorSessionDto
        {
            Id = session.Id,
            StudentId = session.StudentId,
            StudentName = session.Student?.FullName ?? string.Empty,
            MentorId = session.MentorId,
            MentorName = session.Mentor?.User?.FullName ?? string.Empty,
            ScheduledDateTime = session.ScheduledDateTime,
            DurationMinutes = session.DurationMinutes,
            Status = session.Status,
            MeetingLink = session.MeetingLink,
            Notes = session.Notes,
            SessionFee = sessionFee,
            IsPaid = isPaid,
            PaymentStatus = paymentStatus,
            CreatedAt = session.CreatedAt
        };
    }

    private async Task<(decimal sessionFee, bool isPaid, string paymentStatus)> GetSessionPaymentSummaryAsync(MentorSession session)
    {
        var mentor = session.Mentor ?? await _context.Mentors.FirstOrDefaultAsync(m => m.Id == session.MentorId);
        var defaultSessionPrice = mentor?.SessionPrice ?? mentor?.HourlyRate ?? 500;

        var enrollment = await _context.MentorshipProgressEntries
            .FirstOrDefaultAsync(mp => mp.StudentId == session.StudentId && mp.MentorId == session.MentorId && mp.IsApproved);

        if (enrollment == null)
        {
            var fallbackFee = defaultSessionPrice <= 0 ? 500 : defaultSessionPrice;
            return (fallbackFee, false, "Unpaid");
        }

        var priceHistory = await _context.MentorSessionPriceHistory
            .Where(h => h.MentorId == session.MentorId)
            .OrderBy(h => h.EffectiveFrom)
            .ToListAsync();

        var pairSessions = await _context.MentorSessions
            .Where(s => s.StudentId == session.StudentId
                        && s.MentorId == session.MentorId
                        && s.Status != "Cancelled")
            .OrderBy(s => s.ScheduledDateTime)
            .ThenBy(s => s.CreatedAt)
            .ToListAsync();

        if (!pairSessions.Any())
        {
            var fallbackFee = ResolveSessionFee(defaultSessionPrice, session.ScheduledDateTime, priceHistory);
            return (fallbackFee, false, "Unpaid");
        }

        var totalPaidAmount = await _context.PaymentTransactions
            .Where(pt => pt.MentorshipProgressId == enrollment.Id && pt.Status == "Completed")
            .SumAsync(pt => (decimal?)pt.Amount) ?? 0;

        var cumulativeFeeTillTarget = 0m;
        var targetSessionFee = ResolveSessionFee(defaultSessionPrice, session.ScheduledDateTime, priceHistory);

        foreach (var pairSession in pairSessions)
        {
            var fee = ResolveSessionFee(defaultSessionPrice, pairSession.ScheduledDateTime, priceHistory);
            cumulativeFeeTillTarget += fee;

            if (pairSession.Id == session.Id)
            {
                targetSessionFee = fee;
                break;
            }
        }

        var paid = totalPaidAmount >= cumulativeFeeTillTarget;
        return (targetSessionFee, paid, paid ? "Paid" : "Unpaid");
    }

    private static decimal ResolveSessionFee(decimal defaultSessionPrice, DateTime scheduledAt, List<MentorSessionPriceHistory> priceHistory)
    {
        if (priceHistory.Count == 0)
        {
            return defaultSessionPrice <= 0 ? 500 : defaultSessionPrice;
        }

        var effectivePrice = priceHistory
            .Where(h => h.EffectiveFrom <= scheduledAt)
            .OrderByDescending(h => h.EffectiveFrom)
            .Select(h => h.SessionPrice)
            .FirstOrDefault();

        if (effectivePrice > 0)
        {
            return effectivePrice;
        }

        var earliestPrice = priceHistory.OrderBy(h => h.EffectiveFrom).Select(h => h.SessionPrice).FirstOrDefault();
        if (earliestPrice > 0)
        {
            return earliestPrice;
        }

        return defaultSessionPrice <= 0 ? 500 : defaultSessionPrice;
    }

    private MentorDto MapMentor(ApplicationMentor mentor, ApplicationUser? user)
    {
        var skills = string.IsNullOrWhiteSpace(mentor.SkillsCsv)
            ? new List<string>()
            : mentor.SkillsCsv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();

        // Calculate total unique students assigned to this mentor
        var totalStudentsAssigned = mentor.MentorshipProgressEntries
            ?.Where(mp => mp.IsApproved && mp.ApprovalStatus == "Approved")
            .Select(mp => mp.StudentId)
            .Distinct()
            .Count() ?? 0;

        return new MentorDto
        {
            Id = mentor.Id,
            UserId = mentor.UserId,
            UserName = user?.FullName ?? string.Empty,
            ProfileImageUrl = user?.ProfileImageUrl,
            Expertise = mentor.Expertise,
            ExpertiseArea = string.IsNullOrWhiteSpace(mentor.ExpertiseArea) ? mentor.Expertise : mentor.ExpertiseArea,
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
            TotalStudentsAssigned = totalStudentsAssigned
        };
    }

    private async Task CreateNotificationAsync(string userId, string message)
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

    public async Task<MentorshipProgressDto> EnrollStudentWithMentorAsync(string studentId, string mentorId)
    {
        var student = await _context.Users.FindAsync(studentId)
            ?? throw new InvalidOperationException("Student not found");

        var mentor = await _context.Mentors
            .Include(m => m.User)
            .FirstOrDefaultAsync(m => m.Id == mentorId)
            ?? throw new InvalidOperationException("Mentor not found");

        // Check if already enrolled
        var existingEnrollment = await _context.MentorshipProgressEntries
            .FirstOrDefaultAsync(mp => mp.StudentId == studentId && mp.MentorId == mentorId);

        if (existingEnrollment != null)
            throw new InvalidOperationException("Student is already enrolled with this mentor");

        var mentorshipProgress = new MentorshipProgress
        {
            Id = Guid.NewGuid().ToString(),
            StudentId = studentId,
            MentorId = mentorId,
            RoadmapStage = "Basics",
            MentorFeedback = $"Welcome! I'm excited to mentor you in {mentor.Expertise}.",
            CompletedTasks = "Enrollment completed",
            NextMilestone = "Complete first task",
            UpdatedAt = DateTime.UtcNow
        };

        _context.MentorshipProgressEntries.Add(mentorshipProgress);
        await _context.SaveChangesAsync();

        await CreateNotificationAsync(mentor.UserId, $"New enrollment request from {student.FullName}.");

        return new MentorshipProgressDto
        {
            Id = mentorshipProgress.Id,
            StudentId = mentorshipProgress.StudentId,
            MentorId = mentorshipProgress.MentorId,
            RoadmapStage = mentorshipProgress.RoadmapStage,
            MentorFeedback = mentorshipProgress.MentorFeedback,
            CompletedTasks = mentorshipProgress.CompletedTasks,
            NextMilestone = mentorshipProgress.NextMilestone,
            UpdatedAt = mentorshipProgress.UpdatedAt
        };
    }

    public async Task<List<MentorDto>> GetEnrolledMentorsAsync(string studentId)
    {
        var enrolledMentors = await _context.MentorshipProgressEntries
            .Where(mp => mp.StudentId == studentId && mp.IsApproved)
            .Include(mp => mp.Mentor)
                .ThenInclude(m => m.User)
            .Include(mp => mp.Mentor)
                .ThenInclude(m => m.MentorshipProgressEntries)
            .Select(mp => mp.Mentor)
            .Distinct()
            .ToListAsync();

        return enrolledMentors.Select(mentor => MapMentor(mentor, mentor.User)).ToList();
    }

    public async Task<MentorshipProgressDto?> IsStudentEnrolledAsync(string studentId, string mentorId)
    {
        var enrollment = await _context.MentorshipProgressEntries
            .Where(mp => mp.StudentId == studentId && mp.MentorId == mentorId)
            .OrderByDescending(mp => mp.UpdatedAt)
            .FirstOrDefaultAsync();

        if (enrollment == null)
            return null;

        return new MentorshipProgressDto
        {
            Id = enrollment.Id,
            StudentId = enrollment.StudentId,
            MentorId = enrollment.MentorId,
            RoadmapStage = enrollment.RoadmapStage,
            MentorFeedback = enrollment.MentorFeedback,
            CompletedTasks = enrollment.CompletedTasks,
            NextMilestone = enrollment.NextMilestone,
            UpdatedAt = enrollment.UpdatedAt,
            IsApproved = enrollment.IsApproved,
            ApprovedAt = enrollment.ApprovedAt,
            ApprovalStatus = string.IsNullOrWhiteSpace(enrollment.ApprovalStatus) ? "Pending" : enrollment.ApprovalStatus,
            IsPaid = enrollment.IsPaid,
            PaidAt = enrollment.PaidAt,
            Amount = enrollment.Amount,
            PaymentMethod = enrollment.PaymentMethod,
            PaymentStatus = string.IsNullOrWhiteSpace(enrollment.PaymentStatus) ? "Unpaid" : enrollment.PaymentStatus
        };
    }

    public async Task<MentorDashboardDto> GetMentorDashboardAsync(string mentorId)
    {
        var mentor = await _context.Mentors
            .Include(m => m.User)
            .Include(m => m.MentorshipProgressEntries)
            .Include(m => m.Sessions)
            .FirstOrDefaultAsync(m => m.UserId == mentorId)
            ?? throw new InvalidOperationException("Mentor not found");

        // Get all students this mentor is assigned to
        var assignedStudents = await _context.MentorshipProgressEntries
            .Where(mp => mp.MentorId == mentor.Id && mp.IsApproved && mp.ApprovalStatus == "Approved")
            .Select(mp => new { mp.Student, mp.RoadmapStage, mp.UpdatedAt })
            .Distinct()
            .ToListAsync();

        var pendingMentorRequests = await _context.MentorRequests
            .CountAsync(mr => mr.MentorId == mentor.Id && mr.Status == "Pending");

        var pendingEnrollmentRequests = await _context.MentorshipProgressEntries
            .CountAsync(mp =>
                mp.MentorId == mentor.Id &&
                !mp.IsApproved &&
                (string.IsNullOrEmpty(mp.ApprovalStatus) || mp.ApprovalStatus == "Pending"));

        var pendingRequests = pendingMentorRequests + pendingEnrollmentRequests;

        // Get feedback for this mentor
        var feedback = await _context.SessionFeedbacks
            .Where(f => f.MentorId == mentor.Id)
            .Include(f => f.Session)
            .ThenInclude(s => s!.Student)
            .OrderByDescending(f => f.CreatedAt)
            .Take(10)
            .ToListAsync();

        // Get upcoming sessions
        var upcomingSessions = await _context.MentorSessions
            .Where(s => s.MentorId == mentor.Id && s.ScheduledDateTime > DateTime.UtcNow)
            .Include(s => s.Student)
            .OrderBy(s => s.ScheduledDateTime)
            .Take(10)
            .ToListAsync();

        // Get mentorship progress entries
        var progressEntries = await _context.MentorshipProgressEntries
            .Where(mp => mp.MentorId == mentor.Id && mp.IsApproved && mp.ApprovalStatus == "Approved")
            .Include(mp => mp.Student)
            .ToListAsync();

        var now = DateTime.UtcNow;
        var monthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        var studentIds = progressEntries
            .Select(p => p.StudentId)
            .Distinct()
            .ToList();

        var studentUsers = studentIds.Count > 0
            ? await _context.Users
                .Where(u => studentIds.Contains(u.Id))
                .ToDictionaryAsync(u => u.Id)
            : new Dictionary<string, ApplicationUser>();

        var userProgressList = studentIds.Count > 0
            ? await _context.UserProgressions
                .Where(up => studentIds.Contains(up.UserId))
                .ToListAsync()
            : new List<UserProgress>();

        var completedModuleProgress = studentIds.Count > 0
            ? await _context.UserModuleProgressions
                .Include(ump => ump.Module)
                .Where(ump => studentIds.Contains(ump.UserId) && ump.IsCompleted)
                .ToListAsync()
            : new List<UserModuleProgress>();

        var completedUserSkills = studentIds.Count > 0
            ? await _context.UserSkills
                .Where(us => studentIds.Contains(us.UserId) && us.IsCompleted)
                .ToListAsync()
            : new List<UserSkill>();

        var userBadges = studentIds.Count > 0
            ? await _context.UserBadges
                .Where(ub => studentIds.Contains(ub.UserId))
                .ToListAsync()
            : new List<UserBadge>();

        var latestQuizResults = studentIds.Count > 0
            ? await _context.QuizResults
                .Where(qr => studentIds.Contains(qr.UserId))
                .OrderByDescending(qr => qr.CompletedAt)
                .ToListAsync()
            : new List<QuizResult>();

        var mentorPaymentTransactions = await _context.PaymentTransactions
            .Include(pt => pt.Student)
            .Include(pt => pt.MentorshipProgress)
            .Where(pt => pt.MentorshipProgress != null && pt.MentorshipProgress.MentorId == mentor.Id)
            .OrderByDescending(pt => pt.CompletedAt ?? pt.CreatedAt)
            .ToListAsync();

        var completedPaymentTransactions = mentorPaymentTransactions
            .Where(pt => string.Equals(pt.Status, "Completed", StringComparison.OrdinalIgnoreCase))
            .ToList();

        var avgProgressByStudent = userProgressList
            .GroupBy(up => up.UserId)
            .ToDictionary(g => g.Key, g => (int)Math.Round(g.Average(x => x.ProgressPercentage)));

        var domainCountByStudent = userProgressList
            .GroupBy(up => up.UserId)
            .ToDictionary(g => g.Key, g => g.Select(x => x.CareerDomainId).Distinct().Count());

        var hoursByStudent = completedModuleProgress
            .GroupBy(ump => ump.UserId)
            .ToDictionary(g => g.Key, g => g.Sum(x => x.Module?.EstimatedHours ?? 0));

        var skillsCompletedByStudent = completedUserSkills
            .GroupBy(us => us.UserId)
            .ToDictionary(g => g.Key, g => g.Count());

        var latestQuizByStudent = latestQuizResults
            .GroupBy(qr => qr.UserId)
            .ToDictionary(g => g.Key, g => g.First());

        var quizScoreByStudent = latestQuizByStudent
            .ToDictionary(kvp => kvp.Key, kvp => CalculateQuizPerformanceFromScoresJson(kvp.Value.ScoresJson));

        // Calculate stats
        var totalStudents = studentIds.Count;
        var allMentorSessions = await _context.MentorSessions
            .Where(s => s.MentorId == mentor.Id)
            .ToListAsync();

        var completedSessionsCount = allMentorSessions
            .Count(s => string.Equals(s.Status, "Completed", StringComparison.OrdinalIgnoreCase));

        var totalSessions = allMentorSessions.Count;
        var totalFeedback = feedback.Count;
        var avgRating = feedback.Any() ? feedback.Average(f => f.Rating) : 0;

        var completionRate = totalStudents > 0
            ? (int)Math.Round((double)userProgressList.Count(up => up.ProgressPercentage >= 100) / totalStudents * 100)
            : 0;

        var sessionAttendanceRate = totalSessions > 0
            ? Math.Round((double)allMentorSessions.Count(s => !string.Equals(s.Status, "Cancelled", StringComparison.OrdinalIgnoreCase)) / totalSessions * 100, 1)
            : 0;

        var satisfactionRate = Math.Round((avgRating / 5.0) * 100, 1);

        var studentsReachedLevel10 = studentUsers.Values.Count(s => s.Level >= 10);
        var skillsMasteredByStudents = completedUserSkills.Count;
        var careerPathCompletions = userProgressList
            .Where(up => up.ProgressPercentage >= 100)
            .Select(up => up.UserId)
            .Distinct()
            .Count();

        var badgesEarnedByStudents = userBadges.Count;
        var avgStudentLevel = totalStudents > 0
            ? Math.Round(studentUsers.Values.Average(s => s.Level), 1)
            : 0;

        var avgQuizPerformance = quizScoreByStudent.Any()
            ? (int)Math.Round(quizScoreByStudent.Values.Average())
            : 0;

        var avgHoursPerStudent = totalStudents > 0
            ? (int)Math.Round((double)hoursByStudent.Values.Sum() / totalStudents)
            : 0;

        var courseCompletionRate = totalStudents > 0
            ? (int)Math.Round((double)studentIds.Sum(id => avgProgressByStudent.TryGetValue(id, out var progress) ? progress : 0) / totalStudents)
            : 0;

        var retainedStudents = studentUsers.Values.Count(s => s.UpdatedAt >= now.AddDays(-30));
        var studentRetention = totalStudents > 0
            ? (int)Math.Round((double)retainedStudents / totalStudents * 100)
            : 0;

        var studentsWithSkillUsage = skillsCompletedByStudent.Count;
        var skillRecommendationsUsed = totalStudents > 0
            ? (int)Math.Round((double)studentsWithSkillUsage / totalStudents * 100)
            : 0;

        var thisMonthGrowth = totalStudents > 0
            ? (int)Math.Round((double)userProgressList.Count(up => up.UpdatedAt >= monthStart) / totalStudents * 100)
            : 0;

        var careerPathTransitions = domainCountByStudent.Values.Sum(count => Math.Max(0, count - 1));

        var stats = new MentorDashboardStatsDto
        {
            TotalStudents = totalStudents,
            TotalEarnings = completedPaymentTransactions.Sum(pt => pt.Amount),
            ThisMonthEarnings = completedPaymentTransactions
                .Where(pt =>
                {
                    var paidAt = pt.CompletedAt ?? pt.CreatedAt;
                    return paidAt >= monthStart && paidAt <= now;
                })
                .Sum(pt => pt.Amount),
            AvgRating = Math.Round(avgRating, 1),
            TotalSessions = totalSessions,
            TotalFeedback = totalFeedback,
            CompletionRate = completionRate,
            PendingRequests = pendingRequests,
            SessionAttendanceRate = sessionAttendanceRate,
            SatisfactionRate = satisfactionRate,
            AvgQuizPerformance = avgQuizPerformance,
            AvgHoursPerStudent = avgHoursPerStudent,
            CourseCompletionRate = courseCompletionRate,
            StudentRetention = studentRetention,
            SkillRecommendationsUsed = skillRecommendationsUsed,
            ThisMonthGrowth = thisMonthGrowth,
            CareerPathTransitions = careerPathTransitions,
            StudentsReachedLevel10 = studentsReachedLevel10,
            SkillsMasteredByStudents = skillsMasteredByStudents,
            CareerPathCompletions = careerPathCompletions,
            BadgesEarnedByStudents = badgesEarnedByStudents,
            AvgStudentLevel = avgStudentLevel
        };

        // Map student progress data
        var studentProgressList = new List<StudentProgressDto>();
        foreach (var studentEntry in assignedStudents)
        {
            if (studentEntry.Student == null)
            {
                continue;
            }

            var studentId = studentEntry.Student.Id;
            var studentProfile = studentUsers.TryGetValue(studentId, out var profile)
                ? profile
                : studentEntry.Student;

            var studentProgress = avgProgressByStudent.TryGetValue(studentId, out var progressValue) ? progressValue : 0;
            var quizScore = quizScoreByStudent.TryGetValue(studentId, out var quizValue) ? quizValue : 0;
            var hoursSpent = hoursByStudent.TryGetValue(studentId, out var hoursValue) ? hoursValue : 0;
            var isActiveStudent = studentProfile.UpdatedAt >= now.AddDays(-14);

            studentProgressList.Add(new StudentProgressDto
            {
                Id = studentId,
                Name = studentProfile.FullName ?? "Unknown",
                Email = studentProfile.Email ?? string.Empty,
                CurrentLevel = studentProfile.Level,
                Progress = studentProgress,
                RoadmapStage = studentEntry.RoadmapStage,
                LastActive = FormatRelativeTime(studentProfile.UpdatedAt),
                QuizScore = quizScore,
                HoursSpent = hoursSpent,
                Status = isActiveStudent ? "Active" : "Inactive"
            });
        }

        // Map feedback data
        var feedbackList = feedback.Select(f => new FeedbackSummaryDto
        {
            Id = f.Id,
            StudentName = f.Session?.Student?.FullName ?? "Student",
            Rating = f.Rating,
            Comment = f.ReviewText,
            Date = FormatRelativeTime(f.CreatedAt)
        }).ToList();

        // Map sessions data
        var sessionsList = new List<MentorSessionDto>();
        foreach (var s in upcomingSessions)
        {
            var (sessionFee, isPaid, paymentStatus) = await GetSessionPaymentSummaryAsync(s);
            sessionsList.Add(new MentorSessionDto
            {
                Id = s.Id,
                StudentId = s.StudentId,
                StudentName = s.Student?.FullName ?? "Unknown",
                MentorId = s.MentorId,
                MentorName = mentor.User?.FullName ?? "Unknown",
                ScheduledDateTime = s.ScheduledDateTime,
                DurationMinutes = s.DurationMinutes,
                Status = s.Status ?? "Pending",
                MeetingLink = s.MeetingLink,
                Notes = s.Notes,
                SessionFee = sessionFee,
                IsPaid = isPaid,
                PaymentStatus = paymentStatus,
                CreatedAt = s.CreatedAt
            });
        }

        // Map mentorship progress
        var progressList = progressEntries.Select(p => new MentorshipProgressDto
        {
            Id = p.Id,
            StudentId = p.StudentId,
            MentorId = p.MentorId,
            RoadmapStage = p.RoadmapStage,
            MentorFeedback = p.MentorFeedback,
            CompletedTasks = p.CompletedTasks,
            NextMilestone = p.NextMilestone,
            UpdatedAt = p.UpdatedAt,
            IsApproved = p.IsApproved,
            ApprovedAt = p.ApprovedAt,
            ApprovalStatus = string.IsNullOrWhiteSpace(p.ApprovalStatus) ? "Pending" : p.ApprovalStatus,
            IsPaid = p.IsPaid,
            PaidAt = p.PaidAt,
            Amount = p.Amount,
            PaymentMethod = p.PaymentMethod,
            PaymentStatus = string.IsNullOrWhiteSpace(p.PaymentStatus) ? "Unpaid" : p.PaymentStatus
        }).ToList();

        var mentorSessionPrice = mentor.SessionPrice ?? mentor.HourlyRate;
        var paymentStatementList = mentorPaymentTransactions.Select(pt =>
        {
            var sessionsCovered = mentorSessionPrice > 0 ? decimal.Round(pt.Amount / mentorSessionPrice, 2) : 0;
            return new MentorPaymentStatementDto
            {
                TransactionId = pt.Id,
                StudentId = pt.StudentId,
                StudentName = pt.Student?.FullName ?? "Unknown",
                Amount = pt.Amount,
                SessionPrice = mentorSessionPrice,
                SessionsCovered = sessionsCovered,
                PaymentMethod = pt.PaymentMethod,
                Status = pt.Status,
                CompletedAt = pt.CompletedAt,
                CreatedAt = pt.CreatedAt
            };
        }).ToList();

        return new MentorDashboardDto
        {
            Stats = stats,
            AssignedStudents = studentProgressList,
            UpcomingSessions = sessionsList,
            RecentFeedback = feedbackList,
            MentorshipProgresses = progressList,
            PaymentStatements = paymentStatementList
        };
    }

    private string FormatRelativeTime(DateTime dateTime)
    {
        var timeSpan = DateTime.UtcNow - dateTime;

        if (timeSpan.TotalSeconds < 60)
            return "just now";
        if (timeSpan.TotalMinutes < 60)
            return $"{(int)timeSpan.TotalMinutes} minutes ago";
        if (timeSpan.TotalHours < 24)
            return $"{(int)timeSpan.TotalHours} hours ago";
        if (timeSpan.TotalDays < 7)
            return $"{(int)timeSpan.TotalDays} days ago";
        if (timeSpan.TotalDays < 30)
            return $"{(int)(timeSpan.TotalDays / 7)} weeks ago";
        if (timeSpan.TotalDays < 365)
            return $"{(int)(timeSpan.TotalDays / 30)} months ago";

        return $"{(int)(timeSpan.TotalDays / 365)} years ago";
    }

    private static int CalculateQuizPerformanceFromScoresJson(string? scoresJson)
    {
        if (string.IsNullOrWhiteSpace(scoresJson))
        {
            return 0;
        }

        try
        {
            var scores = JsonSerializer.Deserialize<Dictionary<string, int>>(scoresJson);
            if (scores == null || scores.Count == 0)
            {
                return 0;
            }

            var total = scores.Values.Sum();
            var max = scores.Values.Max();

            if (total <= 0)
            {
                return 0;
            }

            return (int)Math.Round((double)max / total * 100);
        }
        catch
        {
            return 0;
        }
    }

    public async Task<EnrollmentApprovalDto> ApproveEnrollmentAsync(string mentorId, ApproveEnrollmentDto request)
    {
        var enrollment = await _context.MentorshipProgressEntries
            .FirstOrDefaultAsync(mp => mp.Id == request.EnrollmentId && mp.MentorId == mentorId)
            ?? throw new InvalidOperationException("Enrollment not found");

        enrollment.IsApproved = request.IsApproved;
        enrollment.ApprovalStatus = request.IsApproved ? "Approved" : "Rejected";
        enrollment.ApprovedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        // Notify student about enrollment decision
        if (request.IsApproved)
        {
            await CreateNotificationAsync(enrollment.StudentId, "Enrollment", "Enrollment Approved", "Your enrollment request has been approved! You can now book sessions.");
        }
        else
        {
            await CreateNotificationAsync(enrollment.StudentId, "Enrollment", "Enrollment Rejected", "Your enrollment request was not approved at this time.");
        }

        return new EnrollmentApprovalDto
        {
            EnrollmentId = enrollment.Id,
            IsApproved = enrollment.IsApproved,
            ApprovalStatus = enrollment.ApprovalStatus,
            ApprovedAt = enrollment.ApprovedAt.Value
        };
    }

    public async Task<PaymentResponseDto> ProcessPaymentAsync(string studentId, PaymentRequestDto request)
    {
        var enrollment = await _context.MentorshipProgressEntries
            .Include(mp => mp.Mentor)
            .FirstOrDefaultAsync(mp => mp.Id == request.EnrollmentId && mp.StudentId == studentId)
            ?? throw new InvalidOperationException("Enrollment not found");

        if (!enrollment.IsApproved)
            throw new InvalidOperationException("Enrollment must be approved first");

        // Get student wallet
        var wallet = await _context.StudentWallets
            .FirstOrDefaultAsync(sw => sw.StudentId == studentId);
        
        if (wallet == null)
        {
            wallet = new StudentWallet { StudentId = studentId, Balance = 1000 };
            _context.StudentWallets.Add(wallet);
            await _context.SaveChangesAsync();
        }

        if (string.IsNullOrWhiteSpace(request.SessionId))
            throw new InvalidOperationException("Session ID is required for payment");

        var targetSession = await _context.MentorSessions
            .FirstOrDefaultAsync(s => s.Id == request.SessionId && s.StudentId == studentId && s.MentorId == enrollment.MentorId)
            ?? throw new InvalidOperationException("Session not found for this enrollment");

        if (targetSession.Status == "Cancelled")
            throw new InvalidOperationException("Cancelled session cannot be paid");

        var priceHistory = await _context.MentorSessionPriceHistory
            .Where(h => h.MentorId == enrollment.MentorId)
            .OrderBy(h => h.EffectiveFrom)
            .ToListAsync();

        var defaultSessionPrice = enrollment.Mentor?.SessionPrice ?? enrollment.Mentor?.HourlyRate ?? 500;
        var sessionsForEnrollment = await _context.MentorSessions
            .Where(s => s.StudentId == studentId
                        && s.MentorId == enrollment.MentorId
                        && s.Status != "Cancelled")
            .OrderBy(s => s.ScheduledDateTime)
            .ThenBy(s => s.CreatedAt)
            .ToListAsync();

        if (!sessionsForEnrollment.Any(s => s.Id == targetSession.Id))
            throw new InvalidOperationException("Session is not eligible for payment");

        var totalPaidBefore = await _context.PaymentTransactions
            .Where(pt => pt.MentorshipProgressId == enrollment.Id && pt.Status == "Completed")
            .SumAsync(pt => (decimal?)pt.Amount) ?? 0;

        var cumulativeFeeTillTarget = 0m;
        decimal sessionFee = ResolveSessionFee(defaultSessionPrice, targetSession.ScheduledDateTime, priceHistory);

        foreach (var session in sessionsForEnrollment)
        {
            var fee = ResolveSessionFee(defaultSessionPrice, session.ScheduledDateTime, priceHistory);
            cumulativeFeeTillTarget += fee;

            if (session.Id == targetSession.Id)
            {
                sessionFee = fee;
                break;
            }
        }

        if (totalPaidBefore >= cumulativeFeeTillTarget)
            throw new InvalidOperationException("This session has already been paid");

        var amount = sessionFee;

        // Check if student has sufficient balance
        if (wallet.Balance < amount)
            throw new InvalidOperationException("Insufficient wallet balance");

        // Deduct amount from wallet
        wallet.Balance -= amount;
        wallet.UpdatedAt = DateTime.UtcNow;

        // Create payment transaction
        var transaction = new PaymentTransaction
        {
            StudentId = studentId,
            MentorshipProgressId = enrollment.Id,
            SessionId = targetSession.Id,
            Amount = amount,
            PaymentMethod = request.PaymentMethod,
            Status = "Completed",
            CompletedAt = DateTime.UtcNow
        };

        var totalPaidAfter = totalPaidBefore + amount;
        var totalSessionFee = sessionsForEnrollment
            .Sum(s => ResolveSessionFee(defaultSessionPrice, s.ScheduledDateTime, priceHistory));

        // Update enrollment aggregate payment status
        enrollment.IsPaid = totalSessionFee > 0 && totalPaidAfter >= totalSessionFee;
        enrollment.PaidAt = DateTime.UtcNow;
        enrollment.Amount = totalPaidAfter;
        enrollment.PaymentMethod = request.PaymentMethod;
        enrollment.PaymentStatus = enrollment.IsPaid ? "Completed" : "Partial";

        _context.PaymentTransactions.Add(transaction);
        await _context.SaveChangesAsync();

        return new PaymentResponseDto
        {
            TransactionId = transaction.Id,
            Status = "Completed",
            Amount = amount,
            PaymentMethod = request.PaymentMethod,
            CreatedAt = transaction.CreatedAt
        };
    }

    public async Task<StudentWalletDto> GetStudentWalletAsync(string studentId)
    {
        var wallet = await _context.StudentWallets
            .FirstOrDefaultAsync(sw => sw.StudentId == studentId);

        if (wallet == null)
        {
            // Create wallet with default balance
            wallet = new StudentWallet { StudentId = studentId, Balance = 1000 };
            _context.StudentWallets.Add(wallet);
            await _context.SaveChangesAsync();
        }

        return new StudentWalletDto
        {
            Id = wallet.Id,
            StudentId = wallet.StudentId,
            Balance = wallet.Balance,
            UpdatedAt = wallet.UpdatedAt
        };
    }
}
