using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using MongoDB.Driver;
using SkillScape.Application.DTOs;
using SkillScape.Application.Interfaces;
using SkillScape.Domain.Entities;
using SkillScape.Infrastructure.Data;

namespace SkillScape.Infrastructure.Services;

public class AuthService : IAuthService
{
    private readonly ApplicationDbContext _context;
    private readonly MongoDbContext _mongoContext;
    private readonly ITokenService _tokenService;
    private readonly IEmailService _emailService;

    public AuthService(
        ApplicationDbContext context,
        ITokenService tokenService,
        IEmailService emailService,
        MongoDbContext mongoContext)
    {
        _context = context;
        _tokenService = tokenService;
        _emailService = emailService;
        _mongoContext = mongoContext;
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request)
    {
        var user = await GetMongoUserByEmailAsync(request.Email);

        if (user == null)
            throw new UnauthorizedAccessException("Invalid email or password");

        if (user.IsBlocked)
            throw new UnauthorizedAccessException($"Your account is blocked. {user.BlockedReason}");

        if (string.IsNullOrEmpty(user.PasswordHash) || !VerifyPassword(request.Password, user.PasswordHash))
            throw new UnauthorizedAccessException("Invalid email or password");

        var sqlUser = await _context.Users.FindAsync(user.Id);
        if (sqlUser == null)
        {
            sqlUser = MapToSqlUser(user);
            _context.Users.Add(sqlUser);
            await _context.SaveChangesAsync();
        }

        var token = _tokenService.GenerateAccessToken(user.Id, user.Email, user.Role);

        return new AuthResponse
        {
            Id = sqlUser.Id,
            Email = sqlUser.Email,
            FullName = sqlUser.FullName,
            Role = sqlUser.Role,
            ProfileCompleted = sqlUser.ProfileCompleted,
            Token = token,
            ExpiresAt = DateTime.UtcNow.AddMinutes(60)
        };
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
    {
        if (request.Password != request.ConfirmPassword)
            throw new InvalidOperationException("Passwords do not match");

        var existingUser = await GetMongoUserByEmailAsync(request.Email);
        if (existingUser != null)
            throw new InvalidOperationException("Email already registered");

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid().ToString(),
            Email = request.Email,
            FullName = request.FullName,
            Role = request.Role,
            ProfileCompleted = false,
            IsBlocked = false,
            BlockedReason = null,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            IsActive = true,
            PasswordHash = HashPassword(request.Password)
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        try
        {
            await _mongoContext.Users.InsertOneAsync(user);
        }
        catch
        {
            _context.Users.Remove(user);
            await _context.SaveChangesAsync();
            throw;
        }

        var token = _tokenService.GenerateAccessToken(user.Id, user.Email, user.Role);

        return new AuthResponse
        {
            Id = user.Id,
            Email = user.Email,
            FullName = user.FullName,
            Role = user.Role,
            ProfileCompleted = user.ProfileCompleted,
            Token = token,
            ExpiresAt = DateTime.UtcNow.AddMinutes(60)
        };
    }

    public async Task<UserProfileDto> GetCurrentUserAsync(string userId)
    {
        var user = await _context.Users.FindAsync(userId)
            ?? throw new InvalidOperationException("User not found");

        return MapToProfileDto(user);
    }

    public async Task<UserProfileDto> UpdateProfileAsync(string userId, UpdateProfileRequest request)
    {
        var user = await _context.Users.FindAsync(userId)
            ?? throw new InvalidOperationException("User not found");

        user.FullName = request.FullName;
        user.Bio = request.Bio;
        user.ProfileImageUrl = request.ProfileImageUrl;
        user.UpdatedAt = DateTime.UtcNow;

        _context.Users.Update(user);
        await _context.SaveChangesAsync();

        var update = Builders<ApplicationUser>.Update
            .Set(u => u.FullName, user.FullName)
            .Set(u => u.Bio, user.Bio)
            .Set(u => u.ProfileImageUrl, user.ProfileImageUrl)
            .Set(u => u.UpdatedAt, user.UpdatedAt);

        await _mongoContext.Users.UpdateOneAsync(u => u.Id == userId, update);

        return MapToProfileDto(user);
    }

    public async Task ChangePasswordAsync(string userId, ChangePasswordRequest request)
    {
        if (request.NewPassword != request.ConfirmPassword)
            throw new InvalidOperationException("Passwords do not match");

        var user = await _context.Users.FindAsync(userId)
            ?? throw new InvalidOperationException("User not found");

        user.UpdatedAt = DateTime.UtcNow;
        _context.Users.Update(user);
        await _context.SaveChangesAsync();

        var passwordHash = HashPassword(request.NewPassword);
        var update = Builders<ApplicationUser>.Update
            .Set(u => u.PasswordHash, passwordHash)
            .Set(u => u.UpdatedAt, user.UpdatedAt)
            .Set(u => u.PasswordResetToken, null)
            .Set(u => u.PasswordResetTokenExpiry, null);

        await _mongoContext.Users.UpdateOneAsync(u => u.Id == userId, update);
    }

    public async Task DeleteAccountAsync(string userId)
    {
        var user = await _context.Users.FindAsync(userId)
            ?? throw new InvalidOperationException("User not found");

        _context.Users.Remove(user);
        await _context.SaveChangesAsync();

        await _mongoContext.Users.DeleteOneAsync(u => u.Id == userId);
    }

    public async Task<string> ForgotPasswordAsync(ForgotPasswordRequest request)
    {
        var user = await GetMongoUserByEmailAsync(request.Email);

        if (user == null)
        {
            return "If an account exists with this email, a reset link has been sent.";
        }

        var token = Guid.NewGuid().ToString("N");
        user.PasswordResetToken = token;
        user.PasswordResetTokenExpiry = DateTime.UtcNow.AddHours(1);
        user.UpdatedAt = DateTime.UtcNow;

        var update = Builders<ApplicationUser>.Update
            .Set(u => u.PasswordResetToken, user.PasswordResetToken)
            .Set(u => u.PasswordResetTokenExpiry, user.PasswordResetTokenExpiry)
            .Set(u => u.UpdatedAt, user.UpdatedAt);

        await _mongoContext.Users.UpdateOneAsync(u => u.Id == user.Id, update);

        var sqlUser = await _context.Users.FindAsync(user.Id);
        if (sqlUser != null)
        {
            sqlUser.PasswordResetToken = user.PasswordResetToken;
            sqlUser.PasswordResetTokenExpiry = user.PasswordResetTokenExpiry;
            sqlUser.UpdatedAt = user.UpdatedAt;
            _context.Users.Update(sqlUser);
            await _context.SaveChangesAsync();
        }

        try
        {
            await _emailService.SendPasswordResetEmailAsync(user.Email, user.FullName, token);
            return "If an account exists with this email, a reset link has been sent.";
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to send email: {ex.Message}");
            return "If an account exists with this email, a reset link has been sent.";
        }
    }

    public async Task ResetPasswordAsync(ResetPasswordRequest request)
    {
        var user = await _mongoContext.Users.Find(u => u.PasswordResetToken == request.Token).FirstOrDefaultAsync();

        if (user == null || user.PasswordResetTokenExpiry == null || user.PasswordResetTokenExpiry < DateTime.UtcNow)
            throw new InvalidOperationException("Invalid or expired reset token");

        var newHash = HashPassword(request.Password);
        var update = Builders<ApplicationUser>.Update
            .Set(u => u.PasswordHash, newHash)
            .Set(u => u.PasswordResetToken, null)
            .Set(u => u.PasswordResetTokenExpiry, null)
            .Set(u => u.UpdatedAt, DateTime.UtcNow);

        await _mongoContext.Users.UpdateOneAsync(u => u.Id == user.Id, update);

        var sqlUser = await _context.Users.FindAsync(user.Id);
        if (sqlUser != null)
        {
            sqlUser.PasswordResetToken = null;
            sqlUser.PasswordResetTokenExpiry = null;
            sqlUser.UpdatedAt = DateTime.UtcNow;
            _context.Users.Update(sqlUser);
            await _context.SaveChangesAsync();
        }
    }

    private static UserProfileDto MapToProfileDto(ApplicationUser user)
    {
        return new UserProfileDto
        {
            Id = user.Id,
            Email = user.Email,
            FullName = user.FullName,
            Bio = user.Bio,
            ProfileImageUrl = user.ProfileImageUrl,
            Role = user.Role,
            ProfileCompleted = user.ProfileCompleted,
            Level = user.Level,
            TotalXP = user.TotalXP,
            CurrentStreak = user.CurrentStreak,
            CreatedAt = user.CreatedAt
        };
    }

    private async Task<ApplicationUser?> GetMongoUserByEmailAsync(string email)
    {
        return await _mongoContext.Users.Find(u => u.Email == email).FirstOrDefaultAsync();
    }

    private async Task<ApplicationUser?> GetMongoUserByIdAsync(string userId)
    {
        return await _mongoContext.Users.Find(u => u.Id == userId).FirstOrDefaultAsync();
    }

    private static string HashPassword(string password)
    {
        using var algorithm = new Rfc2898DeriveBytes(password, 16, 100_000, HashAlgorithmName.SHA256);
        var salt = algorithm.Salt;
        var hash = algorithm.GetBytes(32);
        return Convert.ToBase64String(salt) + "." + Convert.ToBase64String(hash);
    }

    private static bool VerifyPassword(string password, string storedHash)
    {
        var parts = storedHash.Split('.', 2);
        if (parts.Length != 2)
            return false;

        var salt = Convert.FromBase64String(parts[0]);
        var storedBytes = Convert.FromBase64String(parts[1]);

        using var algorithm = new Rfc2898DeriveBytes(password, salt, 100_000, HashAlgorithmName.SHA256);
        var computedHash = algorithm.GetBytes(32);
        return CryptographicOperations.FixedTimeEquals(storedBytes, computedHash);
    }

    private static ApplicationUser MapToSqlUser(ApplicationUser mongoUser)
    {
        return new ApplicationUser
        {
            Id = mongoUser.Id,
            Email = mongoUser.Email,
            FullName = mongoUser.FullName,
            Bio = mongoUser.Bio,
            ProfileImageUrl = mongoUser.ProfileImageUrl,
            Role = mongoUser.Role,
            ProfileCompleted = mongoUser.ProfileCompleted,
            Level = mongoUser.Level,
            TotalXP = mongoUser.TotalXP,
            CurrentStreak = mongoUser.CurrentStreak,
            CreatedAt = mongoUser.CreatedAt,
            UpdatedAt = mongoUser.UpdatedAt,
            IsActive = mongoUser.IsActive,
            IsBlocked = mongoUser.IsBlocked,
            BlockedReason = mongoUser.BlockedReason,
            PasswordResetToken = mongoUser.PasswordResetToken,
            PasswordResetTokenExpiry = mongoUser.PasswordResetTokenExpiry
        };
    }
}
