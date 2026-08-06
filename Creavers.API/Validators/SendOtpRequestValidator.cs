using FluentValidation;
using Creavers.API.DTOs.Auth;

namespace Creavers.API.Validators
{
    public class SendOtpRequestValidator : AbstractValidator<SendOtpRequest>
    {
        public SendOtpRequestValidator()
        {
            RuleFor(x => x.UserId)
                .NotEmpty().WithMessage("UserId is required.");

            RuleFor(x => x.Purpose)
                .IsInEnum().WithMessage("Purpose must be a valid OtpPurpose value (PhoneVerification, PasswordReset, EmailVerification).");
        }
    }
}
