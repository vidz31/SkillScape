using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SkillScape.Application.DTOs;
using SkillScape.Application.Interfaces;

namespace SkillScape.Infrastructure.Services;

/// <summary>
/// Service for integrating with Google Gemini API
/// </summary>
public class GeminiService : IOpenAIService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<GeminiService> _logger;

    public GeminiService(HttpClient httpClient, IConfiguration configuration, ILogger<GeminiService> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<string> GetResponseAsync(string systemPrompt, string userMessage, List<ConversationContext>? conversationHistory = null)
    {
        var modelsToTry = new[] { "gemini-1.5-flash", "gemini-1.5-pro", "gemini-1.5-flash-latest", "gemini-1.5-pro-latest", "gemini-pro" };
        var lastError = "";

        foreach (var modelId in modelsToTry)
        {
            try
            {
                var apiKey = (Environment.GetEnvironmentVariable("GEMINI_API_KEY")
                    ?? _configuration["Gemini:ApiKey"])?.Trim();

                if (string.IsNullOrEmpty(apiKey))
                {
                    throw new InvalidOperationException("Gemini API key is not configured");
                }

                var messages = new List<object>();

                // Add conversation history
                if (conversationHistory != null && conversationHistory.Any())
                {
                    foreach (var context in conversationHistory.TakeLast(10))
                    {
                        messages.Add(new
                        {
                            role = context.Role == "assistant" ? "model" : "user",
                            parts = new[] { new { text = context.Content } }
                        });
                    }
                }

                // Add current message with system prompt combined or as separate if supported
                // For simplicity and stability with various Gemini versions, we'll combine system prompt with the user message if it's the first message, 
                // or keep it as a preamble.
                
                var userContent = string.IsNullOrEmpty(systemPrompt) 
                    ? userMessage 
                    : $"[System Instruction]\n{systemPrompt}\n\n[User Message]\n{userMessage}";

                messages.Add(new
                {
                    role = "user",
                    parts = new[] { new { text = userContent } }
                });

                var requestBody = new
                {
                    contents = messages,
                    generationConfig = new {
                        temperature = 0.7,
                        maxOutputTokens = 1500
                    }
                };

                // Use v1 endpoint as it's more stable for these models
                var url = $"https://generativelanguage.googleapis.com/v1beta/models/{modelId}:generateContent?key={apiKey}";
                var content = new StringContent(JsonSerializer.Serialize(requestBody), System.Text.Encoding.UTF8, "application/json");

                _logger.LogInformation($"Trying Gemini model: {modelId}");
                var response = await _httpClient.PostAsync(url, content);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    using var jsonDoc = JsonDocument.Parse(responseContent);
                    return jsonDoc.RootElement
                        .GetProperty("candidates")[0]
                        .GetProperty("content")
                        .GetProperty("parts")[0]
                        .GetProperty("text")
                        .GetString() ?? "Empty response";
                }

                lastError = $"Model {modelId} failed: {response.StatusCode} - {responseContent}";
                _logger.LogWarning(lastError);
            }
            catch (Exception ex)
            {
                lastError = $"Error with {modelId}: {ex.Message}";
                _logger.LogWarning(ex, lastError);
            }
        }

        throw new HttpRequestException($"All Gemini models failed. Last error: {lastError}");
    }

    public async Task<string> ExtractStructuredDataAsync(string text, string extractionSchema)
    {
        var prompt = $"Extract JSON for schema: {extractionSchema}. Text: {text}";
        return await GetResponseAsync("You are a JSON extractor.", prompt);
    }
}
