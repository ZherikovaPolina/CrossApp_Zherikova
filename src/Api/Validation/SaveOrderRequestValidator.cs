using Api.Contracts;
using FluentValidation;

namespace Api.Validation;

public sealed class SaveOrderRequestValidator
    : AbstractValidator<SaveOrderRequest>
{
    public SaveOrderRequestValidator()
    {
        RuleFor(x => x.Customer)
            .NotEmpty()
            .MaximumLength(100)
            .WithMessage("Клієнт обов'язковий, до 100 символів.");

        RuleFor(x => x.CreatedAt)
            .NotEqual(default(DateTimeOffset))
            .WithMessage("Дата створення обов'язкова.");

        RuleFor(x => x.Lines)
            .NotNull()
            .Must(lines => lines is { Count: > 0 })
            .WithMessage("Замовлення повинно містити хоча б один товар.");

        RuleForEach(x => x.Lines)
    .Must(line => line is not null)
    .WithMessage("Позиція замовлення не може бути порожньою.");

RuleForEach(x => x.Lines)
    .Where(line => line is not null)
    .ChildRules(line =>
    {
        line.RuleFor(x => x!.ProductId)
            .NotEmpty()
            .MaximumLength(50)
            .WithMessage("Ідентифікатор товару обов'язковий, до 50 символів.");

        line.RuleFor(x => x!.Name)
            .NotEmpty()
            .MaximumLength(200)
            .WithMessage("Назва товару обов'язкова, до 200 символів.");

        line.RuleFor(x => x!.Price)
            .GreaterThan(0)
            .WithMessage("Ціна повинна бути більшою за нуль.");

        line.RuleFor(x => x!.Quantity)
            .GreaterThan(0)
            .WithMessage("Кількість повинна бути більшою за нуль.");
    });
    }
}

