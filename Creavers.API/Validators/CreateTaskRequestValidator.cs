using FluentValidation;
using Creavers.API.DTOs.Tasks;

namespace Creavers.API.Validators
{
    public class CreateTaskRequestValidator : AbstractValidator<CreateTaskRequest>
    {
        public CreateTaskRequestValidator()
        {
            RuleFor(x => x.Title)
                .NotEmpty().WithMessage("Title is required.")
                .MaximumLength(200).WithMessage("Title must not exceed 200 characters.");

            RuleFor(x => x.Description)
                .NotEmpty().WithMessage("Description is required.")
                .MaximumLength(2000).WithMessage("Description must not exceed 2000 characters.");

            RuleFor(x => x.CategoryId)
                .NotEmpty().WithMessage("Category ID is required.");

            RuleFor(x => x.Address)
                .NotEmpty().WithMessage("Address is required.")
                .MaximumLength(500).WithMessage("Address must not exceed 500 characters.");

            RuleFor(x => x.SubCity)
                .NotEmpty().WithMessage("SubCity is required.")
                .MaximumLength(100).WithMessage("SubCity must not exceed 100 characters.");

            RuleFor(x => x.Woreda)
                .NotEmpty().WithMessage("Woreda is required.")
                .MaximumLength(100).WithMessage("Woreda must not exceed 100 characters.");

            RuleFor(x => x.Landmark)
                .MaximumLength(300).WithMessage("Landmark must not exceed 300 characters.")
                .When(x => !string.IsNullOrEmpty(x.Landmark));

            RuleFor(x => x.Budget)
                .GreaterThan(0).WithMessage("Budget must be greater than 0.");

            RuleFor(x => x.PreferredDate)
                .GreaterThan(DateTime.UtcNow).WithMessage("PreferredDate must be in the future.");

            RuleFor(x => x.Latitude)
                .InclusiveBetween(-90.0, 90.0).WithMessage("Latitude must be between -90 and 90.")
                .When(x => x.Latitude.HasValue);

            RuleFor(x => x.Longitude)
                .InclusiveBetween(-180.0, 180.0).WithMessage("Longitude must be between -180 and 180.")
                .When(x => x.Longitude.HasValue);
        }
    }
}
