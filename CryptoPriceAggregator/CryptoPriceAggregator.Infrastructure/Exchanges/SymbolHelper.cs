namespace CryptoPriceAggregator.Infrastructure.Exchanges;

internal static class SymbolHelper
{
    private static readonly string[] KnownQuotes = ["USDT", "USDC", "USD", "EUR", "GBP"];

    internal static string EnsurePair(string symbol, string defaultQuote = "USDT")
    {
        foreach (var q in KnownQuotes)
            if (symbol.EndsWith(q, StringComparison.OrdinalIgnoreCase) && symbol.Length > q.Length)
                return symbol;
        return symbol + defaultQuote;
    }

    internal static string ToDelimitedPair(string symbol, string delimiter, string defaultQuote = "USDT")
    {
        foreach (var q in KnownQuotes)
            if (symbol.EndsWith(q, StringComparison.OrdinalIgnoreCase) && symbol.Length > q.Length)
                return symbol[..^q.Length] + delimiter + q;
        return symbol + delimiter + defaultQuote;
    }

    internal static string ExtractBase(string symbol)
    {
        foreach (var q in KnownQuotes)
            if (symbol.EndsWith(q, StringComparison.OrdinalIgnoreCase) && symbol.Length > q.Length)
                return symbol[..^q.Length];
        return symbol;
    }
}
