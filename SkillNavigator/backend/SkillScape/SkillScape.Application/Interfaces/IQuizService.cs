using SkillScape.Application.DTOs;

namespace SkillScape.Application.Interfaces;

/// <summary>
/// Quiz service interface
/// </summary>
public interface IQuizService
{
    Task<List<QuizQuestionDto>> GetAllQuestionsAsync();
    Task<QuizQuestionDto?> GetNextQuestionAsync(List<QuizResponseRequest> currentResponses);
    Task<QuizResultDto> SubmitQuizAsync(string userId, SubmitQuizRequest request);
    Task<QuizResultDto> ConfirmQuizAsync(string userId, string recommendedDomainId);
    Task<QuizResultDto> GetLastResultAsync(string userId);
}



