using FluentValidation;
using Creavers.API.DTOs.Auth;

namespace Creavers.API.Validators
{
    public class LoginRequestValidator : AbstractValidator<LoginRequest>
    {
        public LoginRequestValidator()
        {
            RuleFor(x => x.EmailOrPhone)
                .NotEmpty().WithMessage("Email or phone number is required.");

            RuleFor(x => x.Password)
                .NotEmpty().WithMessage("Password is required.");
        }
    }
}
