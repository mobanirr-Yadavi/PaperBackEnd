using FluentValidation;
using PaperSite.Application.DTOs.Cart;

namespace PaperSite.Application.Validators.Cart;

public class UpdateCartItemQuantityRequestValidator : AbstractValidator<UpdateCartItemQuantityRequest>
{
    public UpdateCartItemQuantityRequestValidator()
    {
        RuleFor(x => x.ProductId).NotEmpty();
        RuleFor(x => x.Quantity).GreaterThan(0);
    }
}
