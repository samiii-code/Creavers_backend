using FluentValidation;
using Creavers.API.DTOs.Bookings;

namespace Creavers.API.Validators
{
    public class CreateBookingRequestValidator : AbstractValidator<CreateBookingRequest>
    {
        public CreateBookingRequestValidator()
        {
            RuleFor(x => x.TaskId)
                .NotEmpty().WithMessage("Task ID is required.");

            RuleFor(x => x.ProviderId)
                .NotEmpty().WithMessage("Provider ID is required.");

            RuleFor(x => x.Notes)
                .MaximumLength(1000).WithMessage("Notes must not exceed 1000 characters.")
                .When(x => !string.IsNullOrEmpty(x.Notes));
        }
    }
}
