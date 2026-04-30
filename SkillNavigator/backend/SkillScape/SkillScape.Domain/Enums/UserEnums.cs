namespace SkillScape.Domain.Enums;

/// <summary>
/// User role enum
/// </summary>
public enum UserRole
{
    Student = 1,
    Mentor = 2,
    Admin = 3
}

/// <summary>
/// Difficulty levels for skills
/// </summary>
public enum DifficultyLevel
{
    Beginner = 1,
    Intermediate = 2,
    Advanced = 3
}

/// <summary>
/// Badge rarity
/// </summary>
public enum BadgeRarity
{
    Common = 1,
    Rare = 2,
    Epic = 3,
    Legendary = 4
}

/// <summary>
/// Mentor request status
/// </summary>
public enum MentorRequestStatus
{
    Pending = 1,
    Accepted = 2,
    Rejected = 3,
    Cancelled = 4
}
