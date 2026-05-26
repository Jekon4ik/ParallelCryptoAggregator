using System.Diagnostics;
using System.Text.Json;
using CryptoPriceAggregator.Application.Interfaces;
using CryptoPriceAggregator.Application.Models;

namespace CryptoPriceAggregator.Infrastructure.Exchanges;

public sealed class KrakenStrategy : IExchangeStrategy
{
    private readonly HttpClient _httpClient;

    public string ExchangeName => "Kraken";

    public KrakenStrategy(IHttpClientFactory httpClientFactory)
    {
        _httpClient = httpClientFactory.CreateClient(ExchangeName);
    }

    public async Task<PriceResult> FetchPriceAsync(string symbol, CancellationToken cancellationToken)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            var pair   = ToKrakenPair(symbol.ToUpperInvariant());
            var url    = $"https://api.kraken.com/0/public/Ticker?pair={pair}";
            var stream = await _httpClient.GetStreamAsync(url, cancellationToken);
            var doc    = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
            sw.Stop();

            var errors = doc.RootElement.GetProperty("error");
            if (errors.GetArrayLength() > 0)
                return PriceResult.Failure(ExchangeName, symbol, errors[0].GetString() ?? "Unknown Kraken error", sw.ElapsedMilliseconds);

            var result = doc.RootElement.GetProperty("result");
            using var enumerator = result.EnumerateObject();
            if (!enumerator.MoveNext())
                return PriceResult.Failure(ExchangeName, symbol, "Empty result", sw.ElapsedMilliseconds);

            var closeArray = enumerator.Current.Value.GetProperty("c");
            var priceStr   = closeArray[0].GetString();

            return decimal.TryParse(priceStr, System.Globalization.NumberStyles.Any,
                       System.Globalization.CultureInfo.InvariantCulture, out var price)
                ? PriceResult.Success(ExchangeName, symbol, price, sw.ElapsedMilliseconds)
                : PriceResult.Failure(ExchangeName, symbol, $"Could not parse price: {priceStr}", sw.ElapsedMilliseconds);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
        {
            sw.Stop();
            return PriceResult.Failure(ExchangeName, symbol, ex.Message, sw.ElapsedMilliseconds);
        }
    }

    private static string ToKrakenPair(string symbol)
    {
        var full = SymbolHelper.EnsurePair(symbol);
        return full.Replace("BTC", "XBT", StringComparison.Ordinal);
    }
}
