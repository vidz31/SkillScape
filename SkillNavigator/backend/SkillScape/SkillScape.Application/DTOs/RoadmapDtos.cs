using System;
using System.Collections.Generic;

namespace SkillScape.Application.DTOs;

public class RoadmapDto
{
    public string DomainId { get; set; } = string.Empty;
    public string DomainName { get; set; } = string.Empty;
    public double ProgressPercentage { get; set; } // Overall domain progress
    public List<SkillRoadmapDto> Skills { get; set; } = new List<SkillRoadmapDto>();
}

public class RoadmapOptionDto
{
    public string DomainId { get; set; } = string.Empty;
    public string DomainName { get; set; } = string.Empty;
    public double ProgressPercentage { get; set; }
    public bool IsRecommended { get; set; }
}

public class SkillRoadmapDto
{
    public string SkillId { get; set; } = string.Empty;
    public string SkillName { get; set; } = string.Empty;
    public string SkillDescription { get; set; } = string.Empty;
    public bool IsCompleted { get; set; }
    public List<RoadmapModuleDto> Modules { get; set; } = new List<RoadmapModuleDto>();
}

public class RoadmapModuleDto
{
    public string ModuleId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int StepNumber { get; set; }
    public int EstimatedHours { get; set; }
    public bool IsCompleted { get; set; }
    public List<RoadmapTopicDto> Topics { get; set; } = new List<RoadmapTopicDto>();
}

public class RoadmapTopicDto
{
    public string TopicId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? ResourceUrl { get; set; }
}
