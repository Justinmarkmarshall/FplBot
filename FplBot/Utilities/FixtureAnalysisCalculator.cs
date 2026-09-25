using FplBot.Model;

namespace FplBot.Utilities;

public static class FixtureAnalysisCalculator
{
    public static decimal? ImpliedProbability(decimal odds)
    {
        if (odds <= 1) return null;
        var probability = 1 / odds;
        return probability > 0 ? probability : null;
    }

    public static decimal[]? RemoveMargin(params decimal[] odds)
    {
        if (odds.Length < 2 || odds.Any(price => ImpliedProbability(price) == null)) return null;
        var raw = odds.Select(price => ImpliedProbability(price)!.Value).ToArray();
        var total = raw.Sum();
        return raw.Select(p => p / total).ToArray();
    }

    public static decimal? CalculateAttackingPotential(decimal? win, decimal? over25) => (win + over25) / 2;
    public static decimal? CalculateDefensivePotential(decimal? win, decimal? btts) => (win + (1 - btts)) / 2;

    public static List<FixtureAnalysisDto> Analyze(Match fixture, MatchOddsResponse? odds)
    {
        var home = new List<decimal>();
        var away = new List<decimal>();
        var totals = new List<decimal>();
        var btts = new List<decimal>();
        foreach (var bookmaker in odds?.Bookmakers ?? [])
        {
            var h2h = bookmaker.Markets.FirstOrDefault(m => m.Key == "h2h");
            var probabilities = NormalizeMarket(h2h,
                o => FixtureMatcher.NormalizeTeamName(o.Name) == FixtureMatcher.NormalizeTeamName(fixture.HomeTeam.Name),
                o => o.Name.Equals("Draw", StringComparison.OrdinalIgnoreCase),
                o => FixtureMatcher.NormalizeTeamName(o.Name) == FixtureMatcher.NormalizeTeamName(fixture.AwayTeam.Name));
            if (probabilities != null)
            {
                home.Add(probabilities[0]);
                away.Add(probabilities[2]);
            }

            var over = NormalizeMarket(bookmaker.Markets.FirstOrDefault(m => m.Key == "totals"),
                o => o.Name.Equals("Over", StringComparison.OrdinalIgnoreCase) && o.Point == 2.5m,
                o => o.Name.Equals("Under", StringComparison.OrdinalIgnoreCase) && o.Point == 2.5m);
            if (over != null) totals.Add(over[0]);

            var both = NormalizeMarket(bookmaker.Markets.FirstOrDefault(m => m.Key == "btts"),
                o => o.Name.Equals("Yes", StringComparison.OrdinalIgnoreCase),
                o => o.Name.Equals("No", StringComparison.OrdinalIgnoreCase));
            if (both != null) btts.Add(both[0]);
        }

        decimal? Average(List<decimal> values) => values.Count == 0 ? null : values.Average();
        FixtureAnalysisDto Create(bool isHome) => new()
        {
            Team = isHome ? fixture.HomeTeam.Name : fixture.AwayTeam.Name,
            Opponent = isHome ? fixture.AwayTeam.Name : fixture.HomeTeam.Name,
            Home = isHome,
            Kickoff = fixture.UtcDate,
            WinProbability = Percentage.FromProbability(Average(isHome ? home : away)),
            Over25Probability = Percentage.FromProbability(Average(totals)),
            BothTeamsToScoreProbability = Percentage.FromProbability(Average(btts)),
            AttackingPotential = Percentage.FromProbability(CalculateAttackingPotential(Average(isHome ? home : away), Average(totals))),
            DefensivePotential = Percentage.FromProbability(CalculateDefensivePotential(Average(isHome ? home : away), Average(btts)))
        };
        return [Create(true), Create(false)];
    }

    private static decimal[]? NormalizeMarket(Market? market, params Func<Outcome, bool>[] selectors)
    {
        if (market == null) return null;
        var prices = new List<decimal>();
        foreach (var selector in selectors)
        {
            var outcomes = market.Outcomes.Where(selector).Take(2).ToList();
            if (outcomes.Count != 1) return null;
            prices.Add(outcomes[0].Price);
        }
        return RemoveMargin(prices.ToArray());
    }
}
