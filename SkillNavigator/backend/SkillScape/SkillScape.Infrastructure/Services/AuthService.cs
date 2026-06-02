using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using SkillScape.Application.DTOs;
using SkillScape.Application.Interfaces;
using SkillScape.Domain.Entities;
using SkillScape.Infrastructure.Data;

namespace SkillScape.Infrastructure.Services;

public class AuthService : IAuthService
{
    private readonly ApplicationDbContext _context;
    private readonly ITokenService _tokenService;
    private readonly IEmailService _emailService;

    public AuthService(
        ApplicationDbContext context,
        ITokenService tokenService,
        IEmailService emailService)
    {
        _context = context;
        _tokenService = tokenService;
        _emailService = emailService;
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);

        if (user == null)
            throw new UnauthorizedAccessException("Invalid email or password");

        if (user.IsBlocked)
            throw new UnauthorizedAccessException($"Your account is blocked. {user.BlockedReason}");

        if (string.IsNullOrEmpty(user.PasswordHash) || !VerifyPassword(request.Password, user.PasswordHash))
            throw new UnauthorizedAccessException("Invalid email or password");

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

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
    {
        if (request.Password != request.ConfirmPassword)
            throw new InvalidOperationException("Passwords do not match");

        var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
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

        return MapToProfileDto(user);
    }

    public async Task ChangePasswordAsync(string userId, ChangePasswordRequest request)
    {
        if (request.NewPassword != request.ConfirmPassword)
            throw new InvalidOperationException("Passwords do not match");

        var user = await _context.Users.FindAsync(userId)
            ?? throw new InvalidOperationException("User not found");

        user.PasswordHash = HashPassword(request.NewPassword);
        user.UpdatedAt = DateTime.UtcNow;
        user.PasswordResetToken = null;
        user.PasswordResetTokenExpiry = null;
        
        _context.Users.Update(user);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAccountAsync(string userId)
    {
        var user = await _context.Users.FindAsync(userId)
            ?? throw new InvalidOperationException("User not found");

        _context.Users.Remove(user);
        await _context.SaveChangesAsync();
    }

    public async Task<string> ForgotPasswordAsync(ForgotPasswordRequest request)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);

        if (user == null)
        {
            return "If an account exists with this email, a reset link has been sent.";
        }

        var token = Guid.NewGuid().ToString("N");
        user.PasswordResetToken = token;
        user.PasswordResetTokenExpiry = DateTime.UtcNow.AddHours(1);
        user.UpdatedAt = DateTime.UtcNow;

        _context.Users.Update(user);
        await _context.SaveChangesAsync();

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
        var user = await _context.Users.FirstOrDefaultAsync(u => u.PasswordResetToken == request.Token);

        if (user == null || user.PasswordResetTokenExpiry == null || user.PasswordResetTokenExpiry < DateTime.UtcNow)
            throw new InvalidOperationException("Invalid or expired reset token");

        user.PasswordHash = HashPassword(request.Password);
        user.PasswordResetToken = null;
        user.PasswordResetTokenExpiry = null;
        user.UpdatedAt = DateTime.UtcNow;
        
        _context.Users.Update(user);
        await _context.SaveChangesAsync();
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
}
