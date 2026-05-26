using System.Diagnostics;
using System.Text.Json;
using CryptoPriceAggregator.Application.Interfaces;
using CryptoPriceAggregator.Application.Models;

namespace CryptoPriceAggregator.Infrastructure.Exchanges;

public sealed class WhiteBITStrategy : IExchangeStrategy
{
    private readonly HttpClient _httpClient;

    public string ExchangeName => "WhiteBIT";

    public WhiteBITStrategy(IHttpClientFactory httpClientFactory)
    {
        _httpClient = httpClientFactory.CreateClient(ExchangeName);
    }

    public async Task<PriceResult> FetchPriceAsync(string symbol, CancellationToken cancellationToken)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            var key    = SymbolHelper.ToDelimitedPair(symbol.ToUpperInvariant(), "_");
            var stream = await _httpClient.GetStreamAsync("https://whitebit.com/api/v4/public/ticker", cancellationToken);
            var doc    = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
            sw.Stop();

            if (!doc.RootElement.TryGetProperty(key, out var tickerEl))
                return PriceResult.Failure(ExchangeName, symbol, $"Symbol '{key}' not found in ticker", sw.ElapsedMilliseconds);

            if (!tickerEl.TryGetProperty("last_price", out var lastPriceEl))
                return PriceResult.Failure(ExchangeName, symbol, "Missing last_price field", sw.ElapsedMilliseconds);

            var lastStr = lastPriceEl.GetString();
            return decimal.TryParse(lastStr, System.Globalization.NumberStyles.Any,
                       System.Globalization.CultureInfo.InvariantCulture, out var price)
                ? PriceResult.Success(ExchangeName, symbol, price, sw.ElapsedMilliseconds)
                : PriceResult.Failure(ExchangeName, symbol, $"Could not parse price: {lastStr}", sw.ElapsedMilliseconds);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
        {
            sw.Stop();
            return PriceResult.Failure(ExchangeName, symbol, ex.Message, sw.ElapsedMilliseconds);
        }
    }
}
