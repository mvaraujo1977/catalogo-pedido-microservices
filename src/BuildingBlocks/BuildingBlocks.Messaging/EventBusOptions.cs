namespace BuildingBlocks.Messaging;

public class EventBusOptions
{
    public const string SectionName = "EventBus";

    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = 5672;
    public string Username { get; set; } = default!;
    public string Password { get; set; } = default!;
    public string VirtualHost { get; set; } = "/";

    /// <summary>
    /// Exchange do tipo topic onde todos os eventos de integração são
    /// publicados. Fica em configuração (e não fixa no código) para que
    /// os testes possam usar uma exchange isolada.
    /// </summary>
    public string ExchangeName { get; set; } = "catalogopedido.events";
}
