using System.Diagnostics;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using CryptoPriceAggregator.Application.Interfaces;
using CryptoPriceAggregator.Application.Models;

namespace CryptoPriceAggregator.Infrastructure.Exchanges;

public sealed class BybitStrategy : IExchangeStrategy
{
    private readonly HttpClient _httpClient;

    public string ExchangeName => "Bybit";

    public BybitStrategy(IHttpClientFactory httpClientFactory)
    {
        _httpClient = httpClientFactory.CreateClient(ExchangeName);
    }

    public async Task<PriceResult> FetchPriceAsync(string symbol, CancellationToken cancellationToken)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            var url      = $"https://api.bybit.com/v5/market/tickers?category=spot&symbol={SymbolHelper.EnsurePair(symbol.ToUpperInvariant())}";
            var response = await _httpClient.GetFromJsonAsync<BybitResponseDto>(url, cancellationToken);
            sw.Stop();

            var lastPrice = response?.Result?.List?.FirstOrDefault()?.LastPrice;
            if (lastPrice is null)
                return PriceResult.Failure(ExchangeName, symbol, "Empty or missing lastPrice", sw.ElapsedMilliseconds);

            return decimal.TryParse(lastPrice, System.Globalization.NumberStyles.Any,
                       System.Globalization.CultureInfo.InvariantCulture, out var price)
                ? PriceResult.Success(ExchangeName, symbol, price, sw.ElapsedMilliseconds)
                : PriceResult.Failure(ExchangeName, symbol, $"Could not parse price: {lastPrice}", sw.ElapsedMilliseconds);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or System.Text.Json.JsonException)
        {
            sw.Stop();
            return PriceResult.Failure(ExchangeName, symbol, ex.Message, sw.ElapsedMilliseconds);
        }
    }

    private sealed record BybitResponseDto(
        [property: JsonPropertyName("result")] BybitResultDto? Result
    );

    private sealed record BybitResultDto(
        [property: JsonPropertyName("list")] List<BybitTickerDto>? List
    );

    private sealed record BybitTickerDto(
        [property: JsonPropertyName("symbol")]    string? Symbol,
        [property: JsonPropertyName("lastPrice")] string? LastPrice
    );
}
