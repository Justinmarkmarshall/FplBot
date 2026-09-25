using System.Net;
using System.Text.Json;
using FplBot.Clients;
using FplBot.Configuration;
using FplBot.Controllers;
using FplBot.Model;
using FplBot.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace FplBot.UnitTests;

public class FixtureAnalysisServiceTests
{
    private sealed class Clock : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new(2026, 9, 25, 12, 0, 0, TimeSpan.Zero);
    }

    private sealed class Handler(Func<HttpRequestMessage, HttpResponseMessage> respond) : HttpMessageHandler
    {
        public List<Uri> Requests { get; } = [];
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Requests.Add(request.RequestUri!);
            return Task.FromResult(respond(request));
        }
    }

    private sealed class Factory(Handler handler, string oddsUrl) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new(handler, false)
        {
            BaseAddress = new Uri(name == "FootballDataClient" ? "https://football.test/v4/" : oddsUrl)
        };
    }

    private static HttpResponseMessage Json(object data) => new(HttpStatusCode.OK)
    {
        Content = new StringContent(JsonSerializer.Serialize(data))
    };

    private static (FixtureAnalysisService service, FootballDataClient football, OddsClient odds) Create(Handler handler,
        string oddsUrl = "https://odds.test/v4/sports/soccer_epl/odds")
    {
        var factory = new Factory(handler, oddsUrl);
        var football = new FootballDataClient(factory);
        var odds = new OddsClient(Options.Create(new OddsClientConfig { BaseUrl = oddsUrl, ApiToken = "test-token" }), factory);
        return (new FixtureAnalysisService(football, odds, new Clock(), NullLogger<FixtureAnalysisService>.Instance,
            new MemoryCache(new MemoryCacheOptions()), Options.Create(new OddsClientConfig())), football, odds);
    }

    [TestCase("https://odds.test/v4/sports/soccer_epl/odds")]
    [TestCase("https://odds.test/v4/sports/soccer_epl/odds/")]
    public async Task RetrievesBulkMarketsAndBttsOnceForBothTeams(string baseUrl)
    {
        using var handler = new Handler(request => request.RequestUri!.Host == "football.test"
            ? Json(new MatchesResponse { Matches = [FixtureAnalysisCalculatorTests.Fixture()] })
            : request.RequestUri.AbsolutePath.Contains("/events/") ? Json(FixtureAnalysisCalculatorTests.Event())
            : Json(new[] { FixtureAnalysisCalculatorTests.Event() }));
        var (service, football, odds) = Create(handler, baseUrl);
        var controller = new PremController(football, odds, new FplService());
        var response = await controller.GetFixtureAnalysis(service, CancellationToken.None);
        var rows = (List<FixtureAnalysisDto>)((OkObjectResult)response.Result!).Value!;
        Assert.That(rows, Has.Count.EqualTo(2));
        Assert.That(rows[0].WinProbability, Is.EqualTo(50));
        Assert.That(rows[1].BothTeamsToScoreProbability, Is.EqualTo(40));
        Assert.That(handler.Requests, Has.Count.EqualTo(3));
        Assert.That(handler.Requests[0].Query, Does.Contain("dateFrom=2026-09-25&dateTo=2026-10-25"));
        Assert.That(handler.Requests[1].Query, Does.Contain("oddsFormat=decimal&markets=h2h,totals"));
        Assert.That(handler.Requests[2].AbsolutePath, Is.EqualTo("/v4/sports/soccer_epl/events/event-1/odds"));
        Assert.That(handler.Requests[2].Query, Does.Contain("markets=btts"));
    }

    [Test]
    public async Task EmptyPeriodAvoidsOddsRequests()
    {
        using var handler = new Handler(_ => Json(new MatchesResponse()));
        var result = await Create(handler).service.AnalyzeUpcomingFixtures(CancellationToken.None);
        Assert.That(result, Is.Empty);
        Assert.That(handler.Requests, Has.Count.EqualTo(1));
    }

    [Test]
    public async Task WinningTeamsIncludesFixturesBeyondTwoWeeksWithExistingResponseFormat()
    {
        var fixture = FixtureAnalysisCalculatorTests.Fixture();
        fixture.UtcDate = DateTime.UtcNow.AddDays(20);
        var oddsEvent = FixtureAnalysisCalculatorTests.Event();
        oddsEvent.CommenceTime = fixture.UtcDate;
        oddsEvent.Bookmakers[0].Markets[0].Outcomes[0].Price = -200;
        oddsEvent.Bookmakers[0].Markets[0].Outcomes[2].Price = 300;
        using var handler = new Handler(request => request.RequestUri!.Host == "football.test"
            ? Json(new MatchesResponse { Matches = [fixture] }) : Json(new[] { oddsEvent }));
        var (_, football, odds) = Create(handler);
        var before = DateTime.UtcNow;
        var response = await new PremController(football, odds, new FplService())
            .GetMostWinningTeamsInTheNext30Days();
        var after = DateTime.UtcNow;
        var rows = (List<WinningTeamDto>)((OkObjectResult)response.Result!).Value!;
        Assert.That(rows, Has.Count.EqualTo(1));
        Assert.That(rows[0].TeamName, Is.EqualTo(fixture.HomeTeam.Name));
        Assert.That(rows[0].WinningMatches.Single().WinProbability, Is.EqualTo(67));
        Assert.That(rows[0].WinningMatches.Single().MatchDay, Is.EqualTo(fixture.UtcDate));
        Assert.That(handler.Requests[0].Query, Is.EqualTo($"?dateFrom={before:yyyy-MM-dd}&dateTo={before.AddDays(30):yyyy-MM-dd}")
            .Or.EqualTo($"?dateFrom={after:yyyy-MM-dd}&dateTo={after.AddDays(30):yyyy-MM-dd}"));
        Assert.That(handler.Requests[1].Query, Does.Contain("oddsFormat=american"));
    }

    [Test]
    public async Task WinningTeamsEmptyPeriodMentionsThirtyDays()
    {
        using var handler = new Handler(_ => Json(new MatchesResponse()));
        var (_, football, odds) = Create(handler);
        var response = await new PremController(football, odds, new FplService()).GetMostWinningTeamsInTheNext30Days();
        Assert.That(((NotFoundObjectResult)response.Result!).Value, Is.EqualTo("No matches found in the next 30 days."));
        Assert.That(handler.Requests, Has.Count.EqualTo(1));
    }

    [Test]
    public async Task WinningTeamsAuthenticationFailureDoesNotLookLikeEmptyPeriod()
    {
        using var handler = new Handler(_ => new(HttpStatusCode.Unauthorized));
        var (_, football, odds) = Create(handler);
        var response = await new PremController(football, odds, new FplService()).GetMostWinningTeamsInTheNext30Days();
        Assert.That(((ObjectResult)response.Result!).StatusCode, Is.EqualTo(503));
    }

    [Test]
    public async Task FootballClientFiltersExactUtcWindowAndUpcomingStatuses()
    {
        var from = new Clock().GetUtcNow().UtcDateTime;
        var fixtures = new[] { from.AddSeconds(-1), from, from.AddDays(30), from.AddDays(30).AddSeconds(1), from.AddDays(1) }
            .Select(date => { var m = FixtureAnalysisCalculatorTests.Fixture(); m.UtcDate = date; return m; }).ToList();
        fixtures[4].Status = "FINISHED";
        using var handler = new Handler(_ => Json(new MatchesResponse { Matches = fixtures }));
        var result = await Create(handler).football.GetUpcomingMatches(from, from.AddDays(30), CancellationToken.None);
        Assert.That(result.Select(m => m.UtcDate), Is.EqualTo(new[] { from, from.AddDays(30) }));
    }

    [TestCase(401)]
    [TestCase(429)]
    [TestCase(500)]
    public async Task FootballFailureReturnsSanitized503(int status)
    {
        using var handler = new Handler(_ => new HttpResponseMessage((HttpStatusCode)status)
            { Content = new StringContent("private-provider-detail test-token") });
        var (service, football, odds) = Create(handler);
        var controller = new PremController(football, odds, new FplService());
        var response = await controller.GetFixtureAnalysis(service, CancellationToken.None);
        var error = (ObjectResult)response.Result!;
        Assert.That(error.StatusCode, Is.EqualTo(503));
        Assert.That(JsonSerializer.Serialize(error.Value), Does.Not.Contain("private-provider-detail").And.Not.Contain("test-token"));
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task OddsFailureKeepsFixturesAndAvailableMarkets(bool bttsOnly)
    {
        using var handler = new Handler(request =>
        {
            if (request.RequestUri!.Host == "football.test") return Json(new MatchesResponse { Matches = [FixtureAnalysisCalculatorTests.Fixture()] });
            if (!bttsOnly || request.RequestUri.AbsolutePath.Contains("/events/")) return new(HttpStatusCode.Forbidden);
            var odds = FixtureAnalysisCalculatorTests.Event();
            odds.Bookmakers[0].Markets.RemoveAll(m => m.Key == "btts");
            return Json(new[] { odds });
        });
        var rows = await Create(handler).service.AnalyzeUpcomingFixtures(CancellationToken.None);
        Assert.That(rows, Has.Count.EqualTo(2));
        Assert.That(rows[0].BothTeamsToScoreProbability, Is.Null);
        Assert.That(rows[0].DefensivePotential, Is.Null);
        Assert.That(rows[0].WinProbability.HasValue, Is.EqualTo(bttsOnly));
        Assert.That(rows[0].AttackingPotential.HasValue, Is.EqualTo(bttsOnly));
    }

    [Test]
    public async Task UnmatchedFixtureDoesNotFetchBttsOrBorrowOdds()
    {
        var odds = FixtureAnalysisCalculatorTests.Event();
        odds.CommenceTime = odds.CommenceTime.AddDays(1);
        using var handler = new Handler(request => request.RequestUri!.Host == "football.test"
            ? Json(new MatchesResponse { Matches = [FixtureAnalysisCalculatorTests.Fixture()] }) : Json(new[] { odds }));
        var rows = await Create(handler).service.AnalyzeUpcomingFixtures(CancellationToken.None);
        Assert.That(rows, Has.Count.EqualTo(2));
        Assert.That(rows.All(r => r.WinProbability == null && r.Over25Probability == null && r.BothTeamsToScoreProbability == null), Is.True);
        Assert.That(handler.Requests, Has.Count.EqualTo(2));
    }

    [TestCase("not json")]
    [TestCase("null")]
    public void MalformedProviderResponsesAreSanitized(string content)
    {
        using var handler = new Handler(_ => new(HttpStatusCode.OK) { Content = new StringContent(content) });
        var (service, football, odds) = Create(handler);
        Assert.ThrowsAsync<ProviderUnavailableException>(async () => await service.AnalyzeUpcomingFixtures(CancellationToken.None));
        Assert.ThrowsAsync<ProviderUnavailableException>(async () => await odds.GetFixtureOdds(CancellationToken.None));
    }

    [Test]
    public void NetworkErrorsDoNotExposeRequestUrls()
    {
        using var handler = new Handler(_ => throw new HttpRequestException("URL with test-token"));
        var exception = Assert.ThrowsAsync<ProviderUnavailableException>(async () => await Create(handler).odds.GetFixtureOdds(CancellationToken.None));
        Assert.That(exception!.ToString(), Does.Not.Contain("test-token"));
    }

    [Test]
    public void RequestCancellationIsNotConvertedToProviderFailure()
    {
        using var handler = new Handler(_ => Json(new MatchesResponse()));
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        Assert.CatchAsync<OperationCanceledException>(async () => await Create(handler).service.AnalyzeUpcomingFixtures(cancellation.Token));
    }
}

