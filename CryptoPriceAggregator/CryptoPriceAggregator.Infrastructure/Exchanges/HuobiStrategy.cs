using System.Diagnostics;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using CryptoPriceAggregator.Application.Interfaces;
using CryptoPriceAggregator.Application.Models;

namespace CryptoPriceAggregator.Infrastructure.Exchanges;

public sealed class HuobiStrategy : IExchangeStrategy
{
    private readonly HttpClient _httpClient;

    public string ExchangeName => "Huobi";

    public HuobiStrategy(IHttpClientFactory httpClientFactory)
    {
        _httpClient = httpClientFactory.CreateClient(ExchangeName);
    }

    public async Task<PriceResult> FetchPriceAsync(string symbol, CancellationToken cancellationToken)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            var url      = $"https://api.huobi.pro/market/detail/merged?symbol={SymbolHelper.EnsurePair(symbol.ToUpperInvariant()).ToLowerInvariant()}";
            var response = await _httpClient.GetFromJsonAsync<HuobiResponseDto>(url, cancellationToken);
            sw.Stop();

            var price = response?.Tick?.Close;
            return price is null
                ? PriceResult.Failure(ExchangeName, symbol, "Empty tick.close", sw.ElapsedMilliseconds)
                : PriceResult.Success(ExchangeName, symbol, price.Value, sw.ElapsedMilliseconds);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or System.Text.Json.JsonException)
        {
            sw.Stop();
            return PriceResult.Failure(ExchangeName, symbol, ex.Message, sw.ElapsedMilliseconds);
        }
    }

    private sealed record HuobiResponseDto(
        [property: JsonPropertyName("tick")] HuobiTickDto? Tick
    );

    private sealed record HuobiTickDto(
        [property: JsonPropertyName("close")] decimal? Close
    );
}
