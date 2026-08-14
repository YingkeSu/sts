namespace SeedSearchPrototype;

public enum OptionArtKind
{
    None,
    Card,
    Relic,
    Boss,
}

public static class OptionArtRouter
{
    public static OptionArtKind For(SearchTheSpireOption option) =>
        For(option.Section);

    public static OptionArtKind For(string? section)
    {
        var value = section ?? string.Empty;
        if (value.Contains("card", StringComparison.OrdinalIgnoreCase))
        {
            return OptionArtKind.Card;
        }

        if (value.Contains("boss", StringComparison.OrdinalIgnoreCase))
        {
            return OptionArtKind.Boss;
        }

        if (value.Contains("relic", StringComparison.OrdinalIgnoreCase) ||
            value.Contains("picks the character", StringComparison.OrdinalIgnoreCase) ||
            value.Equals("any character", StringComparison.OrdinalIgnoreCase))
        {
            return OptionArtKind.Relic;
        }

        // Neow's cursed/bonus offers are all relic-style rewards; the board
        // groups them under their own sections rather than "relic rewards".
        if (value.Equals("cursed offer", StringComparison.OrdinalIgnoreCase) ||
            value.Equals("bonus offer", StringComparison.OrdinalIgnoreCase) ||
            value.Equals("ancient offers", StringComparison.OrdinalIgnoreCase))
        {
            return OptionArtKind.Relic;
        }

        return OptionArtKind.None;
    }
}
