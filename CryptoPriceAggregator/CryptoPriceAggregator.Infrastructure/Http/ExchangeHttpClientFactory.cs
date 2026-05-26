using Microsoft.Extensions.DependencyInjection;

namespace CryptoPriceAggregator.Infrastructure.Http;

public static class ExchangeHttpClientFactory
{
    private static readonly string[] ExchangeNames =
    [
        "Binance", "Bybit", "Kraken", "Coinbase", "OKX",
        "Huobi",   "Gate.io", "MEXC", "Bitstamp", "WhiteBIT"
    ];

    public static IServiceCollection AddExchangeHttpClients(this IServiceCollection services)
    {
        foreach (var name in ExchangeNames)
        {
            services.AddHttpClient(name, client =>
            {
                client.Timeout = TimeSpan.FromSeconds(8);
                client.DefaultRequestHeaders.Add("Accept", "application/json");
                client.DefaultRequestHeaders.Add("User-Agent", "CryptoPriceAggregator/1.0");
                client.DefaultRequestHeaders.ConnectionClose = true;
            });
        }

        return services;
    }
}
