namespace CryptoPriceAggregator.Application.Models;

public sealed record AggregatedPriceResponse
{
    public required string                     Symbol                   { get; init; }
    public required string                     Mode                     { get; init; }
    public required long                       TotalElapsedMs           { get; init; }
    public required long                       TotalIndividualElapsedMs { get; init; }
    public double                              SpeedupFactor            => TotalElapsedMs > 0
                                                                           ? Math.Round((double)TotalIndividualElapsedMs / TotalElapsedMs, 2)
                                                                           : 0;
    public required int                        SuccessCount             { get; init; }
    public required int                        FailureCount             { get; init; }
    public decimal?                            AveragePrice             { get; init; }
    public decimal?                            MinPrice                 { get; init; }
    public decimal?                            MaxPrice                 { get; init; }
    public required IReadOnlyList<PriceResult> Results                  { get; init; }
}
