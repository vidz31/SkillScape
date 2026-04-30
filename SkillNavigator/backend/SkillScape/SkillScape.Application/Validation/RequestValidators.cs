using FluentValidation;
using SkillScape.Application.DTOs;

namespace SkillScape.Application.Validation;

/// <summary>
/// Validators for request DTOs
/// </summary>
public class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required")
            .EmailAddress().WithMessage("Invalid email format");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required")
            .MinimumLength(6).WithMessage("Password must be at least 6 characters");
    }
}

public class RegisterRequestValidator : AbstractValidator<RegisterRequest>
{
    public RegisterRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required")
            .EmailAddress().WithMessage("Invalid email format");

        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("Full name is required")
            .MinimumLength(3).WithMessage("Name must be at least 3 characters");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required")
            .MinimumLength(6).WithMessage("Password must be at least 6 characters")
            .Equal(x => x.ConfirmPassword).WithMessage("Passwords do not match");

        RuleFor(x => x.Role)
            .Must(x => x == "Student" || x == "Mentor")
            .WithMessage("Role must be Student or Mentor");
    }
}

public class SubmitQuizRequestValidator : AbstractValidator<SubmitQuizRequest>
{
    public SubmitQuizRequestValidator()
    {
        RuleFor(x => x.Responses)
            .NotEmpty().WithMessage("Quiz responses are required")
            .Must(x => x.Count > 0).WithMessage("At least one response is required");
    }
}

public class CreateMentorRequestValidator : AbstractValidator<CreateMentorRequestDto>
{
    public CreateMentorRequestValidator()
    {
        RuleFor(x => x.MentorId)
            .NotEmpty().WithMessage("Mentor ID is required");

        RuleFor(x => x.Topic)
            .NotEmpty().WithMessage("Topic is required")
            .MinimumLength(5).WithMessage("Topic must be at least 5 characters");

        RuleFor(x => x.Message)
            .MinimumLength(10).WithMessage("Message should be at least 10 characters");
    }
}
