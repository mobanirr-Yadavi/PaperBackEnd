using FluentValidation;
using PaperSite.Application.DTOs.Order;

namespace PaperSite.Application.Validators.Order;

public class CreateOrderRequestValidator : AbstractValidator<CreateOrderRequest>
{
    public CreateOrderRequestValidator()
    {
        RuleFor(x => x.ShippingAddress).NotEmpty().MaximumLength(1000);
        RuleFor(x => x.ReceiverFullName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.ReceiverPhoneNumber).NotEmpty().MaximumLength(20);
        RuleFor(x => x.Items).NotEmpty();
        RuleForEach(x => x.Items).SetValidator(new CreateOrderItemRequestValidator());
    }
}

public class CreateOrderItemRequestValidator : AbstractValidator<CreateOrderItemRequest>
{
    public CreateOrderItemRequestValidator()
    {
        RuleFor(x => x.ProductId).NotEmpty();
        RuleFor(x => x.Quantity).GreaterThan(0);
    }
}
