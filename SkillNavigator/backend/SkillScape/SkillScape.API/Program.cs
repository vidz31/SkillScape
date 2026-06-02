using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using MongoDB.Driver;
using Microsoft.Extensions.Options;
using SkillScape.Infrastructure.Data;
using SkillScape.Application.Interfaces;
using SkillScape.Infrastructure.Services;
using SkillScape.Application.Configuration;
using SkillScape.API.Hubs;
using SkillScape.API.Data;
using SkillScape.API.Middleware;
using FluentValidation;
using Microsoft.Extensions.ML;

// Load environment variables from .env file before the configuration is built
void LoadEnvFile(string path)
{
    if (File.Exists(path))
    {
        var lines = File.ReadAllLines(path);
        foreach (var line in lines)
        {
            if (!string.IsNullOrWhiteSpace(line) && !line.StartsWith("#"))
            {
                var parts = line.Split('=', 2);
                if (parts.Length == 2)
                {
                    Environment.SetEnvironmentVariable(parts[0].Trim(), parts[1].Trim());
                }
            }
        }
    }
}

// Try current directory
LoadEnvFile(Path.Combine(Directory.GetCurrentDirectory(), ".env"));
// Try parent directory (common for solution-level .env)
LoadEnvFile(Path.Combine(Directory.GetParent(Directory.GetCurrentDirectory())?.FullName ?? "", ".env"));

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
});

// Add AutoMapper
builder.Services.AddAutoMapper(typeof(Program).Assembly);

// Add FluentValidation
builder.Services.AddValidatorsFromAssemblies(new[] { typeof(Program).Assembly, typeof(SkillScape.Application.DTOs.LoginRequest).Assembly });

// Add Entity Framework
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

// Add MongoDB
builder.Services.Configure<MongoDbSettings>(builder.Configuration.GetSection("MongoDbSettings"));
builder.Services.AddSingleton<IMongoClient>(provider =>
{
    var settings = provider.GetRequiredService<IOptions<MongoDbSettings>>().Value;
    return new MongoClient(settings.ConnectionString);
});
builder.Services.AddSingleton(provider =>
{
    var settings = provider.GetRequiredService<IOptions<MongoDbSettings>>().Value;
    return provider.GetRequiredService<IMongoClient>().GetDatabase(settings.DatabaseName);
});
builder.Services.AddScoped<MongoDbContext>();

// Add Application Services
builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("JwtSettings"));
builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("EmailSettings"));
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IDomainService, DomainService>();
builder.Services.AddScoped<IQuizService, QuizService>();
builder.Services.AddScoped<IAiPromptService, AiPromptService>();
builder.Services.AddScoped<IRoadmapService, RoadmapService>();
builder.Services.AddScoped<IProgressService, ProgressService>();
builder.Services.AddScoped<IMentorService, MentorService>();
builder.Services.AddScoped<IAdminService, AdminService>();
builder.Services.AddScoped<IResumeService, ResumeService>();
builder.Services.AddScoped<ICareerGuidanceService, CareerGuidanceService>();
builder.Services.AddHttpClient<ITrendService, TrendService>();

// Add ChatBot Services - Using Groq as Primary, OpenAI as Fallback
builder.Services.AddScoped<IChatBotService, ChatBotService>();
builder.Services.AddScoped<IKnowledgeBaseService>(provider =>
{
    var logger = provider.GetRequiredService<ILogger<KnowledgeBaseService>>();
    var knowledgeBasePath = Path.GetFullPath(
        Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "SkillScape.Infrastructure", "KnowledgeBase"));
    logger.LogInformation($"Knowledge Base Path: {knowledgeBasePath}");
    if (!Directory.Exists(knowledgeBasePath))
    {
        logger.LogWarning($"Knowledge Base directory does not exist: {knowledgeBasePath}");
    }
    return new KnowledgeBaseService(logger, knowledgeBasePath);
});

// Register Groq as primary AI service (faster inference)
builder.Services.AddHttpClient<GroqService>();
builder.Services.AddScoped<IOpenAIService>(provider =>
    provider.GetRequiredService<GroqService>());

// Register OpenAI service for fallback
builder.Services.AddHttpClient<OpenAIService>();

// Register Gemini service
builder.Services.AddHttpClient<GeminiService>();

// Configure SignalR
builder.Services.AddSignalR();

// Add ML.NET Prediction Engine
builder.Services.AddPredictionEnginePool<QuizData, QuizPrediction>()
    .FromFile(modelName: "CareerPredictor", filePath: "CareerPredictor.zip", watchForChanges: true);

builder.Services.AddPredictionEnginePool<SalaryData, SalaryPrediction>()
    .FromFile(modelName: "SalaryPredictor", filePath: "SalaryPredictor.zip", watchForChanges: true);

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins(
            "http://localhost:5173",
            "http://127.0.0.1:5173",
            "http://localhost:3000",
            "http://127.0.0.1:3000",
            "http://localhost:8080",
            "http://127.0.0.1:8080",
            "http://localhost:8081",
            "http://127.0.0.1:8081"
        )
        .AllowAnyMethod()
        .AllowAnyHeader()
        .AllowCredentials();
    });
});

// Add JWT Authentication
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = jwtSettings["SecretKey"];

if (string.IsNullOrEmpty(secretKey))
    throw new InvalidOperationException("JWT SecretKey is not configured");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;
                if (!string.IsNullOrEmpty(accessToken) && 
                    (path.StartsWithSegments("/hubs/chat") || path.StartsWithSegments("/hubs/peer-rooms")))
                {
                    context.Token = accessToken;
                }
                return Task.CompletedTask;
            }
        };
    });

// Add Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Custom middleware
app.UseMiddleware<LoggingMiddleware>();
app.UseMiddleware<ExceptionHandlingMiddleware>();

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseCors("AllowFrontend");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// Map SignalR Hubs
app.MapHub<ChatHub>("/hubs/chat");
app.MapHub<PeerRoomHub>("/hubs/peer-rooms");

// Database initialization
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var context = services.GetRequiredService<ApplicationDbContext>();
    context.Database.Migrate();
    
    // Seed sample data
    await DatabaseSeeder.SeedAsync(context);
}

app.Run();
