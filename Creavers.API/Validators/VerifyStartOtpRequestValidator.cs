using FluentValidation;
using Creavers.API.DTOs.Jobs;

namespace Creavers.API.Validators
{
    public class VerifyStartOtpRequestValidator : AbstractValidator<VerifyStartOtpRequest>
    {
        public VerifyStartOtpRequestValidator()
        {
            RuleFor(x => x.Otp)
                .NotEmpty().WithMessage("OTP is required.")
                .Length(6).WithMessage("OTP must be exactly 6 digits.")
                .Matches(@"^\d{6}$").WithMessage("OTP must contain only digits.");
        }
    }
}
