using FplBot.Model;

namespace FplBot.Utilities;

public static class FixtureMatcher
{
    // Explicit aliases only: never use fuzzy/substring matching for betting events.
    private static readonly Dictionary<string, string> Aliases = new()
    {
        ["man united"] = "manchester united",
        ["man utd"] = "manchester united",
        ["man city"] = "manchester city",
        ["wolves"] = "wolverhampton wanderers",
        ["wolverhampton"] = "wolverhampton wanderers",
        ["spurs"] = "tottenham hotspur",
        ["tottenham"] = "tottenham hotspur",
        ["brighton and hove albion"] = "brighton",
        ["nottingham"] = "nottingham forest",
        ["nottm forest"] = "nottingham forest",
        ["west ham"] = "west ham united",
        ["newcastle"] = "newcastle united",
        ["leicester"] = "leicester city",
        ["leeds"] = "leeds united"
    };

    public static string NormalizeTeamName(string name)
    {
        var words = name.ToLowerInvariant().Replace("&", " and ").Replace(".", "")
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Where(word => word != "fc" && word != "afc");
        var normalized = string.Join(" ", words);
        return Aliases.GetValueOrDefault(normalized, normalized);
    }

    public static MatchOddsResponse? FindMatch(Match fixture, IEnumerable<MatchOddsResponse> events)
    {
        var home = NormalizeTeamName(fixture.HomeTeam.Name);
        var away = NormalizeTeamName(fixture.AwayTeam.Name);
        if (home.Length == 0 || away.Length == 0) return null;

        // Allow only small provider timestamp differences; reject ambiguous candidates.
        var candidates = events.Where(e => NormalizeTeamName(e.HomeTeam) == home
            && NormalizeTeamName(e.AwayTeam) == away
            && Math.Abs((e.CommenceTime - fixture.UtcDate).TotalMinutes) <= 15).Take(2).ToList();
        return candidates.Count == 1 ? candidates[0] : null;
    }
}
