using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.ML;
using Moq;
using SkillScape.Application.DTOs;
using SkillScape.Application.Interfaces;
using SkillScape.Domain.Entities;
using SkillScape.Infrastructure.Data;
using SkillScape.Infrastructure.Services;
using System.Text.Json;
using Xunit;

namespace SkillScape.Tests;

/// <summary>
/// Unit tests for QuizService
/// </summary>
public class QuizServiceTests
{
    private ApplicationDbContext GetInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }

    [Fact]
    public async Task GetAllQuestions_ReturnsActiveQuestions()
    {
        // Arrange
        var context = GetInMemoryDbContext();
        var question = new QuizQuestion
        {
            Id = "q1",
            Text = "Test Question",
            Category = "Test",
            IsActive = true,
            DisplayOrder = 1,
            CareerDomainId = "domain1"
        };
        context.QuizQuestions.Add(question);
        context.SaveChanges();

        var m3 = new Mock<ITrendService>();

        var service = new QuizService(context, null, null, m3.Object);

        // Act
        var result = await service.GetAllQuestionsAsync();

        // Assert
        Assert.Single(result);
        Assert.Equal("Test Question", result[0].Text);
    }

    [Fact]
    public async Task SubmitQuiz_CalculatesCorrectDomainScores()
    {
        // Arrange
        var context = GetInMemoryDbContext();
        
        // Setup domain
        var domain = new CareerDomain
        {
            Id = "fullstack",
            Name = "Full Stack",
            Description = "Full stack development",
            DisplayOrder = 1,
            Color = "purple",
            Skills = new List<Skill>()
        };
        context.CareerDomains.Add(domain);

        // Setup user
        var user = new ApplicationUser
        {
            Id = "user1",
            Email = "test@test.com",
            FullName = "Test User",
            Role = "Student"
        };
        context.Users.Add(user);

        // Setup quiz
        var question = new QuizQuestion
        {
            Id = "q1",
            Text = "Test",
            Category = "Test",
            IsActive = true,
            DisplayOrder = 1,
            CareerDomainId = "fullstack"
        };
        context.QuizQuestions.Add(question);

        var option = new QuizOption
        {
            Id = "o1",
            QuizQuestionId = "q1",
            Text = "Option 1",
            DomainWeightJson = JsonSerializer.Serialize(new { fullstack = 5 }),
            DisplayOrder = 1
        };
        context.QuizOptions.Add(option);
        context.SaveChanges();

        var m3 = new Mock<ITrendService>();
        m3.Setup(s => s.GetFiveYearTrendAsync(It.IsAny<string>())).ReturnsAsync(new Dictionary<int, int>());

        var service = new QuizService(context, null, null, m3.Object);

        // Act
        var request = new SubmitQuizRequest
        {
            Responses = new()
            {
                new QuizResponseRequest { QuestionId = "q1", OptionId = "o1" }
            }
        };
        var result = await service.SubmitQuizAsync("user1", request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("fullstack", result.RecommendedDomainId);
        Assert.Equal("Full Stack", result.RecommendedDomainName);
    }

    [Fact]
    public async Task SubmitQuiz_ThrowsExceptionForInvalidUser()
    {
        // Arrange
        var context = GetInMemoryDbContext();
        var m3 = new Mock<ITrendService>();
        var service = new QuizService(context, null, null, m3.Object);

        var request = new SubmitQuizRequest { Responses = new() };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.SubmitQuizAsync("invalid_user", request));
    }
}
