using CryptoPriceAggregator.Application.Models;

namespace CryptoPriceAggregator.Application.Interfaces;

public interface ISequentialPriceFetchService
{
    Task<AggregatedPriceResponse> FetchAllAsync(string symbol, CancellationToken cancellationToken);
}
