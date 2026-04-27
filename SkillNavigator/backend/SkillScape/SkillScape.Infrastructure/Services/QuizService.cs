using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.ML;
using SkillScape.Application.DTOs;
using SkillScape.Application.Interfaces;
using SkillScape.Domain.Entities;
using SkillScape.Infrastructure.Data;

namespace SkillScape.Infrastructure.Services;

public class QuizService : IQuizService
{
    private readonly ApplicationDbContext _context;
    private readonly PredictionEnginePool<QuizData, QuizPrediction> _careerPredictionEnginePool;
    private readonly PredictionEnginePool<SalaryData, SalaryPrediction> _salaryPredictionEnginePool;
    private readonly ITrendService _trendService;

    public QuizService(
        ApplicationDbContext context, 
        PredictionEnginePool<QuizData, QuizPrediction> careerPredictionEnginePool,
        PredictionEnginePool<SalaryData, SalaryPrediction> salaryPredictionEnginePool,
        ITrendService trendService)
    {
        _context = context;
        _careerPredictionEnginePool = careerPredictionEnginePool;
        _salaryPredictionEnginePool = salaryPredictionEnginePool;
        _trendService = trendService;
    }

    public async Task<List<QuizQuestionDto>> GetAllQuestionsAsync()
    {
        return await _context.QuizQuestions
            .Include(q => q.Options)
            .Where(q => q.IsActive)
            .OrderBy(q => q.DisplayOrder)
            .Select(q => new QuizQuestionDto
            {
                Id = q.Id,
                Text = q.Text,
                Category = q.Category,
                DisplayOrder = q.DisplayOrder,
                Options = q.Options
                    .OrderBy(o => o.DisplayOrder)
                    .Select(o => new QuizOptionDto
                    {
                        Id = o.Id,
                        Text = o.Text,
                        DisplayOrder = o.DisplayOrder
                    })
                    .ToList()
            })
            .ToListAsync();
    }

    public async Task<QuizQuestionDto?> GetNextQuestionAsync(List<QuizResponseRequest> currentResponses)
    {
        var answeredQuestionIds = currentResponses.Select(r => r.QuestionId).ToList();
        
        // Stop after 7 questions
        if (answeredQuestionIds.Count >= 7)
        {
            return null;
        }

        var query = _context.QuizQuestions
            .Include(q => q.Options)
            .Where(q => q.IsActive && !answeredQuestionIds.Contains(q.Id));

        // To make questions "related" to the user's choices
        if (currentResponses.Any())
        {
            var lastResponse = currentResponses.Last();
            var lastOption = await _context.QuizOptions.FindAsync(lastResponse.OptionId);
            if (lastOption != null && !string.IsNullOrEmpty(lastOption.DomainWeightJson))
            {
                try
                {
                    var weights = JsonSerializer.Deserialize<Dictionary<string, int>>(lastOption.DomainWeightJson);
                    if (weights != null && weights.Any())
                    {
                        var preferredDomain = weights.First().Key;
                        // Find questions that have at least one option related to this preferred domain
                        var relatedQuestions = await query.Where(q => q.Options.Any(o => o.DomainWeightJson.Contains(preferredDomain))).ToListAsync();
                        if (relatedQuestions.Any())
                        {
                            // Filter query to only these related questions if any exist
                            var relatedIds = relatedQuestions.Select(rq => rq.Id).ToList();
                            query = _context.QuizQuestions
                                .Include(q => q.Options)
                                .Where(q => q.IsActive && relatedIds.Contains(q.Id));
                        }
                    }
                }
                catch { }
            }
        }

        // Get a random question from the matching pool
        var nextQuestion = await query
            .OrderBy(q => Guid.NewGuid()) // Random order
            .FirstOrDefaultAsync();

        if (nextQuestion == null)
        {
            // Fallback if no related questions found, just get a random unattended one
            nextQuestion = await _context.QuizQuestions
                .Include(q => q.Options)
                .Where(q => q.IsActive && !answeredQuestionIds.Contains(q.Id))
                .OrderBy(q => Guid.NewGuid())
                .FirstOrDefaultAsync();

            if (nextQuestion == null) return null;
        }

        return new QuizQuestionDto
        {
            Id = nextQuestion.Id,
            Text = nextQuestion.Text,
            Category = nextQuestion.Category,
            DisplayOrder = nextQuestion.DisplayOrder,
            Options = nextQuestion.Options
                .OrderBy(o => o.DisplayOrder)
                .Select(o => new QuizOptionDto
                {
                    Id = o.Id,
                    Text = o.Text,
                    DisplayOrder = o.DisplayOrder
                })
                .ToList()
        };
    }

    public async Task<QuizResultDto> SubmitQuizAsync(string userId, SubmitQuizRequest request)
    {
        var user = await _context.Users.FindAsync(userId)
            ?? throw new InvalidOperationException("User not found");

        var optionTexts = new List<string>();

        foreach (var response in request.Responses)
        {
            var option = await _context.QuizOptions.FindAsync(response.OptionId)
                ?? throw new InvalidOperationException("Invalid option");
            
            optionTexts.Add(option.Text);
        }

        var combinedAnswers = string.Join(" ", optionTexts);

        // Make ML.NET Career Prediction
        var predictionData = new QuizData { CombinedAnswers = combinedAnswers };
        string recommendedDomain = "fullstack";
        try
        {
            if (_careerPredictionEnginePool != null)
            {
                var careerPrediction = _careerPredictionEnginePool.Predict(modelName: "CareerPredictor", predictionData);
                recommendedDomain = careerPrediction.PredictedDomain;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ML-ERROR] Career Prediction Failed: {ex.InnerException?.Message ?? ex.Message}");
        }

        var domainScores = new Dictionary<string, int>
        {
            { recommendedDomain, 100 }
        };

        var domain = await _context.CareerDomains
            .Include(d => d.Skills)
            .FirstOrDefaultAsync(d => d.Id == recommendedDomain);

        if (domain == null)
        {
            // Fallback if prediction fails or returns weird domain
            recommendedDomain = "fullstack";
            domain = await _context.CareerDomains.Include(d => d.Skills).FirstOrDefaultAsync(d => d.Id == recommendedDomain);
            
            if (domain == null)
            {
                domain = await _context.CareerDomains.Include(d => d.Skills).FirstOrDefaultAsync();
                if (domain != null)
                {
                    recommendedDomain = domain.Id;
                }
                else
                {
                    throw new InvalidOperationException("No Career Domains found in database.");
                }
            }
        }

        // Predict Salary Range using SalaryPredictor (0 years exp to 5 years exp)
        string salaryRange = "$0 - $0";
        try
        {
            if (_salaryPredictionEnginePool != null)
            {
                var lowSalaryPred = _salaryPredictionEnginePool.Predict(modelName: "SalaryPredictor", new SalaryData { Domain = recommendedDomain, YearsOfExperience = 0 });
                var highSalaryPred = _salaryPredictionEnginePool.Predict(modelName: "SalaryPredictor", new SalaryData { Domain = recommendedDomain, YearsOfExperience = 5 });
                salaryRange = $"${(int)Math.Abs(lowSalaryPred.PredictedSalary):#,##0} - ${(int)Math.Abs(highSalaryPred.PredictedSalary):#,##0}";
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ML-ERROR] Salary Prediction Failed: {ex.InnerException?.Message ?? ex.Message}");
            salaryRange = "$70,000 - $140,000";
        }

        // Get StackOverflow 5-year Trends
        var trendMap = await _trendService.GetFiveYearTrendAsync(recommendedDomain);

        // domainScores is already computed above

        var result = new QuizResult
        {
            Id = Guid.NewGuid().ToString(),
            UserId = userId,
            RecommendedDomainId = recommendedDomain,
            ScoresJson = JsonSerializer.Serialize(domainScores),
            RecommendationReason = GenerateRecommendationReason(domain),
            CompletedAt = DateTime.UtcNow
        };

        var skillsList = domain.Skills.Select(s => s.Name).Take(8).ToList();

        // Do not save to DB yet. Just preview.
        return MapToResultDto(result, domain, domainScores, skillsList, salaryRange, trendMap);
    }

    public async Task<QuizResultDto> ConfirmQuizAsync(string userId, string recommendedDomainId)
    {
        var domain = await _context.CareerDomains
            .Include(d => d.Skills)
            .FirstOrDefaultAsync(d => d.Id == recommendedDomainId)
            ?? throw new InvalidOperationException("Domain not found");

        var domainScores = new Dictionary<string, int>
        {
            { recommendedDomainId, 100 }
        };

        var result = new QuizResult
        {
            Id = Guid.NewGuid().ToString(),
            UserId = userId,
            RecommendedDomainId = recommendedDomainId,
            ScoresJson = JsonSerializer.Serialize(domainScores),
            RecommendationReason = GenerateRecommendationReason(domain),
            CompletedAt = DateTime.UtcNow
        };

        _context.QuizResults.Add(result);

        // Assign top skills to the user automatically when they accept a path
        var topSkills = domain.Skills.Take(5).ToList();
        foreach (var skill in topSkills)
        {
            // Check if user already has it tracking
            var existingProgress = await _context.UserSkills
                .FirstOrDefaultAsync(us => us.UserId == userId && us.SkillId == skill.Id);
            
            if (existingProgress == null)
            {
                _context.UserSkills.Add(new UserSkill
                {
                    UserId = userId,
                    SkillId = skill.Id,
                    IsCompleted = false,
                    IsCleared = false,
                    ProgressPercentage = 0,
                    StartedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                });
            }
        }

        await _context.SaveChangesAsync();

        // Predict Salary Range
        string salaryRange = "$0 - $0";
        try
        {
            if (_salaryPredictionEnginePool != null)
            {
                var lowSalaryPred = _salaryPredictionEnginePool.Predict(modelName: "SalaryPredictor", new SalaryData { Domain = recommendedDomainId, YearsOfExperience = 0 });
                var highSalaryPred = _salaryPredictionEnginePool.Predict(modelName: "SalaryPredictor", new SalaryData { Domain = recommendedDomainId, YearsOfExperience = 5 });
                salaryRange = $"${(int)Math.Abs(lowSalaryPred.PredictedSalary):#,##0} - ${(int)Math.Abs(highSalaryPred.PredictedSalary):#,##0}";
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ML-ERROR] Salary Prediction Failed: {ex.InnerException?.Message ?? ex.Message}");
            salaryRange = "$70,000 - $140,000";
        }

        var trendMap = await _trendService.GetFiveYearTrendAsync(recommendedDomainId);
        var skillsList = domain.Skills.Select(s => s.Name).Take(8).ToList();

        return MapToResultDto(result, domain, domainScores, skillsList, salaryRange, trendMap);
    }

    public async Task<QuizResultDto> GetLastResultAsync(string userId)
    {
        var result = await _context.QuizResults
            .Include(r => r.RecommendedDomain)
            .ThenInclude(d => d!.Skills)
            .Where(r => r.UserId == userId)
            .OrderByDescending(r => r.CompletedAt)
            .FirstOrDefaultAsync()
            ?? throw new InvalidOperationException("No quiz result found");

        var scores = JsonSerializer.Deserialize<Dictionary<string, int>>(result.ScoresJson) ?? new();

        var domainId = result.RecommendedDomainId;
        string salaryRange = "$0 - $0";
        try
        {
            if (_salaryPredictionEnginePool != null)
            {
                var lowSalaryPred = _salaryPredictionEnginePool.Predict(modelName: "SalaryPredictor", new SalaryData { Domain = domainId, YearsOfExperience = 0 });
                var highSalaryPred = _salaryPredictionEnginePool.Predict(modelName: "SalaryPredictor", new SalaryData { Domain = domainId, YearsOfExperience = 5 });
                salaryRange = $"${(int)Math.Abs(lowSalaryPred.PredictedSalary):#,##0} - ${(int)Math.Abs(highSalaryPred.PredictedSalary):#,##0}";
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ML-ERROR] Salary Prediction Failed: {ex.InnerException?.Message ?? ex.Message}");
            salaryRange = "$70,000 - $140,000";
        }

        var trendMap = await _trendService.GetFiveYearTrendAsync(domainId);
        var skillsList = result.RecommendedDomain?.Skills.Select(s => s.Name).Take(8).ToList() ?? new List<string>();

        return MapToResultDto(result, result.RecommendedDomain!, scores, skillsList, salaryRange, trendMap);
    }

    private static QuizResultDto MapToResultDto(QuizResult result, CareerDomain domain, Dictionary<string, int> scores, List<string> skills, string salaryRange, Dictionary<int, int> trendMap)
    {
        return new QuizResultDto
        {
            Id = result.Id,
            RecommendedDomainId = result.RecommendedDomainId,
            RecommendedDomainName = domain.Name,
            Scores = scores,
            RecommendationReason = result.RecommendationReason,
            RecommendedSkills = skills,
            SalaryRange = salaryRange,
            FiveYearTrend = trendMap,
            CompletedAt = result.CompletedAt
        };
    }

    private static string GenerateRecommendationReason(CareerDomain domain)
    {
        return $"Based on our machine learning analysis of your unique answers, you're strongly suited for {domain.Name}!";
    }
}
