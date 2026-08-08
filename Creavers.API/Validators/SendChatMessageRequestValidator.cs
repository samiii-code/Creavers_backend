using FluentValidation;
using Creavers.API.DTOs.Chat;

namespace Creavers.API.Validators
{
    public class SendChatMessageRequestValidator : AbstractValidator<SendChatMessageRequest>
    {
        public SendChatMessageRequestValidator()
        {
            RuleFor(x => x.Message)
                .NotEmpty().WithMessage("Message cannot be empty.")
                .MaximumLength(2000).WithMessage("Message must not exceed 2000 characters.");
        }
    }
}
