using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SkillScape.Application.DTOs;
using SkillScape.Application.Interfaces;

namespace SkillScape.Infrastructure.Services;

/// <summary>
/// Service for integrating with Groq API - Primary AI Provider
/// Groq provides faster inference than traditional APIs
/// </summary>
public class GroqService : IOpenAIService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<GroqService> _logger;

    private const string GroqApiUrl = "https://api.groq.com/openai/v1/chat/completions";

    public GroqService(HttpClient httpClient, IConfiguration configuration, ILogger<GroqService> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
    }

    /// <summary>
    /// Send a message to Groq API and get a response
    /// Groq specializes in fast inference with multiple model options
    /// </summary>
    public async Task<string> GetResponseAsync(string systemPrompt, string userMessage, List<ConversationContext>? conversationHistory = null)
    {
        try
        {
            // Try to get API key from environment variable first
            var apiKey = Environment.GetEnvironmentVariable("GROQ_API_KEY")
                ?? _configuration["Groq:ApiKey"];

            if (string.IsNullOrEmpty(apiKey))
            {
                _logger.LogWarning("Groq API key not configured. Attempting to fall back to OpenAI...");
                throw new InvalidOperationException("Groq API key is not configured");
            }

            var messages = new List<Dictionary<string, string>>();

            // Add system prompt
            messages.Add(new Dictionary<string, string>
            {
                { "role", "system" },
                { "content", systemPrompt }
            });

            // Add conversation history
            if (conversationHistory != null && conversationHistory.Any())
            {
                foreach (var context in conversationHistory.TakeLast(10)) // Limit to last 10 messages
                {
                    messages.Add(new Dictionary<string, string>
                    {
                        { "role", context.Role },
                        { "content", context.Content }
                    });
                }
            }

            // Add current user message
            messages.Add(new Dictionary<string, string>
            {
                { "role", "user" },
                { "content", userMessage }
            });

            // Groq API Request
            // Available models: mixtral-8x7b-32768, llama2-70b-4096, gemma-7b-it
            var request = new
            {
                model = "llama-3.1-8b-instant", // Using updated Llama3.1 model
                messages = messages,
                temperature = 0.7,
                max_tokens = 2000,
                top_p = 0.9
            };

            var content = new StringContent(
                JsonSerializer.Serialize(request),
                System.Text.Encoding.UTF8,
                "application/json"
            );

            _httpClient.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);

            _logger.LogInformation("Sending request to Groq API with model: llama-3.1-8b-instant");

            var response = await _httpClient.PostAsync(GroqApiUrl, content);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError($"Groq API error: {response.StatusCode} - {errorContent}");
                throw new HttpRequestException($"Groq API returned status code: {response.StatusCode}");
            }

            var responseContent = await response.Content.ReadAsStringAsync();
            var jsonResponse = JsonDocument.Parse(responseContent);

            var messageContent = jsonResponse.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString();

            _logger.LogInformation("Successfully received response from Groq API");

            return messageContent ?? "Unable to generate response";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling Groq API");
            throw;
        }
    }

    /// <summary>
    /// Extract structured data from text using Groq
    /// </summary>
    public async Task<string> ExtractStructuredDataAsync(string text, string extractionSchema)
    {
        try
        {
            var systemPrompt = $@"You are a JSON extraction expert. Extract structured data from the provided text according to the following schema:
{extractionSchema}

Return ONLY valid JSON that matches the schema. Do not include any explanations or markdown formatting.";

            return await GetResponseAsync(systemPrompt, text);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting structured data with Groq");
            throw;
        }
    }
}
