namespace BuildingBlocks.Messaging;

/// <summary>
/// Contrato de publicação de eventos de integração. Fica num projeto
/// sem dependência alguma para que a camada Application possa
/// referenciá-lo sem arrastar RabbitMQ.Client — a implementação vive
/// em BuildingBlocks.Messaging, referenciado apenas pela Infrastructure.
/// O namespace é o mesmo do projeto de implementação de propósito: os
/// usings existentes continuam valendo.
/// </summary>
public interface IEventPublisher
{
    Task PublishAsync<TEvent>(TEvent @event, string routingKey, CancellationToken cancellationToken = default)
        where TEvent : class;
}
