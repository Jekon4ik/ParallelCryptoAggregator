using System.Diagnostics;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using CryptoPriceAggregator.Application.Interfaces;
using CryptoPriceAggregator.Application.Models;

namespace CryptoPriceAggregator.Infrastructure.Exchanges;

public sealed class MexcStrategy : IExchangeStrategy
{
    private readonly HttpClient _httpClient;

    public string ExchangeName => "MEXC";

    public MexcStrategy(IHttpClientFactory httpClientFactory)
    {
        _httpClient = httpClientFactory.CreateClient(ExchangeName);
    }

    public async Task<PriceResult> FetchPriceAsync(string symbol, CancellationToken cancellationToken)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            var url      = $"https://api.mexc.com/api/v3/ticker/price?symbol={SymbolHelper.EnsurePair(symbol.ToUpperInvariant())}";
            var response = await _httpClient.GetFromJsonAsync<MexcPriceDto>(url, cancellationToken);
            sw.Stop();
            return response is null
                ? PriceResult.Failure(ExchangeName, symbol, "Empty response", sw.ElapsedMilliseconds)
                : PriceResult.Success(ExchangeName, symbol, response.Price, sw.ElapsedMilliseconds);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or System.Text.Json.JsonException)
        {
            sw.Stop();
            return PriceResult.Failure(ExchangeName, symbol, ex.Message, sw.ElapsedMilliseconds);
        }
    }

    private sealed record MexcPriceDto(
        [property: JsonPropertyName("symbol")] string  Symbol,
        [property: JsonPropertyName("price")]  decimal Price
    );
}
