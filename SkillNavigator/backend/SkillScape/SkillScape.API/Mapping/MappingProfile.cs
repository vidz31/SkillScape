using AutoMapper;
using SkillScape.Application.DTOs;
using SkillScape.Domain.Entities;

namespace SkillScape.API.Mapping;

/// <summary>
/// AutoMapper configuration for DTO mapping
/// </summary>
public class MappingProfile : Profile
{
    public MappingProfile()
    {
        // Domain to DTO mappings
        CreateMap<ApplicationUser, UserProfileDto>().ReverseMap();
        CreateMap<CareerDomain, CareerDomainDto>().ReverseMap();
        CreateMap<Skill, SkillDto>().ReverseMap();
        CreateMap<UserSkill, UserSkillDto>()
            .ForMember(dest => dest.SkillName, opt => opt.MapFrom(src => src.Skill!.Name));
        CreateMap<UserProgress, UserProgressDto>()
            .ForMember(dest => dest.DomainName, opt => opt.MapFrom(src => src.CareerDomain!.Name));
        CreateMap<Badge, BadgeDto>().ReverseMap();
        CreateMap<ApplicationMentor, MentorDto>()
            .ForMember(dest => dest.UserName, opt => opt.MapFrom(src => src.User!.FullName));
        CreateMap<MentorRequest, MentorRequestDto>()
            .ForMember(dest => dest.MentorName, opt => opt.MapFrom(src => src.Mentor!.User!.FullName));

        // Quiz mappings
        CreateMap<QuizQuestion, QuizQuestionDto>().ReverseMap();
        CreateMap<QuizOption, QuizOptionDto>().ReverseMap();
    }
}
