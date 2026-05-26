using System.Diagnostics;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using CryptoPriceAggregator.Application.Interfaces;
using CryptoPriceAggregator.Application.Models;

namespace CryptoPriceAggregator.Infrastructure.Exchanges;

public sealed class BitstampStrategy : IExchangeStrategy
{
    private readonly HttpClient _httpClient;

    public string ExchangeName => "Bitstamp";

    public BitstampStrategy(IHttpClientFactory httpClientFactory)
    {
        _httpClient = httpClientFactory.CreateClient(ExchangeName);
    }

    public async Task<PriceResult> FetchPriceAsync(string symbol, CancellationToken cancellationToken)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            var pair     = SymbolHelper.ExtractBase(symbol.ToUpperInvariant()).ToLowerInvariant() + "usd";
            var url      = $"https://www.bitstamp.net/api/v2/ticker/{pair}/";
            var response = await _httpClient.GetFromJsonAsync<BitstampTickerDto>(url, cancellationToken);
            sw.Stop();

            var lastStr = response?.Last;
            if (lastStr is null)
                return PriceResult.Failure(ExchangeName, symbol, "Empty last field", sw.ElapsedMilliseconds);

            return decimal.TryParse(lastStr, System.Globalization.NumberStyles.Any,
                       System.Globalization.CultureInfo.InvariantCulture, out var price)
                ? PriceResult.Success(ExchangeName, symbol, price, sw.ElapsedMilliseconds)
                : PriceResult.Failure(ExchangeName, symbol, $"Could not parse price: {lastStr}", sw.ElapsedMilliseconds);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or System.Text.Json.JsonException)
        {
            sw.Stop();
            return PriceResult.Failure(ExchangeName, symbol, ex.Message, sw.ElapsedMilliseconds);
        }
    }

    private sealed record BitstampTickerDto(
        [property: JsonPropertyName("last")] string? Last,
        [property: JsonPropertyName("ask")]  string? Ask,
        [property: JsonPropertyName("bid")]  string? Bid
    );
}
