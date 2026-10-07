using System.Text.RegularExpressions;

namespace MiddlewareDemo.Worker.Helpers;

public static class ConverterOutputParser
{
    private static readonly Regex ResultLine =
        new(@"^converted\s+(?<pages>\d{1,5})\s+pages?\s+->\s+(?<target>[\w./-]{1,200})$",
            RegexOptions.Compiled | RegexOptions.Multiline, TimeSpan.FromMilliseconds(200));

    public static (int Pages, string Target)? Parse(string output)
    {
        try
        {
            var match = ResultLine.Match(output ?? string.Empty);
            if (!match.Success)
            {
                return null;
            }

            return (int.Parse(match.Groups["pages"].Value), match.Groups["target"].Value);
        }
        catch (RegexMatchTimeoutException)
        {
            return null;
        }
    }
}
