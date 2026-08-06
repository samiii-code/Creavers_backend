using FluentValidation;
using Creavers.API.DTOs.Auth;

namespace Creavers.API.Validators
{
    public class VerifyOtpRequestValidator : AbstractValidator<VerifyOtpRequest>
    {
        public VerifyOtpRequestValidator()
        {
            RuleFor(x => x.UserId)
                .NotEmpty().WithMessage("UserId is required.");

            RuleFor(x => x.Code)
                .NotEmpty().WithMessage("OTP code is required.")
                .Length(6).WithMessage("OTP code must be exactly 6 digits.")
                .Matches(@"^\d{6}$").WithMessage("OTP code must contain only digits.");

            RuleFor(x => x.Purpose)
                .IsInEnum().WithMessage("Purpose must be a valid OtpPurpose value.");
        }
    }
}
