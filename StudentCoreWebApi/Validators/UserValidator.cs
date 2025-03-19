using FluentValidation;
using StudentCoreWebApi.DTOs;

namespace StudentCoreWebApi.Validators
{
    public class UserValidator : AbstractValidator<AddUser>
    {
        public UserValidator()
        {
            RuleFor(user => user.FirstName).NotEmpty()
                .WithMessage("First Name cannot be Empty");
            RuleFor(user => user.LastName).NotEmpty()
                .WithMessage("Last Name cannot be Empty");
            RuleFor(user => user.Email).NotEmpty().WithMessage("Email is required").EmailAddress()
                .WithMessage("Please check email format");
            RuleFor(user => user.Phone).GreaterThan(999999999)
                .WithMessage("Phone number must be contain 10 digits.").
                LessThan(9999999999).WithMessage("Maximum number exceeded");
        }
    }
}
