using System.Text.Json.Serialization;

namespace FplBot.Model
{
    public sealed class MatchesResponse
    {
        [JsonPropertyName("matches")]
        public List<Match> Matches { get; set; } = new();
    }

    public class Match
    {
        [JsonPropertyName("area")]
        public Area Area { get; set; } = new();

        [JsonPropertyName("competition")]
        public Competition Competition { get; set; } = new();

        [JsonPropertyName("season")]
        public Season Season { get; set; } = new();

        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("utcDate")]
        public DateTime UtcDate { get; set; }

        [JsonPropertyName("status")]
        public string Status { get; set; } = "";

        [JsonPropertyName("matchday")]
        public int? Matchday { get; set; }

        [JsonPropertyName("stage")]
        public string Stage { get; set; } = "";

        [JsonPropertyName("group")]
        public string? Group { get; set; }

        [JsonPropertyName("lastUpdated")]
        public DateTime LastUpdated { get; set; }

        [JsonPropertyName("homeTeam")]
        public Team HomeTeam { get; set; } = new();

        [JsonPropertyName("awayTeam")]
        public Team AwayTeam { get; set; } = new();

        [JsonPropertyName("score")]
        public Score Score { get; set; } = new();

        [JsonPropertyName("odds")]
        public Odds Odds { get; set; } = new();

        [JsonPropertyName("referees")]
        public List<Referee> Referees { get; set; } = new();
    }

    public class Area
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; } = "";

        [JsonPropertyName("code")]
        public string? Code { get; set; }

        [JsonPropertyName("flag")]
        public string? Flag { get; set; }
    }

    public class Competition
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; } = "";

        [JsonPropertyName("code")]
        public string? Code { get; set; }

        [JsonPropertyName("type")]
        public string? Type { get; set; }

        [JsonPropertyName("emblem")]
        public string? Emblem { get; set; }
    }

    public class Season
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        // Dates are provided as yyyy-MM-dd; DateTime is fine for deserialization.
        [JsonPropertyName("startDate")]
        public DateTime StartDate { get; set; }

        [JsonPropertyName("endDate")]
        public DateTime EndDate { get; set; }

        [JsonPropertyName("currentMatchday")]
        public int? CurrentMatchday { get; set; }

        // Often null; when present it's a team object.
        [JsonPropertyName("winner")]
        public Team? Winner { get; set; }
    }

    public class Team
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; } = "";

        [JsonPropertyName("shortName")]
        public string? ShortName { get; set; }

        [JsonPropertyName("tla")]
        public string? Tla { get; set; }

        [JsonPropertyName("crest")]
        public string? Crest { get; set; }
    }

    public class Score
    {
        // "HOME_TEAM" | "AWAY_TEAM" | "DRAW" | null
        [JsonPropertyName("winner")]
        public string? Winner { get; set; }

        // "REGULAR" | "EXTRA_TIME" | "PENALTY_SHOOTOUT"
        [JsonPropertyName("duration")]
        public string Duration { get; set; } = "";

        [JsonPropertyName("fullTime")]
        public ScoreTime FullTime { get; set; } = new();

        [JsonPropertyName("halfTime")]
        public ScoreTime HalfTime { get; set; } = new();
    }

    public class ScoreTime
    {
        [JsonPropertyName("home")]
        public int? Home { get; set; }

        [JsonPropertyName("away")]
        public int? Away { get; set; }
    }

    public class Odds
    {
        [JsonPropertyName("msg")]
        public string? Msg { get; set; }
    }

    public class Referee
    {
        [JsonPropertyName("id")]
        public int? Id { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("type")]
        public string? Type { get; set; }

        [JsonPropertyName("nationality")]
        public string? Nationality { get; set; }
    }
}
