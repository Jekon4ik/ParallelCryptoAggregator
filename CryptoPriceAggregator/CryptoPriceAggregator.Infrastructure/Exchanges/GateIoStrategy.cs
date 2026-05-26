using System.Diagnostics;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using CryptoPriceAggregator.Application.Interfaces;
using CryptoPriceAggregator.Application.Models;

namespace CryptoPriceAggregator.Infrastructure.Exchanges;

public sealed class GateIoStrategy : IExchangeStrategy
{
    private readonly HttpClient _httpClient;

    public string ExchangeName => "Gate.io";

    public GateIoStrategy(IHttpClientFactory httpClientFactory)
    {
        _httpClient = httpClientFactory.CreateClient(ExchangeName);
    }

    public async Task<PriceResult> FetchPriceAsync(string symbol, CancellationToken cancellationToken)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            var pair     = SymbolHelper.ToDelimitedPair(symbol.ToUpperInvariant(), "_");
            var url      = $"https://api.gateio.ws/api/v4/spot/tickers?currency_pair={pair}";
            var response = await _httpClient.GetFromJsonAsync<List<GateIoTickerDto>>(url, cancellationToken);
            sw.Stop();

            var lastStr = response?.FirstOrDefault()?.Last;
            if (lastStr is null)
                return PriceResult.Failure(ExchangeName, symbol, "Empty [0].last", sw.ElapsedMilliseconds);

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

    private sealed record GateIoTickerDto(
        [property: JsonPropertyName("currency_pair")] string? CurrencyPair,
        [property: JsonPropertyName("last")]          string? Last
    );
}
