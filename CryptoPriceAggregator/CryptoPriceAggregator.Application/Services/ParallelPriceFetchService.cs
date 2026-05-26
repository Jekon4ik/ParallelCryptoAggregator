using System.Collections.Concurrent;
using System.Diagnostics;
using CryptoPriceAggregator.Application.Interfaces;
using CryptoPriceAggregator.Application.Models;
using Microsoft.Extensions.Logging;

namespace CryptoPriceAggregator.Application.Services;

public sealed class ParallelPriceFetchService : IParallelPriceFetchService
{
    private readonly IReadOnlyList<IExchangeStrategy>   _exchanges;
    private readonly ILogger<ParallelPriceFetchService> _logger;

    private readonly SemaphoreSlim _semaphore = new(maxCount: 6, initialCount: 6);

    private long _totalRequestsDispatched = 0;
    private long _totalRequestsFailed     = 0;

    public ParallelPriceFetchService(
        IEnumerable<IExchangeStrategy>      exchanges,
        ILogger<ParallelPriceFetchService>  logger)
    {
        _exchanges = exchanges.ToList().AsReadOnly();
        _logger    = logger;
    }

    public async Task<AggregatedPriceResponse> FetchAllAsync(string symbol, CancellationToken cancellationToken)
    {
        using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        using var linkedCts  = CancellationTokenSource.CreateLinkedTokenSource(
                                   cancellationToken, timeoutCts.Token);
        var linkedToken = linkedCts.Token;

        var resultsBag = new ConcurrentBag<PriceResult>();
        var overallSw  = Stopwatch.StartNew();

        var tasks = _exchanges.Select(exchange =>
            FetchWithSemaphoreAsync(exchange, symbol, resultsBag, linkedToken));

        await Task.WhenAll(tasks);
        overallSw.Stop();

        var results = resultsBag.ToList();
        return BuildResponse(symbol, "parallel", results, overallSw.ElapsedMilliseconds);
    }

    private async Task FetchWithSemaphoreAsync(
        IExchangeStrategy           exchange,
        string                      symbol,
        ConcurrentBag<PriceResult>  resultsBag,
        CancellationToken           cancellationToken)
    {
        await _semaphore.WaitAsync(cancellationToken);
        Interlocked.Increment(ref _totalRequestsDispatched);
        try
        {
            _logger.LogDebug("Fetching {Symbol} from {Exchange}", symbol, exchange.ExchangeName);
            var result = await exchange.FetchPriceAsync(symbol, cancellationToken);
            resultsBag.Add(result);
            if (!result.IsSuccess)
                Interlocked.Increment(ref _totalRequestsFailed);
        }
        catch (OperationCanceledException)
        {
            resultsBag.Add(PriceResult.Failure(exchange.ExchangeName, symbol, "Request timed out or cancelled", 0));
            Interlocked.Increment(ref _totalRequestsFailed);
        }
        finally
        {
            _semaphore.Release();
        }
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

    public (long Dispatched, long Failed) GetMetrics()
        => (Interlocked.Read(ref _totalRequestsDispatched),
            Interlocked.Read(ref _totalRequestsFailed));
}
