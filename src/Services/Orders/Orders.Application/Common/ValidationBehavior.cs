using FluentValidation;

namespace Orders.Application.Common;

/// <summary>
/// Roda todos os IValidator&lt;TRequest&gt; registrados no DI antes do
/// Handler. Neste projeto, TResponse é sempre Result ou Result&lt;T&gt;
/// (convenção fixada desde o Catalog) — usamos reflection para invocar
/// o Failure(string) estático correspondente, evitando cada Handler
/// reimplementar a tradução de erro de validação. Trade-off deliberado:
/// uma interface genérica com static abstract evitaria a reflection, mas
/// adiciona complexidade que não se paga no tamanho deste projeto.
/// </summary>
public class ValidationBehavior<TRequest, TResponse> : MediatR.IPipelineBehavior<TRequest, TResponse>
    where TRequest : MediatR.IRequest<TResponse>
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
    {
        _validators = validators;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        MediatR.RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (!_validators.Any())
            return await next();

        var context = new ValidationContext<TRequest>(request);
        var failures = _validators
            .Select(v => v.Validate(context))
            .SelectMany(r => r.Errors)
            .ToList();

        if (failures.Count == 0)
            return await next();

        var errorMessage = string.Join(" | ", failures.Select(f => f.ErrorMessage));

        var failureResult = typeof(TResponse)
            .GetMethod("Failure", new[] { typeof(string) })
            ?.Invoke(null, new object[] { errorMessage });

        if (failureResult is TResponse typedResult)
            return typedResult;

        throw new ValidationException(failures);
    }
}
