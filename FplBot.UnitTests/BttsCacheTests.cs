using System.Collections.Concurrent;
using FplBot.Clients;
using FplBot.Configuration;
using FplBot.Model;
using FplBot.Services;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace FplBot.UnitTests;

public class BttsCacheTests
{
    private sealed class CacheClock : Microsoft.Extensions.Internal.ISystemClock
    {
        public DateTimeOffset UtcNow { get; set; } = DateTimeOffset.UtcNow;
    }

    private sealed class FootballStub(int fixtureCount) : IFootballDataClient
    {
        public Task<List<Match>> GetUpcomingMatches(DateTime from, DateTime to, CancellationToken cancellationToken) =>
            Task.FromResult(Enumerable.Range(0, fixtureCount).Select(i =>
            {
                var fixture = FixtureAnalysisCalculatorTests.Fixture();
                fixture.Id = i;
                fixture.UtcDate = fixture.UtcDate.AddHours(i);
                return fixture;
            }).ToList());

        public Task<List<Match>> GetMatchesInTheNext2Weeks() => throw new NotSupportedException();
        public Task<List<PlayerDto>> GetPlayersOnWinningTeams(List<Tuple<string, int>> teams) => throw new NotSupportedException();
    }

    private sealed class OddsStub(int eventCount, Func<string, CancellationToken, Task<MatchOddsResponse>> fetch) : IOddsClient
    {
        public ConcurrentDictionary<string, int> Calls { get; } = new();
        public Task<List<MatchOddsResponse>> GetFixtureOdds(CancellationToken cancellationToken) =>
            Task.FromResult(Enumerable.Range(0, eventCount).Select(i =>
            {
                var odds = Event(i.ToString());
                odds.Bookmakers[0].Markets.RemoveAll(m => m.Key == "btts");
                return odds;
            }).ToList());

        public Task<MatchOddsResponse> GetBothTeamsToScoreOdds(string eventId, CancellationToken cancellationToken)
        {
            Calls.AddOrUpdate(eventId, 1, (_, count) => count + 1);
            return fetch(eventId, cancellationToken);
        }

        public Task<List<MatchOddsDto>> GetOddsForMatches(List<MatchOddsDto> matches) => throw new NotSupportedException();
    }

    private static MatchOddsResponse Event(string id)
    {
        var odds = FixtureAnalysisCalculatorTests.Event();
        odds.Id = id;
        odds.CommenceTime = odds.CommenceTime.AddHours(int.Parse(id));
        return odds;
    }

    private static FixtureAnalysisService Service(IMemoryCache cache, OddsStub odds, int fixtures = 1, int concurrency = 3) =>
        new(new FootballStub(fixtures), odds, TimeProvider.System, NullLogger<FixtureAnalysisService>.Instance,
            cache, Options.Create(new OddsClientConfig { BttsCacheMinutes = 15, BttsMaxConcurrency = concurrency }));

    [TestCase("available")]
    [TestCase("unavailable")]
    [TestCase("empty")]
    public async Task RepeatedServiceCallsReusePositiveAndNegativeResults(string response)
    {
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var odds = new OddsStub(1, (id, _) =>
        {
            if (response == "unavailable") throw new ProviderUnavailableException("OddsAPI");
            var result = Event(id);
            if (response == "empty") result.Bookmakers.Clear();
            return Task.FromResult(result);
        });
        using var service = Service(cache, odds);
        var first = await service.AnalyzeUpcomingFixtures(CancellationToken.None);
        var second = await service.AnalyzeUpcomingFixtures(CancellationToken.None);
        Assert.That(odds.Calls["0"], Is.EqualTo(1));
        Assert.That(second, Has.Count.EqualTo(2));
        Assert.That(second[0].BothTeamsToScoreProbability, Is.EqualTo(first[0].BothTeamsToScoreProbability));
        Assert.That(second[0].BothTeamsToScoreProbability.HasValue, Is.EqualTo(response == "available"));
        Assert.That(second[0].WinProbability, Is.EqualTo(50));
    }

    [Test]
    public async Task CachedResultsExpireAfterConfiguredTtl()
    {
        var clock = new CacheClock();
        using var cache = new MemoryCache(new MemoryCacheOptions { Clock = clock });
        var odds = new OddsStub(1, (id, _) => Task.FromResult(Event(id)));
        using var service = Service(cache, odds);
        await service.AnalyzeUpcomingFixtures(CancellationToken.None);
        clock.UtcNow = clock.UtcNow.AddMinutes(14);
        await service.AnalyzeUpcomingFixtures(CancellationToken.None);
        Assert.That(odds.Calls["0"], Is.EqualTo(1));
        clock.UtcNow = clock.UtcNow.AddMinutes(2);
        await service.AnalyzeUpcomingFixtures(CancellationToken.None);
        Assert.That(odds.Calls["0"], Is.EqualTo(2));
    }

    [Test]
    public async Task OverlappingInvocationsShareOneFetchForTheSameEvent()
    {
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var odds = new OddsStub(1, async (id, token) =>
        {
            started.TrySetResult();
            await release.Task.WaitAsync(token);
            return Event(id);
        });
        using var service = Service(cache, odds);
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        var first = service.AnalyzeUpcomingFixtures(timeout.Token);
        await started.Task.WaitAsync(timeout.Token);
        var second = service.AnalyzeUpcomingFixtures(timeout.Token);
        release.SetResult();
        await Task.WhenAll(first, second);
        Assert.That(odds.Calls["0"], Is.EqualTo(1));
    }

    [Test]
    public async Task ConcurrencyIsBoundedAcrossOverlappingEndpointInvocationsAndOrderIsPreserved()
    {
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var active = 0;
        var maximum = 0;
        var sync = new object();
        var odds = new OddsStub(12, async (id, token) =>
        {
            lock (sync) { active++; maximum = Math.Max(maximum, active); }
            try
            {
                await Task.Delay(50, token);
                return Event(id);
            }
            finally { lock (sync) active--; }
        });
        using var service = Service(cache, odds, fixtures: 12, concurrency: 3);
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        var results = await Task.WhenAll(service.AnalyzeUpcomingFixtures(timeout.Token), service.AnalyzeUpcomingFixtures(timeout.Token));
        Assert.That(maximum, Is.InRange(2, 3));
        Assert.That(odds.Calls, Has.Count.EqualTo(12));
        Assert.That(odds.Calls.Values, Is.All.EqualTo(1));
        foreach (var rows in results)
        {
            Assert.That(rows, Has.Count.EqualTo(24));
            Assert.That(rows.Select(r => r.Kickoff), Is.Ordered);
            Assert.That(rows.Where((_, i) => i % 2 == 0).All(r => r.Home), Is.True);
        }
    }

    [Test]
    public async Task CancellationDoesNotCacheUnavailableAndReleasesRequestSlot()
    {
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var calls = 0;
        var odds = new OddsStub(1, async (id, token) =>
        {
            if (Interlocked.Increment(ref calls) == 1)
            {
                started.SetResult();
                await Task.Delay(Timeout.InfiniteTimeSpan, token);
            }
            return Event(id);
        });
        using var service = Service(cache, odds, concurrency: 1);
        using var canceled = new CancellationTokenSource();
        var first = service.AnalyzeUpcomingFixtures(canceled.Token);
        await started.Task.WaitAsync(TimeSpan.FromSeconds(5));
        canceled.Cancel();
        Assert.CatchAsync<OperationCanceledException>(async () => await first);
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var second = await service.AnalyzeUpcomingFixtures(timeout.Token);
        Assert.That(odds.Calls["0"], Is.EqualTo(2));
        Assert.That(second[0].BothTeamsToScoreProbability, Is.EqualTo(40));
    }
}
