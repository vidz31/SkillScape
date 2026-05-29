using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SkillScape.Domain.Entities;
using SkillScape.Infrastructure.Data;
using System.Text.Json;


namespace SkillScape.API.Data;

public static class DatabaseSeeder
{
    public static async Task SeedAsync(ApplicationDbContext context)
    {
        // Seed Hierarchical Career Guidance Module Data first if not present
        await SeedCareerPathsAsync(context);
        await SeedHierarchicalQuizQuestionsAsync(context);

        // Skip if data already exists
        if (context.CareerDomains.Any())
        {
            // But still seed mentorship progress if mentors exist but no progress exists
            if (context.Mentors.Any() && !context.MentorshipProgressEntries.Any())
            {
                var existingUsers = context.Users.ToList();
                var existingMentors = context.Mentors.ToList();
                var progressEntries = SeedMentorshipProgress(context, existingUsers, existingMentors);
                context.MentorshipProgressEntries.AddRange(progressEntries);
                await context.SaveChangesAsync();
            }
            return;
        }

        // Seed Career Domains
        var domains = SeedDomains(context);
        context.CareerDomains.AddRange(domains);
        await context.SaveChangesAsync();

        // Seed Skills
        var skills = SeedSkills(context, domains);
        context.Skills.AddRange(skills);
        await context.SaveChangesAsync();

        // Seed Quiz Questions and Options
        var (questions, options) = SeedQuiz(context, domains);
        context.QuizQuestions.AddRange(questions);
        context.QuizOptions.AddRange(options);
        await context.SaveChangesAsync();

        // Seed Badges
        var badges = SeedBadges();
        context.Badges.AddRange(badges);
        await context.SaveChangesAsync();

        // Seed Roadmap Steps
        var roadmapSteps = SeedRoadmapSteps(context, domains, skills);
        context.RoadmapSteps.AddRange(roadmapSteps);
        await context.SaveChangesAsync();

        // Seed Sample Users
        var users = SeedUsers();
        context.Users.AddRange(users);
        await context.SaveChangesAsync();

        // Seed Sample Mentors
        var mentors = SeedMentors(context, users.Where(u => u.Role == "Mentor").ToList());
        context.Mentors.AddRange(mentors);
        await context.SaveChangesAsync();

        // Seed Mentorship Progress (student-mentor relationships)
        var mentorshipProgresses = SeedMentorshipProgress(context, users, mentors);
        context.MentorshipProgressEntries.AddRange(mentorshipProgresses);
        await context.SaveChangesAsync();
    }

    private static List<CareerDomain> SeedDomains(ApplicationDbContext context)
    {
        return new List<CareerDomain>
        {
            new CareerDomain
            {
                Id = "frontend",
                Name = "Frontend Development",
                Description = "Build beautiful and interactive user interfaces with modern web technologies",
                Color = "bg-pink-500",
                IconUrl = "https://api.dicebear.com/7.x/icons/svg?seed=frontend",
                DisplayOrder = 1,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            },
            new CareerDomain
            {
                Id = "backend",
                Name = "Backend Development",
                Description = "Build robust APIs and server-side applications that power modern applications",
                Color = "bg-blue-500",
                IconUrl = "https://api.dicebear.com/7.x/icons/svg?seed=backend",
                DisplayOrder = 2,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            },
            new CareerDomain
            {
                Id = "fullstack",
                Name = "Full Stack Development",
                Description = "Master both frontend and backend to become a complete developer",
                Color = "bg-purple-500",
                IconUrl = "https://api.dicebear.com/7.x/icons/svg?seed=fullstack",
                DisplayOrder = 3,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            },
            new CareerDomain
            {
                Id = "data",
                Name = "Data Science",
                Description = "Analyze data and extract insights using Python, SQL, and statistical methods",
                Color = "bg-green-500",
                IconUrl = "https://api.dicebear.com/7.x/icons/svg?seed=data",
                DisplayOrder = 4,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            },
            new CareerDomain
            {
                Id = "devops",
                Name = "DevOps & Cloud",
                Description = "Master deployment, scaling, and infrastructure automation",
                Color = "bg-orange-500",
                IconUrl = "https://api.dicebear.com/7.x/icons/svg?seed=devops",
                DisplayOrder = 5,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            },
            new CareerDomain
            {
                Id = "ml",
                Name = "Machine Learning",
                Description = "Build intelligent systems using machine learning algorithms",
                Color = "bg-indigo-500",
                IconUrl = "https://api.dicebear.com/7.x/icons/svg?seed=ml",
                DisplayOrder = 6,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            },
            new CareerDomain
            {
                Id = "business",
                Name = "Business & Management",
                Description = "Lead companies, manage budgets, and drive strategic growth",
                Color = "bg-yellow-500",
                IconUrl = "https://api.dicebear.com/7.x/icons/svg?seed=business",
                DisplayOrder = 7,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            },
            new CareerDomain
            {
                Id = "design",
                Name = "Creative & Design",
                Description = "Create stunning visual graphics, brands, and UI/UX",
                Color = "bg-red-500",
                IconUrl = "https://api.dicebear.com/7.x/icons/svg?seed=design",
                DisplayOrder = 8,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            },
            new CareerDomain
            {
                Id = "healthcare",
                Name = "Healthcare & Medicine",
                Description = "Treat patients, innovate medical research, and save lives",
                Color = "bg-teal-500",
                IconUrl = "https://api.dicebear.com/7.x/icons/svg?seed=healthcare",
                DisplayOrder = 9,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            },
            new CareerDomain
            {
                Id = "education",
                Name = "Education & Teaching",
                Description = "Inspire students, foster learning, and cultivate growth",
                Color = "bg-lime-500",
                IconUrl = "https://api.dicebear.com/7.x/icons/svg?seed=education",
                DisplayOrder = 10,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            }
        };
    }

    // Classes to deserialize the new curriculum JSON
    private class JsonCurriculumDomain
    {
        public string domainId { get; set; } = string.Empty;
        public List<JsonCurriculumSkill> skills { get; set; } = new();
    }
    private class JsonCurriculumSkill
    {
        public string id { get; set; } = string.Empty;
        public string name { get; set; } = string.Empty;
        public string description { get; set; } = string.Empty;
        public string difficultyLevel { get; set; } = string.Empty;
        public int xpReward { get; set; }
        public int displayOrder { get; set; }
        public List<JsonCurriculumStep> steps { get; set; } = new();
    }
    private class JsonCurriculumStep
    {
        public string id { get; set; } = string.Empty;
        public string title { get; set; } = string.Empty;
        public string description { get; set; } = string.Empty;
        public int estimatedHours { get; set; }
        public int stepNumber { get; set; }
        public List<JsonCurriculumTopic> topics { get; set; } = new();
    }
    private class JsonCurriculumTopic
    {
        public string title { get; set; } = string.Empty;
        public int displayOrder { get; set; }
    }

    private static List<JsonCurriculumDomain> LoadCurriculum()
    {
        var jsonFilePath = Path.Combine(AppContext.BaseDirectory, "Data", "roadmap_curriculum.json");
        if (!File.Exists(jsonFilePath))
        {
            throw new FileNotFoundException($"Curriculum file not found at {jsonFilePath}");
        }

        var jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var fileContent = File.ReadAllText(jsonFilePath);
        return JsonSerializer.Deserialize<List<JsonCurriculumDomain>>(fileContent, jsonOptions) ?? new List<JsonCurriculumDomain>();
    }

    private static List<Skill> SeedSkills(ApplicationDbContext context, List<CareerDomain> domains)
    {
        var skillsDb = new List<Skill>();
        var curriculum = LoadCurriculum();

        foreach (var cDomain in curriculum)
        {
            var domain = domains.FirstOrDefault(d => d.Id == cDomain.domainId);
            if (domain == null) continue;

            foreach (var cSkill in cDomain.skills)
            {
                skillsDb.Add(new Skill
                {
                    Id = cSkill.id,
                    CareerDomainId = domain.Id,
                    Name = cSkill.name,
                    Description = cSkill.description,
                    DifficultyLevel = cSkill.difficultyLevel,
                    XPReward = cSkill.xpReward,
                    DisplayOrder = cSkill.displayOrder,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                });
            }
        }
        return skillsDb;
    }

    private static List<RoadmapStep> SeedRoadmapSteps(ApplicationDbContext context, List<CareerDomain> domains, List<Skill> skills)
    {
        var stepsDb = new List<RoadmapStep>();
        var curriculum = LoadCurriculum();

        foreach (var cDomain in curriculum)
        {
            var domain = domains.FirstOrDefault(d => d.Id == cDomain.domainId);
            if (domain == null) continue;

            foreach (var cSkill in cDomain.skills)
            {
                var skill = skills.FirstOrDefault(s => s.Id == cSkill.id);
                if (skill == null) continue;

                foreach (var cStep in cSkill.steps)
                {
                    var roadmapStep = new RoadmapStep
                    {
                        Id = cStep.id,
                        CareerDomainId = domain.Id,
                        SkillId = skill.Id,
                        Title = cStep.title,
                        Description = cStep.description,
                        StepNumber = cStep.stepNumber,
                        EstimatedHours = cStep.estimatedHours,
                        IsActive = true,
                        Topics = new List<RoadmapTopic>()
                    };

                    foreach (var cTopic in cStep.topics)
                    {
                        roadmapStep.Topics.Add(new RoadmapTopic
                        {
                            Id = Guid.NewGuid().ToString(),
                            ModuleId = roadmapStep.Id,
                            Title = cTopic.title,
                            DisplayOrder = cTopic.displayOrder,
                            ResourceUrl = "https://developer.mozilla.org"
                        });
                    }
                    
                    stepsDb.Add(roadmapStep);
                }
            }
        }

        return stepsDb;
    }

    private class JsonQuizQuestion
    {
        public string id { get; set; } = string.Empty;
        public string text { get; set; } = string.Empty;
        public string category { get; set; } = string.Empty;
        public List<JsonQuizOption> options { get; set; } = new();
    }
    
    private class JsonQuizOption
    {
        public string id { get; set; } = string.Empty;
        public string text { get; set; } = string.Empty;
        public string domain { get; set; } = string.Empty;
    }

    private static (List<QuizQuestion>, List<QuizOption>) SeedQuiz(ApplicationDbContext context, List<CareerDomain> domains)
    {
        var questionsDb = new List<QuizQuestion>();
        var optionsDb = new List<QuizOption>();

        // Load questions from JSON file instead of hardcoding
        var jsonFilePath = Path.Combine(AppContext.BaseDirectory, "Data", "quiz_150_questions.json");
        if (!File.Exists(jsonFilePath))
        {
            throw new FileNotFoundException($"Seed file not found at {jsonFilePath}");
        }

        var jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var fileContent = File.ReadAllText(jsonFilePath);
        var parsedQuestions = JsonSerializer.Deserialize<List<JsonQuizQuestion>>(fileContent, jsonOptions);

        if (parsedQuestions == null) return (questionsDb, optionsDb);

        int qOrder = 1;
        foreach (var q in parsedQuestions)
        {
            var dbQ = new QuizQuestion 
            { 
                Id = q.id, 
                Text = q.text, 
                Category = q.category, 
                DisplayOrder = qOrder++, 
                IsActive = true, 
                // Just arbitrary mapping for the schema requirement
                CareerDomainId = domains[0].Id 
            };
            questionsDb.Add(dbQ);

            int optOrder = 1;
            foreach (var opt in q.options)
            {
                optionsDb.Add(new QuizOption 
                { 
                    Id = opt.id, 
                    QuizQuestionId = dbQ.Id, 
                    Text = opt.text,
                    // Map the chosen domain with weight 1 (doesn't matter since we use ML now, but keeps old logic safe)
                    DomainWeightJson = JsonSerializer.Serialize(new Dictionary<string, int> { { opt.domain, 1 } }), 
                    DisplayOrder = optOrder++ 
                });
            }
        }

        return (questionsDb, optionsDb);
    }

    private static List<Badge> SeedBadges()
    {
        return new List<Badge>
        {
            new Badge { Id = Guid.NewGuid().ToString(), Name = "First Steps", Description = "Complete your first skill", IconUrl = "👣", Rarity = "Common", XPRequired = 0, SkillsCompletedRequired = 1, IsActive = true },
            new Badge { Id = Guid.NewGuid().ToString(), Name = "Quick Learner", Description = "Complete 5 skills", IconUrl = "⚡", Rarity = "Rare", XPRequired = 0, SkillsCompletedRequired = 5, IsActive = true },
            new Badge { Id = Guid.NewGuid().ToString(), Name = "Skill Master", Description = "Complete 10 skills", IconUrl = "🏆", Rarity = "Epic", XPRequired = 0, SkillsCompletedRequired = 10, IsActive = true },
            new Badge { Id = Guid.NewGuid().ToString(), Name = "Legendary", Description = "Reach 500 XP", IconUrl = "👑", Rarity = "Legendary", XPRequired = 500, IsActive = true },
            new Badge { Id = Guid.NewGuid().ToString(), Name = "Domain Expert", Description = "Complete a full domain", IconUrl = "🔥", Rarity = "Epic", XPRequired = 100, IsActive = true }
        };
    }

    private static List<ApplicationUser> SeedUsers()
    {
        var mentorNames = new[]
        {
            ("Sarah Chen", "Frontend Development", "UI/UX specialist with 10 years experience"),
            ("Rajesh Kumar", "Backend Development", "Cloud architect and API design expert"),
            ("Emily Watson", "Data Science", "ML engineer with focus on deep learning"),
            ("Michael Johnson", "Full Stack Development", "MERN stack expert"),
            ("Priya Sharma", "DevOps & Cloud", "AWS certified cloud architect"),
            ("David Lee", "Mobile Development", "React Native and iOS developer"),
            ("Jessica Brown", "Web3 & Blockchain", "Smart contracts and DeFi specialist"),
            ("Alex Martinez", "System Design", "Distributed systems expert"),
            ("Lisa Wang", "Security & Cryptography", "Cybersecurity specialist"),
            ("James Wilson", "Game Development", "Unity and Unreal Engine expert")
        };

        var users = new List<ApplicationUser>
        {
            new ApplicationUser
            {
                Id = Guid.NewGuid().ToString(),
                Email = "student@example.com",
                FullName = "John Learner",
                Role = "Student",
                Bio = "Aspiring developer learning web technologies",
                Level = 1,
                TotalXP = 0,
                CurrentStreak = 0,
                IsActive = true,
                ProfileCompleted = false,
                IsBlocked = false,
                BlockedReason = null,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new ApplicationUser
            {
                Id = Guid.NewGuid().ToString(),
                Email = "admin@example.com",
                FullName = "Admin User",
                Role = "Admin",
                Level = 20,
                TotalXP = 5000,
                IsActive = true,
                ProfileCompleted = true,
                IsBlocked = false,
                BlockedReason = null,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }
        };

        // Create mentor users with diverse specializations
        foreach (var (name, specialty, bio) in mentorNames)
        {
            users.Add(new ApplicationUser
            {
                Id = Guid.NewGuid().ToString(),
                Email = $"{name.ToLower().Replace(" ", ".")}@mentors.com",
                FullName = name,
                Role = "Mentor",
                Bio = bio,
                Level = 10,
                TotalXP = 2000,
                CurrentStreak = 30,
                IsActive = true,
                ProfileCompleted = true,
                IsBlocked = false,
                BlockedReason = null,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
        }

        return users;
    }

    private static List<ApplicationMentor> SeedMentors(ApplicationDbContext context, List<ApplicationUser> mentorUsers)
    {
        var mentorSpecializations = new Dictionary<string, (string expertise, string skills, string company, decimal rate, double rating)>
        {
            { "Sarah Chen", ("Frontend Development", "React,JavaScript,CSS,TypeScript", "TechCorp", 60, 4.9) },
            { "Rajesh Kumar", ("Backend Development", "Node.js,Python,Docker,Kubernetes", "CloudInnovate", 70, 4.7) },
            { "Emily Watson", ("Data Science", "Python,TensorFlow,pandas,scikit-learn", "DataAI Labs", 80, 4.8) },
            { "Michael Johnson", ("Full Stack Development", "React,Node.js,MongoDB,Express", "WebDev Solutions", 55, 4.6) },
            { "Priya Sharma", ("DevOps & Cloud", "AWS,Azure,Terraform,CI/CD", "CloudOps Inc", 75, 4.9) },
            { "David Lee", ("Mobile Development", "React Native,Swift,Kotlin,Flutter", "MobileWorks", 65, 4.7) },
            { "Jessica Brown", ("Web3 & Blockchain", "Solidity,Ethereum,Web3.js,Smart Contracts", "BlockchainHub", 90, 4.8) },
            { "Alex Martinez", ("System Design", "Microservices,Distributed Systems,C++", "SystemsDesign Co", 85, 4.9) },
            { "Lisa Wang", ("Security & Cryptography", "Cybersecurity,Ethical Hacking,Cryptography", "SecureCode Inc", 95, 4.8) },
            { "James Wilson", ("Game Development", "Unity,Unreal,C#,GameDesign", "GameStudio Pro", 70, 4.7) }
        };

        return mentorUsers
            .Where(u => u.Role == "Mentor")
            .Select(user =>
            {
                var (expertise, skills, company, rate, rating) = mentorSpecializations.ContainsKey(user.FullName)
                    ? mentorSpecializations[user.FullName]
                    : ("Full Stack Development", "C#,ASP.NET Core,React,SQL", "SkillScape Labs", 50, 4.5);

                return new ApplicationMentor
                {
                    Id = Guid.NewGuid().ToString(),
                    UserId = user.Id,
                    Expertise = expertise,
                    ExpertiseArea = expertise,
                    CurrentCompany = company,
                    Bio = user.Bio,
                    SkillsCsv = skills,
                    LinkedInUrl = $"https://linkedin.com/in/{user.FullName.ToLower().Replace(" ", "-")}",
                    AvailabilitySchedule = "Monday,Tuesday,Wednesday,Thursday,Friday,Saturday",
                    YearsOfExperience = new Random().Next(3, 15),
                    HourlyRate = rate,
                    SessionPrice = rate,
                    IsAvailable = true,
                    TotalSessionCount = new Random().Next(10, 100),
                    AvgRating = rating,
                    IsApproved = true,
                    ApprovedByAdminId = null,
                    ApprovedAt = DateTime.UtcNow,
                    RejectionReason = null,
                    CreatedAt = DateTime.UtcNow
                };
            }).ToList();
    }

    private static List<MentorshipProgress> SeedMentorshipProgress(ApplicationDbContext context, List<ApplicationUser> users, List<ApplicationMentor> mentors)
    {
        var mentorshipProgresses = new List<MentorshipProgress>();
        var studentUsers = users.Where(u => u.Role == "Student").ToList();
        
        if (studentUsers.Count == 0)
            return mentorshipProgresses;

        var random = new Random();
        var roadmapStages = new[] { "Basics", "Fundamentals", "Intermediate", "Advanced", "Expert", "Mastery" };
        var tasks = new[] 
        { 
            "Complete beginner course, Learn syntax, Build first project",
            "Build 2 projects, Contribute to open source, Review code",
            "Advanced design patterns, System design, Mentoring skills",
            "Build scalable application, Deploy to production, Optimize performance",
            "Write technical blog, Create course content, Lead team",
            "Expert level, Industry recognition, Multiple achievements"
        };

        // Get existing entries to avoid duplicates
        var existingEntries = context.MentorshipProgressEntries
            .Select(e => new { e.StudentId, e.RoadmapStage })
            .ToHashSet();

        // Create mentorship progress for each mentor
        foreach (var mentor in mentors)
        {
            // Each mentor mentors 2-5 students
            int studentCount = Math.Min(random.Next(2, 6), studentUsers.Count);
            var assignedStudents = studentUsers.OrderBy(x => random.Next()).Take(studentCount).ToList();

            foreach (var student in assignedStudents)
            {
                // Pick stages that don't already exist for this student
                var availableStages = roadmapStages
                    .Where(stage => !existingEntries.Contains(new { StudentId = student.Id, RoadmapStage = stage }))
                    .ToList();

                if (availableStages.Count == 0)
                    continue; // Skip if all stages already assigned to this student

                var stage = availableStages[random.Next(availableStages.Count)];
                
                mentorshipProgresses.Add(new MentorshipProgress
                {
                    Id = Guid.NewGuid().ToString(),
                    StudentId = student.Id,
                    MentorId = mentor.Id,
                    RoadmapStage = stage,
                    MentorFeedback = $"Great progress on {stage}! Keep practicing and building projects.",
                    CompletedTasks = tasks[random.Next(tasks.Length)],
                    NextMilestone = roadmapStages[(Array.IndexOf(roadmapStages, stage) + 1) % roadmapStages.Length],
                    UpdatedAt = DateTime.UtcNow.AddDays(-random.Next(0, 30))
                });

                // Track the new entry
                existingEntries.Add(new { StudentId = student.Id, RoadmapStage = stage });
            }
        }

        return mentorshipProgresses;
    }

    private static async Task SeedCareerPathsAsync(ApplicationDbContext context)
    {
        // Clear existing to refresh seeder data
        if (context.UserCareerProfiles.Any())
        {
            context.UserCareerProfiles.RemoveRange(context.UserCareerProfiles);
            await context.SaveChangesAsync();
        }
        if (context.CareerPaths.Any())
        {
            context.CareerPaths.RemoveRange(context.CareerPaths);
            await context.SaveChangesAsync();
        }

        var allPaths = new List<CareerPath>();

        // Level 1: Root entry-point categories
        // After 10th
        AddPath(allPaths, "10th-sci", "Science Stream", "After10th", "flask", "blue", 1, null);
        AddPath(allPaths, "10th-com", "Commerce Stream", "After10th", "banknote", "green", 1, null);
        AddPath(allPaths, "10th-art", "Arts & Humanities", "After10th", "palette", "purple", 1, null);
        AddPath(allPaths, "10th-dip", "Diploma Studies", "After10th", "briefcase", "amber", 1, null);
        AddPath(allPaths, "10th-iti", "ITI Trades", "After10th", "server", "rose", 1, null);
        AddPath(allPaths, "10th-voc", "Vocational Training", "After10th", "sparkles", "cyan", 1, null);

        // After 12th
        AddPath(allPaths, "12th-pcm", "Science PCM Stream", "After12th", "flask", "blue", 1, null);
        AddPath(allPaths, "12th-pcb", "Science PCB Stream", "After12th", "heart-pulse", "red", 1, null);
        AddPath(allPaths, "12th-com", "Commerce Stream", "After12th", "banknote", "green", 1, null);
        AddPath(allPaths, "12th-art", "Arts Stream", "After12th", "palette", "purple", 1, null);

        // After Graduation
        AddPath(allPaths, "grad-eng", "Engineering Specializations", "AfterGraduation", "cpu", "blue", 1, null);
        AddPath(allPaths, "grad-cs", "Computer Science & IT", "AfterGraduation", "code", "cyan", 1, null);
        AddPath(allPaths, "grad-bus", "Business Administration", "AfterGraduation", "banknote", "green", 1, null);
        AddPath(allPaths, "grad-med", "Clinical & Healthcare Practice", "AfterGraduation", "heart-pulse", "red", 1, null);
        AddPath(allPaths, "grad-art", "Creative Arts & Design", "AfterGraduation", "palette", "purple", 1, null);

        // After Post Graduation
        AddPath(allPaths, "pg-phd", "PhD & Academic Research", "AfterPostGraduation", "flask", "blue", 1, null);
        AddPath(allPaths, "pg-prof", "Advanced Professional Practice", "AfterPostGraduation", "briefcase", "green", 1, null);

        // Career Switch
        AddPath(allPaths, "swi-tech", "Switching to Software Engineering", "CareerSwitch", "code", "rose", 1, null);
        AddPath(allPaths, "swi-mgmt", "Switching to Management & PM", "CareerSwitch", "briefcase", "amber", 1, null);
        AddPath(allPaths, "swi-des", "Switching to Creative Design", "CareerSwitch", "palette", "purple", 1, null);

        // Upskilling
        AddPath(allPaths, "ups-cloud", "Cloud & Infrastructure", "Upskilling", "server", "cyan", 1, null);
        AddPath(allPaths, "ups-ai", "AI & Machine Learning Systems", "Upskilling", "sparkles", "indigo", 1, null);
        AddPath(allPaths, "ups-cyber", "Cybersecurity Systems", "Upskilling", "shield-alert", "red", 1, null);

        // Entrepreneurship
        AddPath(allPaths, "ent-startup", "Tech & Product Startups", "Entrepreneurship", "sparkles", "indigo", 1, null);
        AddPath(allPaths, "ent-d2c", "D2C Brands & Commerce", "Entrepreneurship", "banknote", "green", 1, null);

        // Government Jobs
        AddPath(allPaths, "gov-civil", "Civil Services", "GovernmentJobs", "briefcase", "amber", 1, null);
        AddPath(allPaths, "gov-bank", "Banking PO & RBI Services", "GovernmentJobs", "banknote", "green", 1, null);
        AddPath(allPaths, "gov-def", "Defence Commission", "GovernmentJobs", "shield-alert", "blue", 1, null);

        // International Education
        AddPath(allPaths, "int-ms", "MS in STEM Fields", "InternationalEducation", "flask", "blue", 1, null);
        AddPath(allPaths, "int-mba", "Global MBA Programs", "InternationalEducation", "banknote", "green", 1, null);

        // Level 2: Subdomains
        var level2List = new List<(string Id, string Title, string ParentId, string StreamType, string Icon, string Color)>();

        // After 10th
        level2List.Add(("10th-sci-pcm", "PCM (Physics, Chemistry, Math)", "10th-sci", "After10th", "flask", "blue"));
        level2List.Add(("10th-sci-pcb", "PCB (Physics, Chemistry, Biology)", "10th-sci", "After10th", "flask", "blue"));
        level2List.Add(("10th-sci-pcmb", "PCMB (Bio-Math Combination)", "10th-sci", "After10th", "flask", "blue"));
        
        level2List.Add(("10th-com-math", "Commerce with Mathematics", "10th-com", "After10th", "banknote", "green"));
        level2List.Add(("10th-com-nomath", "Commerce without Mathematics", "10th-com", "After10th", "banknote", "green"));

        level2List.Add(("10th-art-psych", "Psychology Branch", "10th-art", "After10th", "palette", "purple"));
        level2List.Add(("10th-art-soc", "Sociology Branch", "10th-art", "After10th", "palette", "purple"));
        level2List.Add(("10th-art-polsci", "Political Science Branch", "10th-art", "After10th", "palette", "purple"));
        level2List.Add(("10th-art-hist", "History Branch", "10th-art", "After10th", "palette", "purple"));
        level2List.Add(("10th-art-geog", "Geography Branch", "10th-art", "After10th", "palette", "purple"));
        level2List.Add(("10th-art-phil", "Philosophy Branch", "10th-art", "After10th", "palette", "purple"));

        level2List.Add(("10th-dip-comp", "Computer Engineering Diploma", "10th-dip", "After10th", "briefcase", "amber"));
        level2List.Add(("10th-dip-mech", "Mechanical Engineering Diploma", "10th-dip", "After10th", "briefcase", "amber"));
        level2List.Add(("10th-dip-civil", "Civil Engineering Diploma", "10th-dip", "After10th", "briefcase", "amber"));
        level2List.Add(("10th-dip-elec", "Electrical Engineering Diploma", "10th-dip", "After10th", "briefcase", "amber"));
        level2List.Add(("10th-dip-entc", "Electronics Engineering Diploma", "10th-dip", "After10th", "briefcase", "amber"));
        level2List.Add(("10th-dip-auto", "Automobile Engineering Diploma", "10th-dip", "After10th", "briefcase", "amber"));
        level2List.Add(("10th-dip-mecha", "Mechatronics Diploma", "10th-dip", "After10th", "briefcase", "amber"));
        level2List.Add(("10th-dip-text", "Textile Technology Diploma", "10th-dip", "After10th", "briefcase", "amber"));
        level2List.Add(("10th-dip-chem", "Chemical Engineering Diploma", "10th-dip", "After10th", "briefcase", "amber"));

        level2List.Add(("10th-iti-elec", "Electrician Trade ITI", "10th-iti", "After10th", "server", "rose"));
        level2List.Add(("10th-iti-fitter", "Fitter Trade ITI", "10th-iti", "After10th", "server", "rose"));
        level2List.Add(("10th-iti-welder", "Welder Trade ITI", "10th-iti", "After10th", "server", "rose"));
        level2List.Add(("10th-iti-mech", "Mechanic Trade ITI", "10th-iti", "After10th", "server", "rose"));
        level2List.Add(("10th-iti-plumb", "Plumber Trade ITI", "10th-iti", "After10th", "server", "rose"));
        level2List.Add(("10th-iti-tech", "Technician Trade ITI", "10th-iti", "After10th", "server", "rose"));

        level2List.Add(("10th-voc-gd", "Graphic Design", "10th-voc", "After10th", "sparkles", "cyan"));
        level2List.Add(("10th-voc-dm", "Digital Marketing", "10th-voc", "After10th", "sparkles", "cyan"));
        level2List.Add(("10th-voc-anim", "Animation & VFX", "10th-voc", "After10th", "sparkles", "cyan"));
        level2List.Add(("10th-voc-photo", "Photography Skills", "10th-voc", "After10th", "sparkles", "cyan"));
        level2List.Add(("10th-voc-cul", "Culinary Arts", "10th-voc", "After10th", "sparkles", "cyan"));
        level2List.Add(("10th-voc-hosp", "Hospitality Operations", "10th-voc", "After10th", "sparkles", "cyan"));

        // After 12th
        level2List.Add(("12th-pcm-eng", "Engineering (B.Tech/B.E)", "12th-pcm", "After12th", "flask", "blue"));
        level2List.Add(("12th-pcm-arch", "Architecture Studies", "12th-pcm", "After12th", "flask", "blue"));
        level2List.Add(("12th-pcm-def", "Defence Commissioning", "12th-pcm", "After12th", "flask", "blue"));
        level2List.Add(("12th-pcm-av", "Aviation Tracks", "12th-pcm", "After12th", "flask", "blue"));
        level2List.Add(("12th-pcm-res", "Core Research Sciences", "12th-pcm", "After12th", "flask", "blue"));

        level2List.Add(("12th-pcb-med", "Medical Sciences (MBBS/BDS)", "12th-pcb", "After12th", "heart-pulse", "red"));
        level2List.Add(("12th-pcb-hc", "Healthcare Professions", "12th-pcb", "After12th", "heart-pulse", "red"));
        level2List.Add(("12th-pcb-res", "Biological Research", "12th-pcb", "After12th", "heart-pulse", "red"));

        level2List.Add(("12th-com-fin", "Finance Specialties", "12th-com", "After12th", "banknote", "green"));
        level2List.Add(("12th-com-bus", "Business & Startups", "12th-com", "After12th", "banknote", "green"));
        level2List.Add(("12th-com-bank", "Banking Services", "12th-com", "After12th", "banknote", "green"));
        level2List.Add(("12th-com-econ", "Economics & Analysis", "12th-com", "After12th", "banknote", "green"));
        level2List.Add(("12th-com-acct", "Accounting & Taxation", "12th-com", "After12th", "banknote", "green"));
        level2List.Add(("12th-com-fintech", "Financial Technology", "12th-com", "After12th", "banknote", "green"));

        level2List.Add(("12th-art-psych", "Psychology Practices", "12th-art", "After12th", "palette", "purple"));
        level2List.Add(("12th-art-law", "Legal Studies & Judiciary", "12th-art", "After12th", "palette", "purple"));
        level2List.Add(("12th-art-media", "Media & Journalism", "12th-art", "After12th", "palette", "purple"));
        level2List.Add(("12th-art-des", "Creative Design Studies", "12th-art", "After12th", "palette", "purple"));
        level2List.Add(("12th-art-soc", "Social Sciences & Policy", "12th-art", "After12th", "palette", "purple"));
        level2List.Add(("12th-art-civil", "Civil Services Preparations", "12th-art", "After12th", "palette", "purple"));
        level2List.Add(("12th-art-edu", "Education & Teaching", "12th-art", "After12th", "palette", "purple"));

        // Build Level 2 paths for other remaining streams
        var remainingRoots = allPaths.Where(p => p.Level == 1 && !p.Id.StartsWith("10th-") && !p.Id.StartsWith("12th-")).ToList();
        foreach (var r in remainingRoots)
        {
            level2List.Add(($"{r.Id}-sd1", $"{r.Title} - Core Domain", r.Id, r.StreamType, r.Icon, r.Color));
            level2List.Add(($"{r.Id}-sd2", $"{r.Title} - Advanced Studies", r.Id, r.StreamType, r.Icon, r.Color));
        }

        // Add Level 2 paths to database
        foreach (var l2 in level2List)
        {
            AddPath(allPaths, l2.Id, l2.Title, l2.StreamType, l2.Icon, l2.Color, 2, l2.ParentId);
        }

        // Level 3: Fields under Level 2
        var level3List = new List<(string Id, string Title, string ParentId, string StreamType, string Icon, string Color)>();

        // Under 12th-pcm-eng (Level 2 Engineering PCM): 21 fields
        level3List.Add(("12th-pcm-eng-cs", "Computer Science Engineering", "12th-pcm-eng", "After12th,AfterGraduation", "code", "blue"));
        level3List.Add(("12th-pcm-eng-ai", "Artificial Intelligence Engineering", "12th-pcm-eng", "After12th,AfterGraduation", "sparkles", "blue"));
        level3List.Add(("12th-pcm-eng-ml", "Machine Learning Systems", "12th-pcm-eng", "After12th,AfterGraduation", "sparkles", "blue"));
        level3List.Add(("12th-pcm-eng-ds", "Data Science Engineering", "12th-pcm-eng", "After12th,AfterGraduation", "flask", "blue"));
        level3List.Add(("12th-pcm-eng-cyber", "Cyber Security Systems", "12th-pcm-eng", "After12th,AfterGraduation", "shield-alert", "blue"));
        level3List.Add(("12th-pcm-eng-cloud", "Cloud Computing Engineering", "12th-pcm-eng", "After12th,AfterGraduation", "server", "blue"));
        level3List.Add(("12th-pcm-eng-robo", "Robotics Systems Engineering", "12th-pcm-eng", "After12th,AfterGraduation", "cpu", "blue"));
        level3List.Add(("12th-pcm-eng-se", "Software Engineering Practice", "12th-pcm-eng", "After12th,AfterGraduation", "code", "blue"));
        level3List.Add(("12th-pcm-eng-elec", "Electronics Engineering", "12th-pcm-eng", "After12th,AfterGraduation", "cpu", "blue"));
        level3List.Add(("12th-pcm-eng-electrical", "Electrical Engineering", "12th-pcm-eng", "After12th,AfterGraduation", "cpu", "blue"));
        level3List.Add(("12th-pcm-eng-mech", "Mechanical Engineering", "12th-pcm-eng", "After12th,AfterGraduation", "briefcase", "blue"));
        level3List.Add(("12th-pcm-eng-civil", "Civil & Structural Engineering", "12th-pcm-eng", "After12th,AfterGraduation", "briefcase", "blue"));
        level3List.Add(("12th-pcm-eng-chem", "Chemical Engineering", "12th-pcm-eng", "After12th,AfterGraduation", "flask", "blue"));
        level3List.Add(("12th-pcm-eng-petr", "Petroleum & Gas Engineering", "12th-pcm-eng", "After12th,AfterGraduation", "flask", "blue"));
        level3List.Add(("12th-pcm-eng-aero", "Aerospace & Aeronautical Eng", "12th-pcm-eng", "After12th,AfterGraduation", "flask", "blue"));
        level3List.Add(("12th-pcm-eng-auto", "Automobile Design Engineering", "12th-pcm-eng", "After12th,AfterGraduation", "briefcase", "blue"));
        level3List.Add(("12th-pcm-eng-marine", "Marine Engineering", "12th-pcm-eng", "After12th,AfterGraduation", "briefcase", "blue"));
        level3List.Add(("12th-pcm-eng-biotech", "Biotechnology Engineering", "12th-pcm-eng", "After12th,AfterGraduation", "heart-pulse", "blue"));
        level3List.Add(("12th-pcm-eng-mecha", "Mechatronics Engineering", "12th-pcm-eng", "After12th,AfterGraduation", "cpu", "blue"));
        level3List.Add(("12th-pcm-eng-nano", "Nanotechnology Systems", "12th-pcm-eng", "After12th,AfterGraduation", "flask", "blue"));
        level3List.Add(("12th-pcm-eng-env", "Environmental Engineering", "12th-pcm-eng", "After12th,AfterGraduation", "flask", "blue"));

        // Under 12th-pcm-arch: B.Arch, Interior Design, Urban Planning
        level3List.Add(("12th-pcm-arch-barch", "B.Arch Architecture", "12th-pcm-arch", "After12th", "palette", "blue"));
        level3List.Add(("12th-pcm-arch-interior", "Interior Designing", "12th-pcm-arch", "After12th", "palette", "blue"));
        level3List.Add(("12th-pcm-arch-urban", "Urban & City Planning", "12th-pcm-arch", "After12th", "palette", "blue"));

        // Under 12th-pcm-def: NDA, Navy, Air Force, Army
        level3List.Add(("12th-pcm-def-nda", "NDA Academy Commissions", "12th-pcm-def", "After12th", "shield-alert", "blue"));
        level3List.Add(("12th-pcm-def-navy", "Indian Navy", "12th-pcm-def", "After12th", "shield-alert", "blue"));
        level3List.Add(("12th-pcm-def-iaf", "Indian Air Force", "12th-pcm-def", "After12th", "shield-alert", "blue"));
        level3List.Add(("12th-pcm-def-army", "Indian Army", "12th-pcm-def", "After12th", "shield-alert", "blue"));

        // Under 12th-pcm-av: Pilot, Aircraft Maintenance, Aviation Management
        level3List.Add(("12th-pcm-av-pilot", "Commercial & Fighter Pilot", "12th-pcm-av", "After12th", "flask", "blue"));
        level3List.Add(("12th-pcm-av-maint", "Aircraft Maintenance", "12th-pcm-av", "After12th", "flask", "blue"));
        level3List.Add(("12th-pcm-av-mgmt", "Aviation Operations Management", "12th-pcm-av", "After12th", "flask", "blue"));

        // Under 12th-pcm-res: Physics, Mathematics, Astronomy, Space Science
        level3List.Add(("12th-pcm-res-phys", "Physics Research", "12th-pcm-res", "After12th", "flask", "blue"));
        level3List.Add(("12th-pcm-res-math", "Mathematics Research", "12th-pcm-res", "After12th", "flask", "blue"));
        level3List.Add(("12th-pcm-res-astro", "Astronomy Research", "12th-pcm-res", "After12th", "flask", "blue"));
        level3List.Add(("12th-pcm-res-space", "Space Science & Technology", "12th-pcm-res", "After12th", "flask", "blue"));

        // Under 12th-pcb-med: MBBS, BDS, BAMS, BHMS, BPT, Nursing, Pharmacy, Veterinary, Biotechnology, Genetics
        level3List.Add(("12th-pcb-med-mbbs", "MBBS Doctor", "12th-pcb-med", "After12th", "heart-pulse", "red"));
        level3List.Add(("12th-pcb-med-bds", "BDS Dentist", "12th-pcb-med", "After12th", "heart-pulse", "red"));
        level3List.Add(("12th-pcb-med-bams", "BAMS Ayurvedic Practitioner", "12th-pcb-med", "After12th", "heart-pulse", "red"));
        level3List.Add(("12th-pcb-med-bhms", "BHMS Homeopathic Doctor", "12th-pcb-med", "After12th", "heart-pulse", "red"));
        level3List.Add(("12th-pcb-med-bpt", "BPT Physiotherapist", "12th-pcb-med", "After12th", "heart-pulse", "red"));
        level3List.Add(("12th-pcb-med-nurse", "B.Sc Nursing Care", "12th-pcb-med", "After12th", "heart-pulse", "red"));
        level3List.Add(("12th-pcb-med-pharm", "Pharmacy Practice", "12th-pcb-med", "After12th", "heart-pulse", "red"));
        level3List.Add(("12th-pcb-med-vet", "Veterinary Medicine", "12th-pcb-med", "After12th", "heart-pulse", "red"));
        level3List.Add(("12th-pcb-med-biotech", "Biotechnology Studies", "12th-pcb-med", "After12th", "heart-pulse", "red"));
        level3List.Add(("12th-pcb-med-genetics", "Genetics Research", "12th-pcb-med", "After12th", "heart-pulse", "red"));

        // Under 12th-pcb-hc: Radiology, Pathology, Physiotherapy, Occupational Therapy
        level3List.Add(("12th-pcb-hc-radio", "Radiology Tech", "12th-pcb-hc", "After12th", "heart-pulse", "red"));
        level3List.Add(("12th-pcb-hc-path", "Pathology Lab Diagnostics", "12th-pcb-hc", "After12th", "heart-pulse", "red"));
        level3List.Add(("12th-pcb-hc-physio", "Physiotherapy & Rehab", "12th-pcb-hc", "After12th", "heart-pulse", "red"));
        level3List.Add(("12th-pcb-hc-occup", "Occupational Therapy", "12th-pcb-hc", "After12th", "heart-pulse", "red"));

        // Under 12th-pcb-res: Biomedical Science, Neuroscience, Immunology
        level3List.Add(("12th-pcb-res-biomed", "Biomedical Science Studies", "12th-pcb-res", "After12th", "heart-pulse", "red"));
        level3List.Add(("12th-pcb-res-neuro", "Neuroscience Research", "12th-pcb-res", "After12th", "heart-pulse", "red"));
        level3List.Add(("12th-pcb-res-immuno", "Immunology & Vaccine Studies", "12th-pcb-res", "After12th", "heart-pulse", "red"));

        // Under 12th-com-fin: CA, CMA, CS, CFA, FRM, CFP
        level3List.Add(("12th-com-fin-ca", "Chartered Accountant (CA)", "12th-com-fin", "After12th,AfterGraduation", "banknote", "green"));
        level3List.Add(("12th-com-fin-cma", "Cost & Management Accountant", "12th-com-fin", "After12th", "banknote", "green"));
        level3List.Add(("12th-com-fin-cs", "Company Secretary (CS)", "12th-com-fin", "After12th", "banknote", "green"));
        level3List.Add(("12th-com-fin-cfa", "Chartered Financial Analyst", "12th-com-fin", "After12th,AfterGraduation", "banknote", "green"));
        level3List.Add(("12th-com-fin-frm", "Financial Risk Manager", "12th-com-fin", "After12th", "banknote", "green"));
        level3List.Add(("12th-com-fin-cfp", "Certified Financial Planner", "12th-com-fin", "After12th", "banknote", "green"));

        // Under 12th-com-bus: BBA, MBA, Entrepreneurship, Startup Founder
        level3List.Add(("12th-com-bus-bba", "BBA Management studies", "12th-com-bus", "After12th", "banknote", "green"));
        level3List.Add(("12th-com-bus-mba", "MBA Corporate Finance", "12th-com-bus", "After12th,AfterGraduation", "banknote", "green"));
        level3List.Add(("12th-com-bus-ent", "Business Entrepreneurship", "12th-com-bus", "After12th,AfterGraduation", "banknote", "green"));
        level3List.Add(("12th-com-bus-founder", "Startup Founder", "12th-com-bus", "After12th,AfterGraduation,CareerSwitch", "banknote", "green"));

        // Under 12th-com-bank: Investment Banking, Retail Banking
        level3List.Add(("12th-com-bank-ib", "Investment Banking Specialist", "12th-com-bank", "After12th,AfterGraduation", "banknote", "green"));
        level3List.Add(("12th-com-bank-retail", "Retail & Branch Banking", "12th-com-bank", "After12th", "banknote", "green"));

        // Under 12th-com-econ: Economist, Policy Analyst
        level3List.Add(("12th-com-econ-economist", "Professional Economist", "12th-com-econ", "After12th", "banknote", "green"));
        level3List.Add(("12th-com-econ-policy", "Economic Policy Analyst", "12th-com-econ", "After12th,AfterGraduation", "banknote", "green"));

        // Under 12th-com-acct: Auditor, Tax Consultant
        level3List.Add(("12th-com-acct-auditor", "Statutory Auditor", "12th-com-acct", "After12th", "banknote", "green"));
        level3List.Add(("12th-com-acct-tax", "Taxation Consultant", "12th-com-acct", "After12th", "banknote", "green"));

        // Under 12th-com-fintech: Financial Analyst, Wealth Manager, Quant Analyst
        level3List.Add(("12th-com-fintech-analyst", "Financial Analyst", "12th-com-fintech", "After12th,AfterGraduation", "banknote", "green"));
        level3List.Add(("12th-com-fintech-wealth", "Wealth Manager", "12th-com-fintech", "After12th", "banknote", "green"));
        level3List.Add(("12th-com-fintech-quant", "Quant Analytics Specialist", "12th-com-fintech", "After12th,AfterGraduation", "banknote", "green"));

        // Under 12th-art-psych: Clinical Psychologist, Counsellor
        level3List.Add(("12th-art-psych-clinical", "Clinical Psychologist", "12th-art-psych", "After12th,AfterGraduation", "palette", "purple"));
        level3List.Add(("12th-art-psych-counsel", "Family & Career Counsellor", "12th-art-psych", "After12th", "palette", "purple"));

        // Under 12th-art-law: Corporate Lawyer, Criminal Lawyer, Judge
        level3List.Add(("12th-art-law-corporate", "Corporate Lawyer", "12th-art-law", "After12th,AfterGraduation", "palette", "purple"));
        level3List.Add(("12th-art-law-criminal", "Criminal Litigation Lawyer", "12th-art-law", "After12th", "palette", "purple"));
        level3List.Add(("12th-art-law-judge", "Judicial Magistrate", "12th-art-law", "After12th,AfterGraduation", "palette", "purple"));

        // Under 12th-art-media: Journalism, Content Creator, Film Making
        level3List.Add(("12th-art-media-journal", "Print & Digital Journalism", "12th-art-media", "After12th", "palette", "purple"));
        level3List.Add(("12th-art-media-creator", "Digital Content Creator", "12th-art-media", "After12th,CareerSwitch", "palette", "purple"));
        level3List.Add(("12th-art-media-film", "Cinematography & Film Making", "12th-art-media", "After12th", "palette", "purple"));

        // Under 12th-art-des: UI UX, Product Design, Fashion Design, Interior Design
        level3List.Add(("12th-art-des-uiux", "UI/UX Designer", "12th-art-des", "After12th,AfterGraduation,CareerSwitch", "palette", "purple"));
        level3List.Add(("12th-art-des-product", "Industrial Product Design", "12th-art-des", "After12th", "palette", "purple"));
        level3List.Add(("12th-art-des-fashion", "Fashion & Apparel Design", "12th-art-des", "After12th", "palette", "purple"));
        level3List.Add(("12th-art-des-interior", "Commercial Interior Design", "12th-art-des", "After12th", "palette", "purple"));

        // Under 12th-art-soc: Sociology, Anthropology, Public Policy
        level3List.Add(("12th-art-soc-sociology", "Sociology Research", "12th-art-soc", "After12th", "palette", "purple"));
        level3List.Add(("12th-art-soc-anthro", "Anthropology Studies", "12th-art-soc", "After12th", "palette", "purple"));
        level3List.Add(("12th-art-soc-policy", "Public Policy Advisor", "12th-art-soc", "After12th,AfterGraduation", "palette", "purple"));

        // Under 12th-art-civil: UPSC, State PSC
        level3List.Add(("12th-art-civil-upsc", "UPSC IAS Preparation", "12th-art-civil", "After12th,AfterGraduation,GovernmentJobs", "palette", "purple"));
        level3List.Add(("12th-art-civil-state", "State Service Commission", "12th-art-civil", "After12th,AfterGraduation,GovernmentJobs", "palette", "purple"));

        // Under 12th-art-edu: Professor, Lecturer, Teacher
        level3List.Add(("12th-art-edu-prof", "University Professor", "12th-art-edu", "After12th,AfterGraduation", "palette", "purple"));
        level3List.Add(("12th-art-edu-lecturer", "College Lecturer", "12th-art-edu", "After12th", "palette", "purple"));
        level3List.Add(("12th-art-edu-teacher", "High School Teacher", "12th-art-edu", "After12th", "palette", "purple"));

        // Generate Level 3 fields for other Level 2 nodes recursively
        foreach (var l2 in level2List)
        {
            if (!level3List.Any(l3 => l3.ParentId == l2.Id))
            {
                level3List.Add(($"{l2.Id}-f1", $"{l2.Title} - Core Field", l2.Id, l2.StreamType, l2.Icon, l2.Color));
            }
        }

        // Add Level 3 paths to database
        foreach (var l3 in level3List)
        {
            AddPath(allPaths, l3.Id, l3.Title, l3.StreamType, l3.Icon, l3.Color, 3, l3.ParentId);
        }

        // Level 4: Specializations under Level 3
        var level4List = new List<(string Id, string Title, string ParentId, string StreamType, string Icon, string Color)>();

        // Under CS Engineering (12th-pcm-eng-cs): 16 specializations
        string csId = "12th-pcm-eng-cs";
        level4List.Add(($"{csId}-front", "Frontend Engineer", csId, "After12th,AfterGraduation,CareerSwitch", "code", "blue"));
        level4List.Add(($"{csId}-back", "Backend Engineer", csId, "After12th,AfterGraduation,CareerSwitch", "code", "blue"));
        level4List.Add(($"{csId}-full", "Full Stack Engineer", csId, "After12th,AfterGraduation,CareerSwitch", "code", "blue"));
        level4List.Add(($"{csId}-ai", "AI Engineer", csId, "After12th,AfterGraduation,CareerSwitch,Upskilling", "sparkles", "blue"));
        level4List.Add(($"{csId}-ml", "ML Engineer", csId, "After12th,AfterGraduation,CareerSwitch,Upskilling", "sparkles", "blue"));
        level4List.Add(($"{csId}-data", "Data Engineer", csId, "After12th,AfterGraduation,Upskilling", "flask", "blue"));
        level4List.Add(($"{csId}-cloud", "Cloud Engineer", csId, "After12th,AfterGraduation,Upskilling", "server", "blue"));
        level4List.Add(($"{csId}-devops", "DevOps Engineer", csId, "After12th,AfterGraduation,Upskilling", "server", "blue"));
        level4List.Add(($"{csId}-sre", "Site Reliability Engineer", csId, "After12th,AfterGraduation", "server", "blue"));
        level4List.Add(($"{csId}-block", "Blockchain Developer", csId, "After12th,AfterGraduation", "code", "blue"));
        level4List.Add(($"{csId}-game", "Game Developer", csId, "After12th,AfterGraduation", "code", "blue"));
        level4List.Add(($"{csId}-arvr", "AR/VR Developer", csId, "After12th,AfterGraduation", "code", "blue"));
        level4List.Add(($"{csId}-embed", "Embedded Engineer", csId, "After12th,AfterGraduation", "cpu", "blue"));
        level4List.Add(($"{csId}-prod", "Product Engineer", csId, "After12th,AfterGraduation", "briefcase", "blue"));
        level4List.Add(($"{csId}-arch", "Solution Architect", csId, "After12th,AfterGraduation", "briefcase", "blue"));
        level4List.Add(($"{csId}-cto", "CTO Path", csId, "After12th,AfterGraduation", "sparkles", "blue"));

        // Generate Level 4 paths for other Level 3 nodes recursively
        foreach (var l3 in level3List)
        {
            if (!level4List.Any(l4 => l4.ParentId == l3.Id))
            {
                level4List.Add(($"{l3.Id}-s1", $"{l3.Title} - Specialization", l3.Id, l3.StreamType, l3.Icon, l3.Color));
            }
        }

        // Add Level 4 paths to database
        foreach (var l4 in level4List)
        {
            AddPath(allPaths, l4.Id, l4.Title, l4.StreamType, l4.Icon, l4.Color, 4, l4.ParentId);
        }

        // Level 5: Job Roles under Level 4
        var level5List = new List<(string Id, string Title, string ParentId, string StreamType, string Icon, string Color)>();

        // Generate 2 job roles for every Level 4 specialization programmatically
        foreach (var l4 in level4List)
        {
            level5List.Add(($"{l4.Id}-r1", $"Lead {l4.Title}", l4.Id, l4.StreamType, l4.Icon, l4.Color));
            level5List.Add(($"{l4.Id}-r2", $"Senior {l4.Title} Architect", l4.Id, l4.StreamType, l4.Icon, l4.Color));
        }

        // Add Level 5 paths to database
        foreach (var l5 in level5List)
        {
            AddPath(allPaths, l5.Id, l5.Title, l5.StreamType, l5.Icon, l5.Color, 5, l5.ParentId);
        }

        // Populate RelatedCareerIds based on sibling nodes in Level 5
        var level5Paths = allPaths.Where(p => p.Level == 5).ToList();
        for (int i = 0; i < level5Paths.Count; i++)
        {
            var p = level5Paths[i];
            var siblings = level5Paths.Where(x => x.ParentCareerId == p.ParentCareerId && x.Id != p.Id).Select(x => x.Id).ToList();
            p.RelatedCareerIds = string.Join(",", siblings);
        }

        // Save in batches to database
        context.ChangeTracker.AutoDetectChangesEnabled = false;
        int batchSize = 100;
        for (int i = 0; i < allPaths.Count; i += batchSize)
        {
            var batch = allPaths.Skip(i).Take(batchSize);
            context.CareerPaths.AddRange(batch);
            await context.SaveChangesAsync();
        }
        context.ChangeTracker.AutoDetectChangesEnabled = true;
    }

    private static void AddPath(
        List<CareerPath> list,
        string id,
        string title,
        string streamType,
        string icon,
        string color,
        int level,
        string parentId)
    {
        string desc = $"Professional career pathway in {title} at Level {level}, highlighting growth tracks, certifications, and recommended skills.";
        string diffLevel = level switch {
            1 => "Easy",
            2 => "Medium",
            3 => "Medium",
            4 => "Hard",
            _ => "Extremely Hard"
        };
        double risk = level switch {
            1 => 15.0,
            2 => 20.0,
            3 => 25.0,
            4 => 30.0,
            _ => 10.0
        };
        double cagr = level switch {
            1 => 8.0,
            2 => 10.0,
            3 => 12.0,
            4 => 14.0,
            _ => 16.0
        };
        double demand = 70.0 + (level * 5.0);

        string salaryJson = level switch {
            1 => "{\"Starting\": 300000, \"Mid-Career\": 600000, \"Senior\": 1200000}",
            2 => "{\"Starting\": 400000, \"Mid-Career\": 800000, \"Senior\": 1600000}",
            3 => "{\"Starting\": 500000, \"Mid-Career\": 1000000, \"Senior\": 2200000}",
            4 => "{\"Starting\": 700000, \"Mid-Career\": 1400000, \"Senior\": 3000000}",
            _ => "{\"Starting\": 900000, \"Mid-Career\": 2000000, \"Senior\": 4000000}"
        };

        list.Add(new CareerPath
        {
            Id = id,
            Title = title,
            Description = desc,
            Level = level,
            ParentCareerId = string.IsNullOrEmpty(parentId) ? null : parentId,
            StreamType = streamType,
            Tags = id.Replace("-", ","),
            SkillsRequired = "[\"Communication\", \"Problem Solving\", \"Critical Thinking\"]",
            FutureScope = $"Excellent scope driven by advancements and market demand for {title}.",
            SalaryDataJson = salaryJson,
            IndustryGrowth = cagr,
            RoadmapJson = "[{\"phase\":\"Foundations\",\"duration\":\"3 Months\",\"topics\":[\"Introduction\",\"Core Concepts\"],\"projectSuggestion\":\"Simple review/presentation\"}]",
            Certifications = "Professional Standard Certifications",
            Colleges = "Top National & Global Universities",
            DemandIndex = demand,
            Icon = icon,
            Color = color,
            WhatYouWillStudy = $"Theoretical foundations and applied skills of {title}.",
            RecommendedSubjects = "[\"Core Theory\", \"Electives\"]",
            TopCompanies = "[\"MNCs\", \"Startups\", \"Public Sector\"]",
            RequiredDegrees = "[\"Bachelor's / Associate degree\"]",
            WorkEnvironment = "Hybrid",
            RemoteWorkPossibility = 45.0,
            EntrepreneurshipPotential = 50.0,
            CareerFlexibility = 60.0,
            LearningResources = "[\"Introductory Books\", \"Online Reference Course\"]",
            YoutubeResources = "[\"Educational Tutorials\", \"Crash Course\"]",
            RoadmapTimeline = "3-4 Years",
            DifficultyLevel = diffLevel,
            PersonalityMatch = "INTJ, ENTJ, INTP, ENFP",
            RelatedCareerIds = "",
            AutomationRisk = risk
        });
    }
    private static async Task SeedHierarchicalQuizQuestionsAsync(ApplicationDbContext context)
    {
        if (context.HierarchicalQuizQuestions.Any())
        {
            context.HierarchicalQuizQuestions.RemoveRange(context.HierarchicalQuizQuestions);
            await context.SaveChangesAsync();
        }

        var paths = await context.CareerPaths.ToListAsync();
        var questionsList = new List<HierarchicalQuizQuestion>();

        // Seed 3 base Level 1 questions
        var level1Questions = new List<HierarchicalQuizQuestion>
        {
            new HierarchicalQuizQuestion
            {
                Id = "hq-lvl1-1",
                QuestionText = "Which academic discipline or domain of work excites you the most when reading news or blogs?",
                HierarchyLevel = 1,
                StreamType = "After10th, After12th, AfterGraduation, AfterPostGraduation, CareerSwitch, Upskilling, Entrepreneurship, GovernmentJobs, InternationalEducation",
                AptitudeType = "Interest",
                TargetCareerTags = "science,commerce,arts,diploma",
                OptionsJson = "[" +
                              "{\"text\": \"Scientific inventions, engineering marvels, or medical discoveries.\", \"weights\": {\"science\": 3, \"pcm\": 2, \"pcb\": 2}, \"nextLevelTags\": \"science\"}," +
                              "{\"text\": \"Stock markets, finance trends, corporate mergers, or startups.\", \"weights\": {\"commerce\": 3, \"finance\": 2, \"management\": 2}, \"nextLevelTags\": \"commerce\"}," +
                              "{\"text\": \"Graphic designs, legal analyses, media reporting, or writing.\", \"weights\": {\"arts\": 3, \"creative\": 2, \"law\": 2}, \"nextLevelTags\": \"arts\"}," +
                              "{\"text\": \"Practical hands-on repairs, workshop machines, or technical polytechnics.\", \"weights\": {\"diploma\": 3, \"polytech\": 2, \"vocational\": 2}, \"nextLevelTags\": \"diploma\"}" +
                              "]"
            },
            new HierarchicalQuizQuestion
            {
                Id = "hq-lvl1-3",
                QuestionText = "How do you naturally approach problem solving or task organization?",
                HierarchyLevel = 1,
                StreamType = "After10th, After12th, AfterGraduation, AfterPostGraduation, CareerSwitch, Upskilling, Entrepreneurship, GovernmentJobs, InternationalEducation",
                AptitudeType = "Aptitude",
                TargetCareerTags = "science,commerce,arts,diploma",
                OptionsJson = "[" +
                              "{\"text\": \"By collecting data, analyzing equations, and modeling logical steps.\", \"weights\": {\"science\": 3, \"pcm\": 2}, \"nextLevelTags\": \"science\"}," +
                              "{\"text\": \"By measuring cash flows, estimating risk, and negotiating resource allocation.\", \"weights\": {\"commerce\": 3, \"finance\": 2}, \"nextLevelTags\": \"commerce\"}," +
                              "{\"text\": \"By evaluating emotional response, visual aesthetics, and cultural impact.\", \"weights\": {\"arts\": 3, \"creative\": 2}, \"nextLevelTags\": \"arts\"}," +
                              "{\"text\": \"By physically disassembling the device, testing parts, and repairing manually.\", \"weights\": {\"diploma\": 3, \"polytech\": 2}, \"nextLevelTags\": \"diploma\"}" +
                              "]"
            }
        };
        questionsList.AddRange(level1Questions);

        // For each career path at Level 2, 3, 4, 5, generate 9 questions
        var targetPaths = paths.Where(p => p.Level >= 2).ToList();

        foreach (var path in targetPaths)
        {
            var pathTags = path.Tags.Split(',', StringSplitOptions.RemoveEmptyEntries);
            string primaryTag = pathTags.LastOrDefault() ?? "general";

            // Generate 3 Interest questions
            for (int i = 1; i <= 3; i++)
            {
                questionsList.Add(new HierarchicalQuizQuestion
                {
                    Id = $"hq-g-{path.Id}-i{i}",
                    QuestionText = $"Regarding {path.Title}, what aspect of interest excites you the most (Scenario {i})?",
                    HierarchyLevel = path.Level,
                    StreamType = path.StreamType,
                    AptitudeType = "Interest",
                    TargetCareerTags = primaryTag,
                    OptionsJson = "[" +
                                  $"{{\"text\": \"Deep diving into core {path.Title} workflows and mastering its details.\", \"weights\": {{\"{primaryTag}\": 5}}, \"nextLevelTags\": \"{primaryTag}\"}}," +
                                  $"{{\"text\": \"Coordinating high-level strategies surrounding {path.Title}.\", \"weights\": {{\"{primaryTag}\": 3}}, \"nextLevelTags\": \"{primaryTag}\"}}," +
                                  $"{{\"text\": \"Working on visual layout designs and aesthetics for {path.Title}.\", \"weights\": {{\"creative\": 2, \"design\": 2}}, \"nextLevelTags\": \"design\"}}," +
                                  $"{{\"text\": \"Dealing with basic system setups, network routers, and support.\", \"weights\": {{\"diploma\": 2, \"tech\": 2}}, \"nextLevelTags\": \"diploma\"}}" +
                                  "]"
                });
            }

            // Generate 3 WorkStyle questions
            for (int i = 1; i <= 3; i++)
            {
                questionsList.Add(new HierarchicalQuizQuestion
                {
                    Id = $"hq-g-{path.Id}-w{i}",
                    QuestionText = $"In a day-to-day role in {path.Title}, how do you prefer to approach your team tasks (Scenario {i})?",
                    HierarchyLevel = path.Level,
                    StreamType = path.StreamType,
                    AptitudeType = "WorkStyle",
                    TargetCareerTags = primaryTag,
                    OptionsJson = "[" +
                                  $"{{\"text\": \"Independently focusing on technical deliverables and specifications for {path.Title}.\", \"weights\": {{\"{primaryTag}\": 5}}, \"nextLevelTags\": \"{primaryTag}\"}}," +
                                  $"{{\"text\": \"Collaborating in agile sprint planning sessions with business analysts.\", \"weights\": {{\"{primaryTag}\": 3, \"management\": 2}}, \"nextLevelTags\": \"management\"}}," +
                                  $"{{\"text\": \"Creating vector mockups, icons, and aesthetic layouts.\", \"weights\": {{\"creative\": 2, \"design\": 2}}, \"nextLevelTags\": \"design\"}}," +
                                  $"{{\"text\": \"Troubleshooting physical machines, switchboards, or server terminals.\", \"weights\": {{\"diploma\": 2, \"tech\": 2}}, \"nextLevelTags\": \"diploma\"}}" +
                                  "]"
                });
            }

            // Generate 3 Aptitude questions
            for (int i = 1; i <= 3; i++)
            {
                questionsList.Add(new HierarchicalQuizQuestion
                {
                    Id = $"hq-g-{path.Id}-a{i}",
                    QuestionText = $"If a critical block occurs during a {path.Title} project, what is your diagnostic method (Scenario {i})?",
                    HierarchyLevel = path.Level,
                    StreamType = path.StreamType,
                    AptitudeType = "Aptitude",
                    TargetCareerTags = primaryTag,
                    OptionsJson = "[" +
                                  $"{{\"text\": \"Deep diving into the underlying logic or ledger data of {path.Title}.\", \"weights\": {{\"{primaryTag}\": 5}}, \"nextLevelTags\": \"{primaryTag}\"}}," +
                                  $"{{\"text\": \"Formulating new business strategies and negotiating project scope changes.\", \"weights\": {{\"{primaryTag}\": 3, \"finance\": 2}}, \"nextLevelTags\": \"finance\"}}," +
                                  $"{{\"text\": \"Vetting user persona reviews and redesigning wireframes.\", \"weights\": {{\"creative\": 2, \"design\": 2}}, \"nextLevelTags\": \"design\"}}," +
                                  $"{{\"text\": \"Disassembling components, checking logs, and applying quick hotfixes.\", \"weights\": {{\"diploma\": 2, \"tech\": 2}}, \"nextLevelTags\": \"diploma\"}}" +
                                  "]"
                });
            }
        }

        // Save in batches
        context.ChangeTracker.AutoDetectChangesEnabled = false;
        int batchSize = 200;
        for (int i = 0; i < questionsList.Count; i += batchSize)
        {
            var batch = questionsList.Skip(i).Take(batchSize);
            context.HierarchicalQuizQuestions.AddRange(batch);
            await context.SaveChangesAsync();
        }
        context.ChangeTracker.AutoDetectChangesEnabled = true;
    }
}
