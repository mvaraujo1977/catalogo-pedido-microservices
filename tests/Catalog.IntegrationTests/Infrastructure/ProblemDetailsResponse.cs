namespace Catalog.IntegrationTests.Infrastructure;

/// <summary>
/// Forma mínima de ProblemDetails para leitura nas asserções — evita depender
/// do tipo do ASP.NET Core só para inspecionar o corpo de erro.
/// </summary>
public record ProblemDetailsResponse(string? Title, int? Status, string? Detail);
