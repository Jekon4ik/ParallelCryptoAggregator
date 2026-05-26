namespace CryptoPriceAggregator.Application.Models;

public sealed record PriceResult
{
    public required string Exchange     { get; init; }
    public required string Symbol       { get; init; }
    public decimal?        Price        { get; init; }
    public string?         Currency     { get; init; } = "USDT";
    public bool            IsSuccess    { get; init; }
    public string?         ErrorMessage { get; init; }
    public long            ElapsedMs    { get; init; }
    public DateTimeOffset  FetchedAt    { get; init; } = DateTimeOffset.UtcNow;

    public static PriceResult Success(string exchange, string symbol, decimal price, long elapsedMs)
        => new() { Exchange = exchange, Symbol = symbol, Price = price, IsSuccess = true, ElapsedMs = elapsedMs };

    public static PriceResult Failure(string exchange, string symbol, string error, long elapsedMs)
        => new() { Exchange = exchange, Symbol = symbol, IsSuccess = false, ErrorMessage = error, ElapsedMs = elapsedMs };
}
