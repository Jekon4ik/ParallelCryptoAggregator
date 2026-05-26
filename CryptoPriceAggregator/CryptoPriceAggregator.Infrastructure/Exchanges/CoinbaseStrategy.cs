using System.Diagnostics;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using CryptoPriceAggregator.Application.Interfaces;
using CryptoPriceAggregator.Application.Models;

namespace CryptoPriceAggregator.Infrastructure.Exchanges;

public sealed class CoinbaseStrategy : IExchangeStrategy
{
    private readonly HttpClient _httpClient;

    public string ExchangeName => "Coinbase";

    public CoinbaseStrategy(IHttpClientFactory httpClientFactory)
    {
        _httpClient = httpClientFactory.CreateClient(ExchangeName);
    }

    public async Task<PriceResult> FetchPriceAsync(string symbol, CancellationToken cancellationToken)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            var baseAsset = SymbolHelper.ExtractBase(symbol.ToUpperInvariant());
            var url       = $"https://api.coinbase.com/v2/prices/{baseAsset}-USD/spot";
            var response  = await _httpClient.GetFromJsonAsync<CoinbaseResponseDto>(url, cancellationToken);
            sw.Stop();

            var amountStr = response?.Data?.Amount;
            if (amountStr is null)
                return PriceResult.Failure(ExchangeName, symbol, "Empty data.amount", sw.ElapsedMilliseconds);

            return decimal.TryParse(amountStr, System.Globalization.NumberStyles.Any,
                       System.Globalization.CultureInfo.InvariantCulture, out var price)
                ? PriceResult.Success(ExchangeName, symbol, price, sw.ElapsedMilliseconds)
                : PriceResult.Failure(ExchangeName, symbol, $"Could not parse price: {amountStr}", sw.ElapsedMilliseconds);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or System.Text.Json.JsonException)
        {
            sw.Stop();
            return PriceResult.Failure(ExchangeName, symbol, ex.Message, sw.ElapsedMilliseconds);
        }
    }

    private sealed record CoinbaseResponseDto(
        [property: JsonPropertyName("data")] CoinbaseDataDto? Data
    );

    private sealed record CoinbaseDataDto(
        [property: JsonPropertyName("amount")]   string? Amount,
        [property: JsonPropertyName("currency")] string? Currency
    );
}
