using Ganss.Xss;
using System.Text.RegularExpressions;

public static class InputSanitizer
{
    private static readonly HtmlSanitizer _sanitizer;
    static InputSanitizer()
    {
        _sanitizer = new HtmlSanitizer();

        _sanitizer.AllowedTags.Clear();
        _sanitizer.AllowedAttributes.Clear();
        _sanitizer.AllowedCssProperties.Clear();
        _sanitizer.AllowedSchemes.Clear();
        _sanitizer.AllowDataAttributes = false;
        _sanitizer.KeepChildNodes = true;
    }

    public static string Sanitize(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return input;

        string result = input;

        // 1️⃣ Remove <script> and <style> blocks
        result = Regex.Replace(result, @"<script.*?>.*?</script>", string.Empty, RegexOptions.IgnoreCase | RegexOptions.Singleline);
        result = Regex.Replace(result, @"<style.*?>.*?</style>", string.Empty, RegexOptions.IgnoreCase | RegexOptions.Singleline);

        // 2️⃣ Strip all remaining tags
        result = _sanitizer.Sanitize(result);

        // 3️⃣ Remove suspicious JS patterns
        //    alert(...), eval(...), document.*, window.*, onXYZ=
        string[] jsPatterns = new string[]
        {
            @"\b(alert|eval|prompt|confirm)\s*\(.*?\)", // function calls
            @"\b(document|window)\b",                   // access to document/window
            @"on\w+\s*=",                               // inline event handlers
            @"javascript\s*:"                           // javascript: URIs
        };

        foreach (var pattern in jsPatterns)
        {
            result = Regex.Replace(result, pattern, string.Empty, RegexOptions.IgnoreCase | RegexOptions.Singleline);
        }

        // 4️⃣ Remove extra tags if any left
        result = Regex.Replace(result, "<.*?>", string.Empty);

        // 5️⃣ Trim
        return result.Trim();
    }

    public static bool ContainsUnsafeContent(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return false;

        string sanitized = Sanitize(input);
        return !string.Equals(input, sanitized, StringComparison.Ordinal);
    }
}
