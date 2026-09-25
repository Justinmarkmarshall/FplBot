using FplBot.Model;
using FplBot.Utilities;

namespace FplBot.UnitTests;

public class FixtureAnalysisCalculatorTests
{
    internal static Match Fixture() => new()
    {
        Id = 1, HomeTeam = new() { Name = "Arsenal FC" }, AwayTeam = new() { Name = "Everton FC" },
        UtcDate = new DateTime(2026, 9, 26, 15, 0, 0, DateTimeKind.Utc), Status = "TIMED"
    };

    internal static Market Market(string key, params (string name, decimal price, decimal? point)[] outcomes) => new()
    {
        Key = key, Outcomes = outcomes.Select(o => new Outcome { Name = o.name, Price = o.price, Point = o.point }).ToList()
    };

    internal static MatchOddsResponse Event() => new()
    {
        Id = "event-1", HomeTeam = "Arsenal", AwayTeam = "Everton", CommenceTime = Fixture().UtcDate,
        Bookmakers = [new Bookmaker { Key = "one", Markets = [
            Market("h2h", ("Arsenal", 2m, null), ("Draw", 4m, null), ("Everton", 4m, null)),
            Market("totals", ("Over", 1.5m, 2.5m), ("Under", 3m, 2.5m)),
            Market("btts", ("Yes", 2.5m, null), ("No", 5m / 3m, null))] }]
    };

    [TestCase(2, 0.5)]
    [TestCase(4, 0.25)]
    public void DecimalOddsBecomeImpliedProbabilities(decimal odds, decimal expected) =>
        Assert.That(FixtureAnalysisCalculator.ImpliedProbability(odds), Is.EqualTo(expected));

    [Test]
    public void RemovesMarginFromAllThreeOutcomes()
    {
        var probabilities = FixtureAnalysisCalculator.RemoveMargin(2m, 3m, 4m)!;
        Assert.That(probabilities.Sum(), Is.EqualTo(1m).Within(0.0000001m));
        Assert.That(probabilities[0], Is.EqualTo(6m / 13m).Within(0.0000001m));
        Assert.That(probabilities[1], Is.EqualTo(4m / 13m).Within(0.0000001m));
    }

    [Test]
    public void AveragesNormalizedBookmakersInsteadOfPrices()
    {
        var odds = Event();
        odds.Bookmakers.Add(new Bookmaker { Key = "two", Markets = [
            Market("h2h", ("Arsenal", 2m, null), ("Draw", 3m, null), ("Everton", 4m, null)),
            Market("totals", ("Over", 2m, 2.5m), ("Under", 2m, 2.5m)),
            Market("btts", ("Yes", 2m, null), ("No", 2m, null))] });
        var result = FixtureAnalysisCalculator.Analyze(Fixture(), odds);
        Assert.That(result[0].WinProbability, Is.EqualTo(48));
        Assert.That(result[1].WinProbability, Is.EqualTo(24));
        Assert.That(result[0].Over25Probability, Is.EqualTo(58));
        Assert.That(result[0].BothTeamsToScoreProbability, Is.EqualTo(45));
    }

    [Test]
    public void OpportunityScoresUseTheirSpecifiedInputs()
    {
        Assert.That(FixtureAnalysisCalculator.CalculateAttackingPotential(0.71m, 0.64m), Is.EqualTo(0.675m));
        Assert.That(FixtureAnalysisCalculator.CalculateDefensivePotential(0.71m, 0.46m), Is.EqualTo(0.625m));
        Assert.That(FixtureAnalysisCalculator.CalculateAttackingPotential(null, 0.64m), Is.Null);
        Assert.That(FixtureAnalysisCalculator.CalculateDefensivePotential(0.71m, null), Is.Null);
    }

    [Test]
    public void ReturnsHomeAndAwayPerspectivesWithSharedFixtureMetrics()
    {
        var rows = FixtureAnalysisCalculator.Analyze(Fixture(), Event());
        Assert.That(rows, Has.Count.EqualTo(2));
        Assert.That(rows[0].Team, Is.EqualTo("Arsenal FC"));
        Assert.That(rows[0].Opponent, Is.EqualTo("Everton FC"));
        Assert.That(rows[0].Home, Is.True);
        Assert.That(rows[0].WinProbability, Is.EqualTo(50));
        Assert.That(rows[1].Team, Is.EqualTo("Everton FC"));
        Assert.That(rows[1].Opponent, Is.EqualTo("Arsenal FC"));
        Assert.That(rows[1].Home, Is.False);
        Assert.That(rows[1].WinProbability, Is.EqualTo(25));
        Assert.That(rows[1].Kickoff, Is.EqualTo(Fixture().UtcDate));
        Assert.That(rows[1].Over25Probability, Is.EqualTo(rows[0].Over25Probability));
        Assert.That(rows[1].BothTeamsToScoreProbability, Is.EqualTo(rows[0].BothTeamsToScoreProbability));
        Assert.That(rows[1].AttackingPotential, Is.EqualTo(46));
        Assert.That(rows[1].DefensivePotential, Is.EqualTo(43));
    }

    [TestCase("totals")]
    [TestCase("btts")]
    [TestCase("h2h")]
    public void MissingMarketLeavesDependentValuesNull(string key)
    {
        var odds = Event();
        odds.Bookmakers[0].Markets.RemoveAll(m => m.Key == key);
        foreach (var row in FixtureAnalysisCalculator.Analyze(Fixture(), odds))
        {
            if (key == "totals") { Assert.That(row.Over25Probability, Is.Null); Assert.That(row.AttackingPotential, Is.Null); Assert.That(row.DefensivePotential, Is.Not.Null); }
            if (key == "btts") { Assert.That(row.BothTeamsToScoreProbability, Is.Null); Assert.That(row.DefensivePotential, Is.Null); Assert.That(row.AttackingPotential, Is.Not.Null); }
            if (key == "h2h") { Assert.That(row.WinProbability, Is.Null); Assert.That(row.AttackingPotential, Is.Null); Assert.That(row.DefensivePotential, Is.Null); }
        }
    }

    [TestCase(0)]
    [TestCase(-2)]
    [TestCase(1)]
    [TestCase(0.5)]
    public void InvalidOddsInvalidateOnlyThatBookmakersMarket(decimal price)
    {
        Assert.That(FixtureAnalysisCalculator.ImpliedProbability(price), Is.Null);
        var odds = Event();
        odds.Bookmakers[0].Markets[0].Outcomes[0].Price = price;
        odds.Bookmakers.Add(Event().Bookmakers[0]);
        var row = FixtureAnalysisCalculator.Analyze(Fixture(), odds)[0];
        Assert.That(row.WinProbability, Is.EqualTo(50));
        Assert.That(row.Over25Probability, Is.Not.Null);
    }

    [Test]
    public void IncompleteDuplicateOrWrongLineMarketsAreIgnored()
    {
        var odds = Event();
        odds.Bookmakers[0].Markets[0].Outcomes.RemoveAt(1);
        odds.Bookmakers[0].Markets[1].Outcomes.ForEach(o => o.Point = 3.5m);
        odds.Bookmakers[0].Markets[2].Outcomes.Add(new Outcome { Name = "Yes", Price = 2m });
        var row = FixtureAnalysisCalculator.Analyze(Fixture(), odds)[0];
        Assert.That(row.WinProbability, Is.Null);
        Assert.That(row.Over25Probability, Is.Null);
        Assert.That(row.BothTeamsToScoreProbability, Is.Null);
    }

    [TestCase("AFC Bournemouth", "Bournemouth")]
    [TestCase("Brighton & Hove Albion FC", "Brighton and Hove Albion")]
    [TestCase("Wolverhampton Wanderers FC", "Wolves")]
    [TestCase("Manchester United FC", "Man Utd")]
    [TestCase("Nottingham Forest FC", "Nottingham Forest")]
    public void NormalizesProviderNames(string footballName, string oddsName) =>
        Assert.That(FixtureMatcher.NormalizeTeamName(footballName), Is.EqualTo(FixtureMatcher.NormalizeTeamName(oddsName)));

    [Test]
    public void MatchesOnlyUniqueNamesAndNearbyKickoffs()
    {
        var fixture = Fixture();
        var odds = Event();
        Assert.That(FixtureMatcher.FindMatch(fixture, [odds]), Is.SameAs(odds));
        Assert.That(FixtureMatcher.FindMatch(fixture, [odds, Event()]), Is.Null);
        odds.CommenceTime = fixture.UtcDate.AddMinutes(15);
        Assert.That(FixtureMatcher.FindMatch(fixture, [odds]), Is.SameAs(odds));
        odds.CommenceTime = fixture.UtcDate.AddMinutes(16);
        Assert.That(FixtureMatcher.FindMatch(fixture, [odds]), Is.Null);
        odds.CommenceTime = fixture.UtcDate;
        (odds.HomeTeam, odds.AwayTeam) = (odds.AwayTeam, odds.HomeTeam);
        Assert.That(FixtureMatcher.FindMatch(fixture, [odds]), Is.Null);
        odds.HomeTeam = "Arsenal Women";
        odds.AwayTeam = "Everton";
        Assert.That(FixtureMatcher.FindMatch(fixture, [odds]), Is.Null);
    }
}

