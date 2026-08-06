using FluentValidation;
using Creavers.API.DTOs.Tasks;

namespace Creavers.API.Validators
{
    public class UpdateTaskRequestValidator : AbstractValidator<UpdateTaskRequest>
    {
        public UpdateTaskRequestValidator()
        {
            RuleFor(x => x.Title)
                .MaximumLength(200).WithMessage("Title must not exceed 200 characters.")
                .When(x => x.Title != null);

            RuleFor(x => x.Description)
                .MaximumLength(2000).WithMessage("Description must not exceed 2000 characters.")
                .When(x => x.Description != null);

            RuleFor(x => x.Address)
                .MaximumLength(500).WithMessage("Address must not exceed 500 characters.")
                .When(x => x.Address != null);

            RuleFor(x => x.SubCity)
                .MaximumLength(100).WithMessage("SubCity must not exceed 100 characters.")
                .When(x => x.SubCity != null);

            RuleFor(x => x.Woreda)
                .MaximumLength(100).WithMessage("Woreda must not exceed 100 characters.")
                .When(x => x.Woreda != null);

            RuleFor(x => x.Landmark)
                .MaximumLength(300).WithMessage("Landmark must not exceed 300 characters.")
                .When(x => !string.IsNullOrEmpty(x.Landmark));

            RuleFor(x => x.Budget)
                .GreaterThan(0).WithMessage("Budget must be greater than 0.")
                .When(x => x.Budget.HasValue);

            RuleFor(x => x.Status)
                .IsInEnum().WithMessage("Invalid TaskStatus.")
                .When(x => x.Status.HasValue);
        }
    }
}
