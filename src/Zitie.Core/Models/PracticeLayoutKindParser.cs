namespace Zitie.Core.Models;

public static class PracticeLayoutKindParser
{
    public static PracticeLayoutKind Parse(string? value)
    {
        var normalized = Normalize(value);
        if (normalized.Length == 0) return PracticeLayoutKind.Standard;

        foreach (var kind in Enum.GetValues<PracticeLayoutKind>())
            if (string.Equals(Normalize(kind.ToString()), normalized, StringComparison.OrdinalIgnoreCase))
                return kind;

        return PracticeLayoutKind.Standard;
    }

    private static string Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return string.Empty;

        return value.Trim()
            .Replace("-", string.Empty, StringComparison.Ordinal)
            .Replace("_", string.Empty, StringComparison.Ordinal)
            .Replace(" ", string.Empty, StringComparison.Ordinal);
    }
}
