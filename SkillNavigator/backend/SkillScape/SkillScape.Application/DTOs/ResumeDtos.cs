namespace SkillScape.Application.DTOs;

/// <summary>
/// Resume preview DTO
/// </summary>
public class ResumePreviewDto
{
    public PersonalInfoDto PersonalInfo { get; set; } = new();
    public string Summary { get; set; } = string.Empty;
    public List<string> Skills { get; set; } = new();
    public List<DomainSkillsDto> DomainSkills { get; set; } = new();
    public List<BadgeDto> Certifications { get; set; } = new();
}

/// <summary>
/// Personal information for resume
/// </summary>
public class PersonalInfoDto
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Bio { get; set; } = string.Empty;
    public int Level { get; set; }
    public long TotalXP { get; set; }
}

/// <summary>
/// Skills grouped by domain
/// </summary>
public class DomainSkillsDto
{
    public string DomainName { get; set; } = string.Empty;
    public List<string> CompletedSkills { get; set; } = new();
    public int TotalSkillsCompleted { get; set; }
}
