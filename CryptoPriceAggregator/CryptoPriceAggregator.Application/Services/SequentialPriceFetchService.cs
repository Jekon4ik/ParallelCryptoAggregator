using System.Diagnostics;
using CryptoPriceAggregator.Application.Interfaces;
using CryptoPriceAggregator.Application.Models;

namespace CryptoPriceAggregator.Application.Services;

public sealed class SequentialPriceFetchService : ISequentialPriceFetchService
{
    private readonly IReadOnlyList<IExchangeStrategy> _exchanges;

    public SequentialPriceFetchService(IEnumerable<IExchangeStrategy> exchanges)
    {
        _exchanges = exchanges.ToList().AsReadOnly();
    }

    public async Task<AggregatedPriceResponse> FetchAllAsync(string symbol, CancellationToken cancellationToken)
    {
        var results   = new List<PriceResult>(_exchanges.Count);
        var overallSw = Stopwatch.StartNew();

        foreach (var exchange in _exchanges)
        {
            var result = await exchange.FetchPriceAsync(symbol, cancellationToken);
            results.Add(result);
        }

        overallSw.Stop();
        return BuildResponse(symbol, "sequential", results, overallSw.ElapsedMilliseconds);
    }

    private static AggregatedPriceResponse BuildResponse(
        string                     symbol,
        string                     mode,
        IReadOnlyList<PriceResult> results,
        long                       totalElapsedMs)
    {
        var successful = results.Where(r => r.IsSuccess && r.Price.HasValue).ToList();
        return new AggregatedPriceResponse
        {
            Symbol                   = symbol,
            Mode                     = mode,
            TotalElapsedMs           = totalElapsedMs,
            TotalIndividualElapsedMs = results.Sum(r => r.ElapsedMs),
            SuccessCount             = successful.Count,
            FailureCount             = results.Count - successful.Count,
            AveragePrice             = successful.Count > 0 ? successful.Average(r => r.Price!.Value) : null,
            MinPrice                 = successful.Count > 0 ? successful.Min(r => r.Price!.Value)     : null,
            MaxPrice                 = successful.Count > 0 ? successful.Max(r => r.Price!.Value)     : null,
            Results                  = results
        };
    }
}
