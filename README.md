# FplBot

A .NET 10 ASP.NET Core web API for Fantasy Premier League data, integrating with Football Data API and The Odds API.

## 🚀 Quick Start

### Prerequisites
- .NET 10 SDK
- Docker (optional)
- Kubernetes cluster (optional)
- API tokens for:
  - [Football Data API](https://www.football-data.org/client/register)
  - [The Odds API](https://the-odds-api.com/)

### Local Development

1. **Clone and setup:**
   ```bash
   git clone <your-repo-url>
   cd FplBot
   cp FplBot/appsettings.example.json FplBot/appsettings.Development.json
   ```

2. **Configure API keys:**
   Edit `FplBot/appsettings.Development.json` with your API tokens:
   ```json
   {
     "FootballData": {
       "BaseUrl": "https://api.football-data.org/v4/",
       "ApiToken": "your-football-data-token"
     },
     "OddsAPI": {
       "BaseUrl": "https://api.the-odds-api.com/v4/sports/soccer_epl/odds",
       "ApiToken": "your-odds-api-token"
     }
   }
   ```

3. **Run the application:**
   ```bash
   cd FplBot
   dotnet run
   ```

4. **Test the API:**
   - Health check: http://localhost:5000/health
   - Premier League teams: http://localhost:5000/Prem/WinningTeams
   - Fixture analysis: http://localhost:5000/Prem/FixtureAnalysis

### GET /Prem/WinningTeams

Returns the existing winning-team predictions for scheduled/timed fixtures in the
next 30 days from the current UTC instant. Win probabilities are returned as whole-number
percentages in `winPercentage` (for example, `72` means 72%) using the existing American-odds calculation.
An empty period returns HTTP 404; FootballData failures
return a generic HTTP 503 rather than appearing as an empty fixture period.

### Visual Studio API tokens

For local development, right-click the FplBot project and select **Manage User Secrets**.
Store the tokens under the configuration sections used by the application:

```json
{
  "FootballData": { "ApiToken": "<your FootballData token>" },
  "OddsAPI": { "ApiToken": "<your Odds API token>" }
}
```

The project is configured to load User Secrets in Development, which all included
Visual Studio launch profiles select. Restart debugging after configuring the tokens.
`FOOTBALL_DATA_API_TOKEN` and `ODDS_API_TOKEN`, when set in the application process,
override these values. A `.env` file is not automatically loaded by this application.

### GET /Prem/FixtureAnalysis

Identifies upcoming fixtures with attractive attacking and defensive opportunities
for FPL planning. Higher potential scores indicate stronger opportunities under the
application's scoring model; they do not predict an individual player's FPL points.

Returns two rows per upcoming Premier League fixture, one for each team, ordered by
kickoff (home row first). FootballData supplies fixtures with `SCHEDULED` or `TIMED`
status between the current UTC instant and 30 days later, inclusive. An empty period
returns HTTP 200 with `[]`.

The endpoint takes no request body or query parameters. The 30-day horizon is fixed.
Configure the provider tokens on the server as described above; callers do not supply
FootballData or Odds API tokens in this request.

Start the included HTTP development profile and call the endpoint:

```bash
dotnet run --project FplBot --launch-profile http
curl -H "Accept: application/json" http://localhost:5161/Prem/FixtureAnalysis
```

Use the application's actual listening address when running another launch profile
or a deployed instance.

| Field | Meaning |
| --- | --- |
| `team`, `opponent` | FootballData team names |
| `home` | Whether this row describes the home team |
| `kickoff` | Fixture kickoff in UTC |
| `winPercentage` | Team's implied win percentage from decimal h2h odds, including the draw in margin removal |
| `threeOrMoreGoalsPercentage` | Fixture-level percentage for three or more goals (Over 2.5 market) |
| `bothTeamsToScorePercentage` | Fixture-level percentage for BTTS Yes |
| `attackingPotential` | Average of win percentage and three-or-more-goals percentage, using unrounded inputs |
| `defensivePotential` | Average of win percentage and `100 - bothTeamsToScorePercentage`, using unrounded inputs |

All metrics are whole-number percentages from 0 to 100, or `null` when the required
markets are unavailable. For example, `71` means 71%. Calculations use full-precision
probabilities from 0 to 1 internally, with equivalent scoring formulas. Only the final
response values are multiplied by 100 and rounded to the nearest integer, with
midpoints rounded up (`0.625` becomes `63`). Scores are calculated before rounding
their inputs. Probabilities are derived from bookmaker odds, not guaranteed outcomes.
For each bookmaker and market, decimal prices are inverted (`1 / price`), then
normalized by the sum of the implied probabilities of all required outcomes.
The resulting probabilities are averaged equally across bookmakers providing a
complete valid market. Margin is removed **before** averaging. Invalid prices
(including zero and prices at or below 1), incomplete markets, duplicate outcomes,
and totals at lines other than 2.5 are excluded from the relevant calculation.

AttackingPotential and DefensivePotential are application-defined FPL indicators,
not official bookmaker statistics. DefensivePotential is **not a clean-sheet
probability**. Each score is null if either of its inputs is missing. Both teams
share the same totals and BTTS probabilities.

Example HTTP 200 response (illustrative values, not live odds):

```json
[
  {
    "team": "Arsenal FC",
    "opponent": "Everton FC",
    "home": true,
    "kickoff": "2026-09-26T15:00:00Z",
    "winPercentage": 71,
    "threeOrMoreGoalsPercentage": 64,
    "bothTeamsToScorePercentage": 46,
    "attackingPotential": 68,
    "defensivePotential": 63
  },
  {
    "team": "Everton FC",
    "opponent": "Arsenal FC",
    "home": false,
    "kickoff": "2026-09-26T15:00:00Z",
    "winPercentage": 11,
    "threeOrMoreGoalsPercentage": 64,
    "bothTeamsToScorePercentage": 46,
    "attackingPotential": 38,
    "defensivePotential": 33
  }
]
```

The two win percentages need not total 100 because the h2h market also includes a draw.
Clients must distinguish `null` (unavailable) from `0` (a calculated value rounded to zero).

| Condition | HTTP status | Response behavior |
| --- | --- | --- |
| Upcoming fixtures found | 200 | Two team rows per fixture |
| No upcoming fixtures | 200 | Empty array `[]` |
| FootballData unavailable, including authentication failure | 503 | Problem response titled `Fixture data is temporarily unavailable.` |
| Bulk Odds API request fails, or a fixture cannot be uniquely matched | 200 | Affected fixtures retained with null market metrics and scores |
| Totals market unavailable | 200 | `threeOrMoreGoalsPercentage` and `attackingPotential` are null |
| BTTS unavailable | 200 | `bothTeamsToScorePercentage` and `defensivePotential` are null |
| h2h unavailable | 200 | `winPercentage` and both potential scores are null; other valid markets remain available |

The existing named clients and API configuration/environment variables are reused.
The Odds API is queried once for bulk `h2h,totals` in UK-region decimal format.
Its [additional markets API](https://the-odds-api.com/sports-odds-data/betting-markets.html)
requires BTTS to be requested once per matched event; that response serves both
teams. Subscription access, remaining quota and bookmaker coverage determine
whether BTTS is available. No player-prop markets are requested. These requests
consume the configured subscription's quota.

Matching uses home and away names normalized through a central explicit alias map,
plus kickoff timestamps within 15 minutes. IDs are never compared across providers.
Unknown names require an exact normalized match; ambiguous or unmatched fixtures
are logged by fixture ID and kickoff and returned with null metrics. The alias map
is in `FplBot/Utilities/FixtureMatcher.cs` and can be extended as provider names evolve.

FootballData failures produce HTTP 503 with a generic problem response. If bulk
odds fail, fixtures are still returned with null metrics; if a BTTS request fails,
other available markets are retained. Diagnostics omit provider response bodies,
exceptions and tokens. Automatic Odds HTTP request logging is disabled because the
provider authenticates via a query parameter. `/Prem/WinningTeams` retains its
existing odds format, matching and calculations.

Run the NUnit suite with `dotnet test FplBot.sln`. Tests use stubbed HTTP responses
and do not call either provider. No live subscription coverage is assumed by tests.

### Docker Deployment

#### Using Pre-built Images (Recommended)
```bash
# Pull from GitHub Container Registry (automatically built on every commit)
docker pull ghcr.io/justinmarkmarshall/fplbot:latest

# Run container
docker run -p 8080:8080 \
  -e FOOTBALL_DATA_API_TOKEN="your-football-token" \
  -e ODDS_API_TOKEN="your-odds-token" \
  ghcr.io/justinmarkmarshall/fplbot:latest
```

#### Building Locally
```bash
# Build image locally
docker build -t fplbot:latest .

# Run container
docker run -p 8080:8080 \
  -e FOOTBALL_DATA_API_TOKEN="your-football-token" \
  -e ODDS_API_TOKEN="your-odds-token" \
  fplbot:latest
```

### Kubernetes Deployment

See the comprehensive [Helm chart documentation](helm/fplbot/README.md) for Kubernetes deployment.

**Quick deploy:**
```bash
# Using environment variables
helm install fplbot helm/fplbot \
  --set-string config.footballData.apiToken="your-football-token" \
  --set-string config.oddsApi.apiToken="your-odds-token"
```

## 📁 Project Structure

```
FplBot/
├── Controllers/         # API controllers
├── Services/           # Business logic
├── Clients/            # External API clients
├── Model/              # Data models
├── Configuration/      # App configuration
├── Utilities/          # Helper utilities
└── Properties/         # Launch settings

FplBot.UnitTests/       # Unit tests
helm/fplbot/           # Kubernetes Helm chart
.github/workflows/     # GitHub Actions CI/CD
```

## 🚀 Automated CI/CD

Every commit automatically:
- ✅ Builds multi-platform Docker images
- ✅ Publishes to GitHub Container Registry (GHCR)
- ✅ Tags with branch and commit info
- ✅ Runs security scans

See [GITHUB_ACTIONS.md](GITHUB_ACTIONS.md) for setup details.

## 🔒 Security

This repository is configured for safe public hosting:
- ✅ No API tokens in source code
- ✅ Environment variable configuration
- ✅ Kubernetes secrets integration
- ✅ Comprehensive .gitignore
- ✅ Automated security scanning

See [GITHUB_SECURITY.md](GITHUB_SECURITY.md) for detailed security information.

## 🧪 Testing

```bash
# Run unit tests
dotnet test

# Run specific test project
dotnet test FplBot.UnitTests/
```

## 📊 API Endpoints

- `GET /health` - Health check endpoint
- `GET /health/ready` - Readiness probe
- `GET /Prem/WinningTeams` - Get Premier League winning team predictions
- `GET /Prem/FixtureAnalysis` - Get attacking and defensive analysis for both teams in each upcoming fixture; see [endpoint documentation](#get-premfixtureanalysis)

## Upcoming features (not yet implemented)

The following milestones are planned work. Their endpoints, query parameters and
response examples describe proposed contracts and are not currently available.

### Milestone 2 — Team Analysis

**Goal:** Build on FixtureAnalysis to identify the strongest attacking and defensive
fixture runs for transfer planning over the next few gameweeks.

**Planned endpoint:** `GET /Prem/TeamAnalysis?fixtures=3`, with a configurable number
of upcoming fixtures and a documented default, initially the next three fixtures.

Reuse FixtureAnalysis's domain logic and existing FootballData/Odds API integrations.
TeamAnalysis should aggregate fixture metrics, home/away status, kickoff and odds
availability rather than fetching the same data or recalculating bookmaker
probabilities independently. Keep the controller thin and aggregation isolated in
a testable TeamAnalysisService, with room for future fixture weighting.

For each team, collect its next N fixtures and calculate arithmetic means of the
available values for each metric:

- `averageWinPercentage`
- `averageAttackingPotential`
- `averageDefensivePotential`
- `averageThreeOrMoreGoalsPercentage`
- `averageBothTeamsToScorePercentage`

Return `fixturesAnalysed`, `fixturesWithOdds` and the underlying fixture details so
callers can assess coverage. Retain fixtures without odds in the run, exclude missing
values from each metric's average, and return null when no values are available.
Teams with fewer than N upcoming fixtures must be handled safely.

Allow consumers to compare attacking runs, defensive runs and average win
expectation separately. Do not introduce a single opaque overall team score.

Illustrative response (fixture details abbreviated):

```json
[
  {
    "team": "Arsenal FC",
    "fixturesAnalysed": 3,
    "fixturesWithOdds": 3,
    "averageWinPercentage": 64,
    "averageAttackingPotential": 59,
    "averageDefensivePotential": 57,
    "averageThreeOrMoreGoalsPercentage": 55,
    "averageBothTeamsToScorePercentage": 49,
    "fixtures": [
      {
        "opponent": "Leeds United FC",
        "home": true,
        "kickoff": "2026-10-10T11:30:00Z",
        "winPercentage": 68,
        "attackingPotential": 62,
        "defensivePotential": 61
      }
    ]
  }
]
```

**Required tests:** Multiple fixtures for one team; correct attacking, defensive and
win averages; mixed home/away runs; missing values excluded rather than counted as
zero; partial and absent odds coverage; configurable fixture counts; and teams with
fewer fixtures than requested. Existing fixture-analysis tests must keep passing.

**Definition of done:** The endpoint reuses FixtureAnalysis, exposes comparable
attacking/defensive runs and coverage metadata, handles missing data safely, has a
configurable or clearly documented default horizon, and has unit tests and API
documentation. Existing endpoints and tests remain unaffected. This service becomes
the bridge from fixture analysis to player analysis.

### Milestone 3 — Player Analysis

**Goal:** Combine individual FPL player data with fixture/team opportunities to help
identify players who can benefit from upcoming fixtures. Provide inputs for transfer,
starting-XI and future captaincy analysis without claiming a perfect expected-points model.

**Planned endpoint and optional filters:**

```http
GET /Prem/PlayerAnalysis?fixtures=3
GET /Prem/PlayerAnalysis?position=MID&fixtures=3
GET /Prem/PlayerAnalysis?team=Arsenal
```

Follow existing API parameter and validation conventions. Reuse FixtureAnalysis and
TeamAnalysis, and add or reuse an FPL integration for player ID/name, team, position,
price, total points, form, minutes, starts, selected-by percentage, goals, assists,
clean sheets, bonus, availability/status, and expected goals/assists where supplied.
Do not invent unavailable statistics.

Match FPL team identities deterministically to the teams used by FootballData/Odds
API. Centralize reusable name normalization and mappings. For an unmapped player,
omit the fixture-derived score and log a diagnostic instead of attaching another
team's analysis.

**Position-aware scoring:**

- Goalkeepers and defenders: prioritize team defensive potential, minutes/starts,
  form and clean-sheet record, with attacking contributions where available.
- Midfielders and forwards: prioritize team attacking potential, minutes/starts,
  form, goals/assists and expected goals/assists where available.
- Inspect each source field's scale and meaning before defining formulas. Normalize
  inputs before combining percentages, form, minutes and individual output. Keep
  scoring in isolated calculators and document formulas and weights in code and README.
- Label the result `opportunityScore`, an application-defined indicator rather than
  official FPL expected points. Retain underlying metrics so rankings are explainable.
- Guard against very low minutes or unreliable starts producing unrealistic rankings.
  Expose supplied injury/availability metadata; never infer injury from missing minutes.

Illustrative response, subject to the available FPL fields and existing conventions:

```json
[
  {
    "playerId": 123,
    "player": "Example Player",
    "team": "Arsenal FC",
    "position": "MID",
    "price": 9.5,
    "form": 7.2,
    "minutes": 612,
    "selectedByPercentage": 18.4,
    "teamAttackingPotential": 59,
    "teamDefensivePotential": 57,
    "opportunityScore": 74,
    "fixturesAnalysed": 3,
    "fixturesWithOdds": 3
  }
]
```

Enable comparison by position, team, price and opportunity score. Expose price for
future value analysis; optionally add `valueScore = opportunityScore / price` only
when the scales make it meaningful and its interpretation is documented. It must
not replace the raw opportunity score.

Keep external retrieval, team matching, normalization and player scoring independently
testable, with a thin endpoint over PlayerAnalysisService. The intended flow is
FootballData/OddsAPI → FixtureAnalysisService → TeamAnalysisService, combined with
FPL player data → PlayerAnalysisService → `/Prem/PlayerAnalysis`.

**Required tests:** Player/team matching; attacker, defender and goalkeeper scoring;
form normalization; minutes/starts effects; missing fixture odds and optional player
statistics; injured/unavailable metadata; position/team filters; configurable fixture
horizons; low-minute ranking safeguards; and unmapped teams. Mock external responses;
unit tests must not call live FPL, FootballData or Odds APIs.

**Definition of done:** The endpoint combines FPL data with team opportunity over a
configurable horizon, uses transparent position-appropriate scoring, considers playing
time and availability, centralizes team mapping, handles missing data safely, and has
unit tests and documentation covering inputs and scoring. FixtureAnalysis and
TeamAnalysis behavior remains unaffected.

**Later features:** Transfer targets and comparisons, starting-XI decisions, captaincy,
fixture swings, budget/value analysis and multi-gameweek squad planning should consume
PlayerAnalysis. A separate captaincy engine is outside this milestone; retain attacking
opportunity, fixture potential, minutes, form and individual attacking statistics so a
later feature can reuse them.

## 🛠️ Development

### Adding New Features

1. Create feature branch
2. Add tests in `FplBot.UnitTests/`
3. Implement feature
4. Update documentation
5. Submit pull request

### Configuration

The application uses a layered configuration approach:
1. `appsettings.json` - Base configuration
2. `appsettings.Development.json` - Development overrides
3. Environment variables - Runtime overrides (Kubernetes/Docker)

## 📦 Dependencies

- ASP.NET Core 9.0
- Microsoft.Extensions.Http
- Microsoft.AspNetCore.OpenApi
- Scalar.AspNetCore

## 🚢 Deployment Environments

- **Development**: Local with `appsettings.Development.json`
- **Docker**: Container with environment variables
- **Kubernetes**: Helm chart with secrets management

## 📝 License

[Add your license here]

## 🤝 Contributing

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Add tests
5. Submit a pull request

## 📞 Support

For issues and questions:
- Create an issue in this repository
- Check the [security documentation](GITHUB_SECURITY.md)
- Review the [Helm chart README](helm/fplbot/README.md)
