namespace Catalog.IntegrationTests.Infrastructure;

/// <summary>
/// Compartilha um único container/host entre todas as classes de teste —
/// subir um Postgres por classe deixaria a suíte lenta demais.
/// </summary>
[CollectionDefinition(nameof(CatalogApiCollection))]
public class CatalogApiCollection : ICollectionFixture<CatalogApiFactory>
{
}
