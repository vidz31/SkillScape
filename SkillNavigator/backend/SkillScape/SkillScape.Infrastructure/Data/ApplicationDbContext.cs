using Microsoft.EntityFrameworkCore;
using SkillScape.Domain.Entities;

namespace SkillScape.Infrastructure.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    // DbSets
    public DbSet<ApplicationUser> Users { get; set; } = null!;
    public DbSet<CareerDomain> CareerDomains { get; set; } = null!;
    public DbSet<Skill> Skills { get; set; } = null!;
    public DbSet<UserSkill> UserSkills { get; set; }
    public DbSet<UserProgress> UserProgressions { get; set; }
    public DbSet<ChatMessage> ChatMessages { get; set; } = null!;
    public DbSet<QuizQuestion> QuizQuestions { get; set; } = null!;
    public DbSet<QuizOption> QuizOptions { get; set; } = null!;
    public DbSet<QuizResponse> QuizResponses { get; set; } = null!;
    public DbSet<QuizResult> QuizResults { get; set; } = null!;
    public DbSet<Badge> Badges { get; set; } = null!;
    public DbSet<UserBadge> UserBadges { get; set; } = null!;
    public DbSet<RoadmapStep> RoadmapSteps { get; set; } = null!;
    public DbSet<RoadmapTopic> RoadmapTopics { get; set; } = null!;
    public DbSet<UserModuleProgress> UserModuleProgressions { get; set; } = null!;
    public DbSet<ApplicationMentor> Mentors { get; set; } = null!;
    public DbSet<MentorRequest> MentorRequests { get; set; } = null!;
    public DbSet<MentorSession> MentorSessions { get; set; } = null!;
    public DbSet<SessionFeedback> SessionFeedbacks { get; set; } = null!;
    public DbSet<MentorshipProgress> MentorshipProgressEntries { get; set; } = null!;
    public DbSet<Notification> Notifications { get; set; } = null!;
    public DbSet<SessionComplaint> SessionComplaints { get; set; } = null!;
    public DbSet<AdminAuditLog> AdminAuditLogs { get; set; } = null!;
    public DbSet<StudentWallet> StudentWallets { get; set; } = null!;
    public DbSet<PaymentTransaction> PaymentTransactions { get; set; } = null!;
    public DbSet<MentorSessionPriceHistory> MentorSessionPriceHistory { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ApplicationUser
        modelBuilder.Entity<ApplicationUser>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Email).IsRequired().HasMaxLength(256);
            entity.Property(e => e.FullName).IsRequired().HasMaxLength(256);
            entity.Property(e => e.Role).IsRequired().HasMaxLength(50);
            entity.Property(e => e.ProfileCompleted).IsRequired();
            entity.Property(e => e.BlockedReason).HasMaxLength(500);
            entity.HasIndex(e => e.Email).IsUnique();
        });

        // CareerDomain
        modelBuilder.Entity<CareerDomain>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(256);
            entity.Property(e => e.Description).IsRequired();
            entity.Property(e => e.Color).HasMaxLength(50);
            
            entity.HasMany(d => d.Skills)
                .WithOne(s => s.CareerDomain)
                .HasForeignKey(s => s.CareerDomainId)
                .OnDelete(DeleteBehavior.Cascade);
            
            entity.HasMany(d => d.RoadmapSteps)
                .WithOne(r => r.CareerDomain)
                .HasForeignKey(r => r.CareerDomainId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Skill
        modelBuilder.Entity<Skill>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(256);
            entity.Property(e => e.DifficultyLevel).IsRequired().HasMaxLength(50);
        });

        // UserSkill
        modelBuilder.Entity<UserSkill>(entity =>
        {
            entity.HasKey(e => e.Id);
            
            entity.HasOne(us => us.User)
                .WithMany(u => u.UserSkills)
                .HasForeignKey(us => us.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            
            entity.HasOne(us => us.Skill)
                .WithMany(s => s.UserSkills)
                .HasForeignKey(us => us.SkillId)
                .OnDelete(DeleteBehavior.Cascade);
            
            entity.HasIndex(e => new { e.UserId, e.SkillId }).IsUnique();
        });

        // UserProgress
        modelBuilder.Entity<UserProgress>(entity =>
        {
            entity.HasKey(e => e.Id);
            
            entity.HasOne(up => up.User)
                .WithMany(u => u.UserProgressions)
                .HasForeignKey(up => up.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            
            entity.HasOne(up => up.CareerDomain)
                .WithMany(d => d.UserProgressions)
                .HasForeignKey(up => up.CareerDomainId)
                .OnDelete(DeleteBehavior.Cascade);
            
            entity.HasIndex(e => new { e.UserId, e.CareerDomainId }).IsUnique();
        });

        // QuizQuestion
        modelBuilder.Entity<QuizQuestion>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Text).IsRequired();
            entity.Property(e => e.Category).IsRequired().HasMaxLength(100);
            
            entity.HasMany(q => q.Options)
                .WithOne(o => o.QuizQuestion)
                .HasForeignKey(o => o.QuizQuestionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // QuizOption
        modelBuilder.Entity<QuizOption>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Text).IsRequired();
            entity.Property(e => e.DomainWeightJson).IsRequired();
        });

        // QuizResponse
        modelBuilder.Entity<QuizResponse>(entity =>
        {
            entity.HasKey(e => e.Id);
            
            entity.HasOne(qr => qr.User)
                .WithMany(u => u.QuizResponses)
                .HasForeignKey(qr => qr.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            
            entity.HasOne(qr => qr.QuizQuestion)
                .WithMany()
                .HasForeignKey(qr => qr.QuizQuestionId)
                .OnDelete(DeleteBehavior.Restrict);
            
            entity.HasOne(qr => qr.QuizOption)
                .WithMany()
                .HasForeignKey(qr => qr.QuizOptionId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // QuizResult
        modelBuilder.Entity<QuizResult>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ScoresJson).IsRequired();
            
            entity.HasOne(qr => qr.User)
                .WithMany()
                .HasForeignKey(qr => qr.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Badge
        modelBuilder.Entity<Badge>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(256);
            entity.Property(e => e.Rarity).IsRequired().HasMaxLength(50);
        });

        // UserBadge
        modelBuilder.Entity<UserBadge>(entity =>
        {
            entity.HasKey(e => e.Id);
            
            entity.HasOne(ub => ub.User)
                .WithMany(u => u.UserBadges)
                .HasForeignKey(ub => ub.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            
            entity.HasOne(ub => ub.Badge)
                .WithMany(b => b.UserBadges)
                .HasForeignKey(ub => ub.BadgeId)
                .OnDelete(DeleteBehavior.Cascade);
            
            entity.HasIndex(e => new { e.UserId, e.BadgeId }).IsUnique();
        });

        // RoadmapStep
        modelBuilder.Entity<RoadmapStep>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).IsRequired().HasMaxLength(256);
            
            entity.HasOne(r => r.Skill)
                .WithMany(s => s.RoadmapSteps)
                .HasForeignKey(r => r.SkillId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // RoadmapTopic
        modelBuilder.Entity<RoadmapTopic>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).IsRequired().HasMaxLength(256);
            
            entity.HasOne(t => t.Module)
                .WithMany(m => m.Topics)
                .HasForeignKey(t => t.ModuleId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // UserModuleProgress
        modelBuilder.Entity<UserModuleProgress>(entity =>
        {
            entity.HasKey(e => e.Id);
            
            entity.HasOne(ump => ump.User)
                .WithMany()
                .HasForeignKey(ump => ump.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            
            entity.HasOne(ump => ump.Module)
                .WithMany(m => m.UserProgressions)
                .HasForeignKey(ump => ump.ModuleId)
                .OnDelete(DeleteBehavior.Cascade);
            
            entity.HasIndex(e => new { e.UserId, e.ModuleId }).IsUnique();
        });

        // ApplicationMentor
        modelBuilder.Entity<ApplicationMentor>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Expertise).IsRequired().HasMaxLength(256);
            entity.Property(e => e.ExpertiseArea).HasMaxLength(256);
            entity.Property(e => e.CurrentCompany).HasMaxLength(256);
            entity.Property(e => e.SkillsCsv).HasMaxLength(2000);
            entity.Property(e => e.LinkedInUrl).HasMaxLength(500);
            entity.Property(e => e.AvailabilitySchedule).HasMaxLength(2000);
            entity.Property(e => e.HourlyRate).HasPrecision(18, 2);
            entity.Property(e => e.SessionPrice).HasPrecision(18, 2);
            entity.Property(e => e.ApprovedByAdminId).HasMaxLength(450);
            entity.Property(e => e.RejectionReason).HasMaxLength(1000);
            
            entity.HasOne(m => m.User)
                .WithOne(u => u.MentorProfile)
                .HasForeignKey<ApplicationMentor>(m => m.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // MentorRequest
        modelBuilder.Entity<MentorRequest>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Topic).IsRequired().HasMaxLength(256);
            entity.Property(e => e.Status).IsRequired().HasMaxLength(50);
            
            entity.HasOne(mr => mr.Student)
                .WithMany(u => u.SentRequests)
                .HasForeignKey(mr => mr.StudentId)
                .OnDelete(DeleteBehavior.Restrict);
            
            entity.HasOne(mr => mr.Mentor)
                .WithMany(m => m.MentorRequests)
                .HasForeignKey(mr => mr.MentorId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // MentorSession
        modelBuilder.Entity<MentorSession>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Status).IsRequired().HasMaxLength(50);
            entity.Property(e => e.MeetingLink).HasMaxLength(500);

            entity.HasOne(ms => ms.Student)
                .WithMany(u => u.StudentSessions)
                .HasForeignKey(ms => ms.StudentId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(ms => ms.Mentor)
                .WithMany(m => m.Sessions)
                .HasForeignKey(ms => ms.MentorId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // SessionFeedback
        modelBuilder.Entity<SessionFeedback>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Rating).IsRequired();

            entity.HasOne(sf => sf.Session)
                .WithOne(s => s.Feedback)
                .HasForeignKey<SessionFeedback>(sf => sf.SessionId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(sf => sf.Mentor)
                .WithMany(m => m.Feedbacks)
                .HasForeignKey(sf => sf.MentorId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // MentorshipProgress
        modelBuilder.Entity<MentorshipProgress>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.RoadmapStage).IsRequired().HasMaxLength(256);
            entity.Property(e => e.Amount).HasPrecision(18, 2);

            entity.HasOne(mp => mp.Student)
                .WithMany(u => u.MentorshipProgressEntries)
                .HasForeignKey(mp => mp.StudentId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(mp => mp.Mentor)
                .WithMany(m => m.MentorshipProgressEntries)
                .HasForeignKey(mp => mp.MentorId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => new { e.StudentId, e.MentorId }).IsUnique();
        });

        modelBuilder.Entity<StudentWallet>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Balance).HasPrecision(18, 2);

            entity.HasOne(sw => sw.Student)
                .WithMany()
                .HasForeignKey(sw => sw.StudentId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => e.StudentId).IsUnique();
        });

        modelBuilder.Entity<PaymentTransaction>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Amount).HasPrecision(18, 2);

            entity.HasOne(pt => pt.Student)
                .WithMany()
                .HasForeignKey(pt => pt.StudentId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(pt => pt.MentorshipProgress)
                .WithMany()
                .HasForeignKey(pt => pt.MentorshipProgressId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(pt => pt.Session)
                .WithMany()
                .HasForeignKey(pt => pt.SessionId)
                .OnDelete(DeleteBehavior.NoAction);
        });

        modelBuilder.Entity<MentorSessionPriceHistory>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.SessionPrice).HasPrecision(18, 2);

            entity.HasOne(h => h.Mentor)
                .WithMany(m => m.SessionPriceHistory)
                .HasForeignKey(h => h.MentorId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => new { e.MentorId, e.EffectiveFrom });
        });

        // Notification
        modelBuilder.Entity<Notification>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Message).IsRequired().HasMaxLength(1000);

            entity.HasOne(n => n.User)
                .WithMany(u => u.Notifications)
                .HasForeignKey(n => n.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // SessionComplaint
        modelBuilder.Entity<SessionComplaint>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Reason).IsRequired().HasMaxLength(1000);
            entity.Property(e => e.Status).IsRequired().HasMaxLength(50);
            entity.Property(e => e.ResolutionNote).HasMaxLength(1000);

            entity.HasOne(sc => sc.Session)
                .WithMany()
                .HasForeignKey(sc => sc.SessionId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(sc => sc.Reporter)
                .WithMany()
                .HasForeignKey(sc => sc.ReportedBy)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // AdminAuditLog
        modelBuilder.Entity<AdminAuditLog>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Action).IsRequired().HasMaxLength(300);
            entity.Property(e => e.TargetEntity).IsRequired().HasMaxLength(200);
            entity.Property(e => e.TargetEntityId).HasMaxLength(450);

            entity.HasOne(a => a.Admin)
                .WithMany()
                .HasForeignKey(a => a.AdminId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // ChatMessage
        modelBuilder.Entity<ChatMessage>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Content).IsRequired();
            
            entity.HasOne(cm => cm.Sender)
                .WithMany()
                .HasForeignKey(cm => cm.SenderId)
                .OnDelete(DeleteBehavior.Restrict);
            
            entity.HasOne(cm => cm.Receiver)
                .WithMany()
                .HasForeignKey(cm => cm.ReceiverId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
