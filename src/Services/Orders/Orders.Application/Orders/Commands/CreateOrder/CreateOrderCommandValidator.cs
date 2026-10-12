using FluentValidation;

namespace Orders.Application.Orders.Commands.CreateOrder;

public class CreateOrderCommandValidator : AbstractValidator<CreateOrderCommand>
{
    public CreateOrderCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.Items).NotEmpty().WithMessage("O pedido precisa de pelo menos um item.");

        // Sem isso, um pedido pode listar o mesmo produto duas vezes e
        // a reserva de estoque no Catalog teria que lidar com a soma.
        // É mais honesto recusar aqui, com mensagem clara, do que criar
        // um pedido que falha depois de forma assíncrona.
        RuleFor(x => x.Items)
            .Must(items => items is null || items.Select(i => i.ProductId).Distinct().Count() == items.Count)
            .WithMessage("Cada produto pode aparecer apenas uma vez no pedido; some as quantidades.");

        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.ProductId).NotEmpty();
            item.RuleFor(i => i.Quantity).GreaterThan(0);
        });
    }
}
