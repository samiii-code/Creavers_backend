using FluentValidation;
using Creavers.API.DTOs.Providers;

namespace Creavers.API.Validators
{
    public class UpdateProviderProfileRequestValidator : AbstractValidator<UpdateProviderProfileRequest>
    {
        public UpdateProviderProfileRequestValidator()
        {
            RuleFor(x => x.CategoryId)
                .NotEmpty().WithMessage("Category is required.");

            RuleFor(x => x.ExperienceYears)
                .GreaterThanOrEqualTo(0).WithMessage("Experience years must be zero or greater.")
                .LessThanOrEqualTo(60).WithMessage("Experience years seems too high.");

            RuleFor(x => x.Bio)
                .NotEmpty().WithMessage("Bio is required.")
                .MaximumLength(1000).WithMessage("Bio must not exceed 1000 characters.");

            RuleFor(x => x.ServiceArea)
                .NotEmpty().WithMessage("Service area is required.")
                .MaximumLength(300).WithMessage("Service area must not exceed 300 characters.");

            RuleFor(x => x.Availability)
                .NotEmpty().WithMessage("Availability is required.")
                .MaximumLength(300).WithMessage("Availability must not exceed 300 characters.");
        }
    }
}
