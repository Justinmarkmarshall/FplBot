using FplBot.Clients;
using FplBot.Model;
using FplBot.Utilities;

namespace FplBot.Services;

public interface IFixtureAnalysisService
{
    Task<List<FixtureAnalysisDto>> AnalyzeUpcomingFixtures(CancellationToken cancellationToken);
}

public class FixtureAnalysisService(IFootballDataClient footballDataClient, IOddsClient oddsClient,
    TimeProvider timeProvider, ILogger<FixtureAnalysisService> logger) : IFixtureAnalysisService
{
    public async Task<List<FixtureAnalysisDto>> AnalyzeUpcomingFixtures(CancellationToken cancellationToken)
    {
        var from = timeProvider.GetUtcNow().UtcDateTime;
        List<Match> fixtures;
        try
        {
            fixtures = await footballDataClient.GetUpcomingMatches(from, from.AddDays(30), cancellationToken);
        }
        catch (ProviderUnavailableException)
        {
            logger.LogWarning("FootballData unavailable during fixture analysis.");
            throw;
        }
        if (fixtures.Count == 0) return [];

        List<MatchOddsResponse> events;
        try
        {
            events = await oddsClient.GetFixtureOdds(cancellationToken);
        }
        catch (ProviderUnavailableException)
        {
            logger.LogWarning("OddsAPI unavailable; returning fixtures with null market metrics.");
            events = [];
        }

        var results = new List<FixtureAnalysisDto>();
        var bttsCache = new Dictionary<string, MatchOddsResponse?>();
        foreach (var fixture in fixtures)
        {
            var matched = FixtureMatcher.FindMatch(fixture, events);
            if (matched == null)
            {
                logger.LogWarning("No unique odds event for fixture {FixtureId} at {Kickoff}.", fixture.Id, fixture.UtcDate);
            }
            else if (!string.IsNullOrWhiteSpace(matched.Id))
            {
                if (!bttsCache.TryGetValue(matched.Id, out var extra))
                {
                    try
                    {
                        extra = await oddsClient.GetBothTeamsToScoreOdds(matched.Id, cancellationToken);
                        if (extra.Id != matched.Id || FixtureMatcher.FindMatch(fixture, [extra]) == null)
                        {
                            logger.LogWarning("BTTS event mismatch for fixture {FixtureId}.", fixture.Id);
                            extra = null;
                        }
                    }
                    catch (ProviderUnavailableException)
                    {
                        logger.LogWarning("BTTS unavailable for fixture {FixtureId}; retaining other markets.", fixture.Id);
                        extra = null;
                    }
                    bttsCache[matched.Id] = extra;
                }
                if (extra != null)
                {
                    // Add BTTS-only bookmaker entries; each market is averaged independently.
                    matched = new MatchOddsResponse
                    {
                        Bookmakers = matched.Bookmakers.Select(b => new Bookmaker
                        {
                            Key = b.Key, Markets = b.Markets.Where(m => m.Key != "btts").ToList()
                        }).Concat(extra.Bookmakers.Select(b => new Bookmaker
                        {
                            Key = b.Key, Markets = b.Markets.Where(m => m.Key == "btts").ToList()
                        })).ToList()
                    };
                }
            }
            results.AddRange(FixtureAnalysisCalculator.Analyze(fixture, matched));
        }
        return results;
    }
}
