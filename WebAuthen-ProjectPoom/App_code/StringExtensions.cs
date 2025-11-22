namespace WebAuthen.App_code;

public static class StringExtensions
{
    public static string? NullIfEmpty(this string? input)
    {
        return string.IsNullOrWhiteSpace(input) ? null : input;
    }
}

