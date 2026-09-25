using FplBot.Clients;
using FplBot.Configuration;
using FplBot.Model;
using FplBot.Utilities;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace FplBot.Services;

public interface IFixtureAnalysisService
{
    Task<List<FixtureAnalysisDto>> AnalyzeUpcomingFixtures(CancellationToken cancellationToken);
}

public class FixtureAnalysisService(IFootballDataClient footballDataClient, IOddsClient oddsClient,
    TimeProvider timeProvider, ILogger<FixtureAnalysisService> logger, IMemoryCache cache,
    IOptions<OddsClientConfig> options) : IFixtureAnalysisService, IDisposable
{
    private readonly int maxConcurrency = options.Value.BttsMaxConcurrency;
    private readonly TimeSpan cacheTtl = TimeSpan.FromMinutes(options.Value.BttsCacheMinutes);
    // Shared by all endpoint invocations: the limit applies to this application instance.
    private readonly SemaphoreSlim requestSlots = new(options.Value.BttsMaxConcurrency);
    // Fixed lock stripes coalesce concurrent misses without an ever-growing per-event lock map.
    private readonly SemaphoreSlim[] eventLocks = Enumerable.Range(0, 64).Select(_ => new SemaphoreSlim(1)).ToArray();
    private sealed record BttsResult(MatchOddsResponse? Odds);

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

        var results = new List<FixtureAnalysisDto>[fixtures.Count];
        await Parallel.ForEachAsync(Enumerable.Range(0, fixtures.Count), new ParallelOptions
        {
            MaxDegreeOfParallelism = maxConcurrency,
            CancellationToken = cancellationToken
        }, async (index, token) =>
        {
            var fixture = fixtures[index];
            var matched = FixtureMatcher.FindMatch(fixture, events);
            if (matched == null)
            {
                logger.LogWarning("No unique odds event for fixture {FixtureId} at {Kickoff}.", fixture.Id, fixture.UtcDate);
            }
            else if (!string.IsNullOrWhiteSpace(matched.Id))
            {
                var extra = await GetCachedBtts(matched.Id, token);
                // Revalidate on cache hits too: kickoff/team data may have changed since retrieval.
                if (extra != null && (extra.Id != matched.Id || FixtureMatcher.FindMatch(fixture, [extra]) == null))
                {
                    logger.LogWarning("BTTS event mismatch for fixture {FixtureId}.", fixture.Id);
                    extra = null;
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
            results[index] = FixtureAnalysisCalculator.Analyze(fixture, matched);
        });
        return results.SelectMany(rows => rows).ToList();
    }

    private async Task<MatchOddsResponse?> GetCachedBtts(string eventId, CancellationToken cancellationToken)
    {
        var key = ("FixtureAnalysis.Btts", eventId);
        if (cache.TryGetValue<BttsResult>(key, out var cached)) return cached!.Odds;

        var eventLock = eventLocks[(uint)StringComparer.Ordinal.GetHashCode(eventId) % (uint)eventLocks.Length];
        await eventLock.WaitAsync(cancellationToken);
        try
        {
            if (cache.TryGetValue<BttsResult>(key, out cached)) return cached!.Odds;
            await requestSlots.WaitAsync(cancellationToken);
            try
            {
                MatchOddsResponse? extra;
                try
                {
                    extra = await oddsClient.GetBothTeamsToScoreOdds(eventId, cancellationToken);
                    if (extra.Id != eventId)
                    {
                        logger.LogWarning("BTTS event mismatch for requested event {EventId}.", eventId);
                        extra = null;
                    }
                    else if (!extra.Bookmakers.Any(b => b.Markets.Any(m => m.Key == "btts")))
                        extra = null;
                }
                catch (ProviderUnavailableException)
                {
                    logger.LogWarning("BTTS unavailable for event {EventId}; retaining other markets.", eventId);
                    extra = null;
                }
                // Cache a wrapper so an intentional unavailable result is distinct from a miss.
                // Caller cancellation propagates and never populates the cache.
                cancellationToken.ThrowIfCancellationRequested();
                cache.Set(key, new BttsResult(extra), cacheTtl);
                return extra;
            }
            finally { requestSlots.Release(); }
        }
        finally { eventLock.Release(); }
    }

    public void Dispose()
    {
        requestSlots.Dispose();
        foreach (var eventLock in eventLocks) eventLock.Dispose();
    }
}
