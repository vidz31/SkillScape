using SkillScape.Domain.Entities;
using SkillScape.Infrastructure.Data;
using System.Text.Json;

namespace SkillScape.API.Data;

public static class DatabaseSeeder
{
    public static async Task SeedAsync(ApplicationDbContext context)
    {
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
}
