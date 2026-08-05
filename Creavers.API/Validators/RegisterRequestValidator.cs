using FluentValidation;
using Creavers.API.DTOs.Auth;

namespace Creavers.API.Validators
{
    public class RegisterRequestValidator : AbstractValidator<RegisterRequest>
    {
        private static readonly string[] AllowedRoles = { "ADMIN", "PROVIDER", "CUSTOMER" };

        public RegisterRequestValidator()
        {
            RuleFor(x => x.FullName)
                .NotEmpty().WithMessage("Full name is required.")
                .MaximumLength(200).WithMessage("Full name must not exceed 200 characters.");

            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Email is required.")
                .EmailAddress().WithMessage("A valid email address is required.");

            RuleFor(x => x.PhoneNumber)
                .NotEmpty().WithMessage("Phone number is required.")
                .Matches(@"^\+?[0-9\s\-]{7,20}$").WithMessage("A valid phone number is required.");

            RuleFor(x => x.Password)
                .NotEmpty().WithMessage("Password is required.")
                .MinimumLength(8).WithMessage("Password must be at least 8 characters.")
                .Matches(@"[A-Z]").WithMessage("Password must contain at least one uppercase letter.")
                .Matches(@"[a-z]").WithMessage("Password must contain at least one lowercase letter.")
                .Matches(@"[0-9]").WithMessage("Password must contain at least one digit.")
                .Matches(@"[^a-zA-Z0-9]").WithMessage("Password must contain at least one special character.");

            RuleFor(x => x.Role)
                .NotEmpty().WithMessage("Role is required.")
                .Must(r => AllowedRoles.Contains(r.ToUpper()))
                .WithMessage("Role must be one of: ADMIN, PROVIDER, CUSTOMER.");
        }
    }
}
