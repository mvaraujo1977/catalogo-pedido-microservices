using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Orders.Application.Orders;
using Orders.Application.Products;
using Orders.Infrastructure.Clients;
using Orders.Infrastructure.Persistence;
using Orders.Infrastructure.Persistence.Repositories;

namespace Orders.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("OrdersDb")
            ?? throw new InvalidOperationException("Connection string 'OrdersDb' não configurada.");

        services.AddDbContext<OrdersDbContext>(options => options.UseNpgsql(connectionString));

        services.AddScoped<IOrderRepository, OrderRepository>();

        services.AddHttpClient("keycloak-token");
        services.AddTransient<CatalogServiceTokenHandler>();

        var catalogBaseUrl = configuration["Services:CatalogApi:BaseUrl"]
            ?? throw new InvalidOperationException("Services:CatalogApi:BaseUrl não configurado.");

        services.AddHttpClient<ICatalogServiceClient, CatalogServiceClient>(client =>
        {
            client.BaseAddress = new Uri(catalogBaseUrl);
        })
        .AddHttpMessageHandler<CatalogServiceTokenHandler>();

        return services;
    }
}
