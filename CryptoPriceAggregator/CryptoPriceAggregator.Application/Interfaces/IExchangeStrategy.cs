using CryptoPriceAggregator.Application.Models;

namespace CryptoPriceAggregator.Application.Interfaces;

public interface IExchangeStrategy
{
    string ExchangeName { get; }
    Task<PriceResult> FetchPriceAsync(string symbol, CancellationToken cancellationToken);
}
