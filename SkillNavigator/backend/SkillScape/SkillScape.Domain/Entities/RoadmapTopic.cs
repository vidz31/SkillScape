using System;

namespace SkillScape.Domain.Entities;

/// <summary>
/// Defines a specific learning topic or path within a broader Roadmap Module (Step)
/// </summary>
public class RoadmapTopic
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    
    public string ModuleId { get; set; } = string.Empty; // FK to RoadmapStep
    
    public string Title { get; set; } = string.Empty;
    
    public string? ResourceUrl { get; set; }
    
    public int DisplayOrder { get; set; }
    
    // Navigation property
    public RoadmapStep? Module { get; set; }
}
