using System.Diagnostics;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using CryptoPriceAggregator.Application.Interfaces;
using CryptoPriceAggregator.Application.Models;

namespace CryptoPriceAggregator.Infrastructure.Exchanges;

public sealed class OKXStrategy : IExchangeStrategy
{
    private readonly HttpClient _httpClient;

    public string ExchangeName => "OKX";

    public OKXStrategy(IHttpClientFactory httpClientFactory)
    {
        _httpClient = httpClientFactory.CreateClient(ExchangeName);
    }

    public async Task<PriceResult> FetchPriceAsync(string symbol, CancellationToken cancellationToken)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            var instId   = SymbolHelper.ToDelimitedPair(symbol.ToUpperInvariant(), "-");
            var url      = $"https://www.okx.com/api/v5/market/ticker?instId={instId}";
            var response = await _httpClient.GetFromJsonAsync<OkxResponseDto>(url, cancellationToken);
            sw.Stop();

            var lastStr = response?.Data?.FirstOrDefault()?.Last;
            if (lastStr is null)
                return PriceResult.Failure(ExchangeName, symbol, "Empty data[0].last", sw.ElapsedMilliseconds);

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

    private sealed record OkxResponseDto(
        [property: JsonPropertyName("data")] List<OkxTickerDto>? Data
    );

    private sealed record OkxTickerDto(
        [property: JsonPropertyName("instId")] string? InstId,
        [property: JsonPropertyName("last")]   string? Last
    );
}
