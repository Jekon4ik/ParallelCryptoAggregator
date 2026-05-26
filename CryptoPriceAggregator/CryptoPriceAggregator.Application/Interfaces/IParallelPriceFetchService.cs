using CryptoPriceAggregator.Application.Models;

namespace CryptoPriceAggregator.Application.Interfaces;

public interface IParallelPriceFetchService
{
    Task<AggregatedPriceResponse> FetchAllAsync(string symbol, CancellationToken cancellationToken);
}
