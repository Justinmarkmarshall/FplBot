using System.Text.Json.Serialization;

namespace FplBot.Model
{
    public class MatchOddsResponse
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("sport_key")]
        public string SportKey { get; set; } = string.Empty;

        [JsonPropertyName("sport_title")]
        public string SportTitle { get; set; } = string.Empty;

        [JsonPropertyName("commence_time")]
        public DateTime CommenceTime { get; set; }

        [JsonPropertyName("home_team")]
        public string HomeTeam { get; set; } = string.Empty;

        [JsonPropertyName("away_team")]
        public string AwayTeam { get; set; } = string.Empty;

        [JsonPropertyName("bookmakers")]
        public List<Bookmaker> Bookmakers { get; set; } = new();
    }

    public class Bookmaker
    {
        [JsonPropertyName("key")]
        public string Key { get; set; } = string.Empty;

        [JsonPropertyName("title")]
        public string Title { get; set; } = string.Empty;

        [JsonPropertyName("last_update")]
        public DateTime LastUpdate { get; set; }

        [JsonPropertyName("markets")]
        public List<Market> Markets { get; set; } = new();
    }

    public class Market
    {
        [JsonPropertyName("key")]
        public string Key { get; set; } = string.Empty;

        [JsonPropertyName("last_update")]
        public DateTime LastUpdate { get; set; }

        [JsonPropertyName("outcomes")]
        public List<Outcome> Outcomes { get; set; } = new();
    }

    public class Outcome
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("price")]
        public decimal Price { get; set; }
    }
}
