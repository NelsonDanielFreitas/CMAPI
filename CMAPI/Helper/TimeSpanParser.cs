using System.Text.RegularExpressions;

namespace CMAPI.Helper;

public static class TimeSpanParser
{
    private static readonly Dictionary<string, int> _ptNumbers = new()
    {
        ["zero"] = 0, ["um"] = 1, ["uma"] = 1,
        ["dois"] = 2, ["duas"] = 2,
        ["três"] = 3, ["tres"] = 3,
        ["quatro"] = 4,
        ["cinco"] = 5,
        ["seis"] = 6,
        ["sete"] = 7,
        ["oito"] = 8,
        ["nove"] = 9,
        ["dez"] = 10
        // extend as needed…
    };

    public static bool TryParsePortuguese(string input, out TimeSpan result)
    {
        result = TimeSpan.Zero;
        if (string.IsNullOrWhiteSpace(input))
            return false;

        // find all "<number> <unit>" e.g. "1 dia", "duas horas", "3 minutos"
        var pattern = @"(?<num>\d+|\p{L}+)\s*(?<unit>dia|dias|hora|horas|minuto|minutos|segundo|segundos)";
        var matches = Regex.Matches(input.ToLowerInvariant(), pattern, RegexOptions.CultureInvariant);

        if (matches.Count == 0)
            return false;

        foreach (Match m in matches)
        {
            var numStr = m.Groups["num"].Value;
            int n;
            if (!int.TryParse(numStr, out n))
            {
                if (!_ptNumbers.TryGetValue(numStr, out n))
                    return false;    // unknown number word
            }

            var unit = m.Groups["unit"].Value;
            switch (unit)
            {
                case "dia":
                case "dias":
                    result = result.Add(TimeSpan.FromDays(n));
                    break;
                case "hora":
                case "horas":
                    result = result.Add(TimeSpan.FromHours(n));
                    break;
                case "minuto":
                case "minutos":
                    result = result.Add(TimeSpan.FromMinutes(n));
                    break;
                case "segundo":
                case "segundos":
                    result = result.Add(TimeSpan.FromSeconds(n));
                    break;
            }
        }

        return true;
    }
}