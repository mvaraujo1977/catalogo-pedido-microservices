using BuildingBlocks.Messaging;
using Catalog.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Testcontainers.PostgreSql;

namespace Catalog.IntegrationTests.Infrastructure;

/// <summary>
/// Sobe a API em memória apontando para um PostgreSQL descartável em container.
/// O banco de desenvolvimento (pedidos-postgres) não é tocado.
/// </summary>
public class CatalogApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:16-alpine")
        .WithDatabase("catalog_db_tests")
        .WithUsername("tests")
        .WithPassword("tests")
        .Build();

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();

        // AddInfrastructure lê a connection string durante a construção do host e
        // lança se ela não existir, então precisa estar disponível antes do primeiro
        // acesso a Services. A variável de ambiente tem precedência sobre o
        // user-secrets do ambiente de desenvolvimento.
        Environment.SetEnvironmentVariable("ConnectionStrings__CatalogDb", _postgres.GetConnectionString());

        using var scope = Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
        await context.Database.MigrateAsync();
    }

    public new async Task DisposeAsync()
    {
        await _postgres.DisposeAsync();
        await base.DisposeAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        // Garante que o DbContext use o container, independentemente do que a
        // configuração da aplicação tenha resolvido.
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll(typeof(DbContextOptions<CatalogDbContext>));
            services.RemoveAll(typeof(DbContextOptions));
            services.RemoveAll(typeof(CatalogDbContext));

            services.AddDbContext<CatalogDbContext>(options =>
                options.UseNpgsql(_postgres.GetConnectionString()));

            services.AddAuthentication(TestAuthHandler.SchemeName)
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.SchemeName, options => { });

            // Sem isso, cada teste tentaria abrir conexão TCP com o
            // RabbitMQ e esperaria o timeout.
            services.RemoveAll(typeof(IEventPublisher));
            services.AddSingleton<IEventPublisher, FakeEventPublisher>();

            services.RemoveAll(typeof(Microsoft.Extensions.Hosting.IHostedService));
        });
    }

    /// <summary>Limpa a tabela de produtos para isolar cada teste.</summary>
    public async Task ResetDatabaseAsync()
    {
        using var scope = Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
        await context.Database.ExecuteSqlRawAsync("TRUNCATE TABLE products;");
    }

    /// <summary>Executa uma ação contra o banco, fora do pipeline HTTP.</summary>
    public async Task<T> QueryDatabaseAsync<T>(Func<CatalogDbContext, Task<T>> query)
    {
        using var scope = Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
        return await query(context);
    }
}
