using FluentValidation;
using StudentCoreWebApi.Controllers;
using StudentCoreWebApi.DTOs;

public class RegisterRequestValidator : AbstractValidator<RegisterRequest>
{
    public RegisterRequestValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress().WithMessage("Invalid email format");
        RuleFor(x => x.Password).MinimumLength(6).WithMessage("Password must be at least 6 characters long");
    }
}