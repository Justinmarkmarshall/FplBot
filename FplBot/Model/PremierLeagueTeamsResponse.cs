using System.Text.Json.Serialization;

namespace FplBot.Model
{
    public class PremierLeagueTeamsResponse
    {
        [JsonPropertyName("count")]
        public int Count { get; set; }

        [JsonPropertyName("filters")]
        public Filters Filters { get; set; } = new();

        [JsonPropertyName("competition")]
        public Competition Competition { get; set; } = new();

        [JsonPropertyName("season")]
        public Season Season { get; set; } = new();

        [JsonPropertyName("teams")]
        public List<SquadTeam> Teams { get; set; } = new();
    }

    public class Filters
    {
        [JsonPropertyName("season")]
        public int Season { get; set; }
    }

    public class SquadTeam
    {
        [JsonPropertyName("area")]
        public Area Area { get; set; } = new();

        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("shortName")]
        public string ShortName { get; set; } = string.Empty;

        [JsonPropertyName("tla")]
        public string Tla { get; set; } = string.Empty;

        [JsonPropertyName("crest")]
        public string Crest { get; set; } = string.Empty;

        [JsonPropertyName("address")]
        public string Address { get; set; } = string.Empty;

        [JsonPropertyName("website")]
        public string Website { get; set; } = string.Empty;

        [JsonPropertyName("founded")]
        public int Founded { get; set; }

        [JsonPropertyName("clubColors")]
        public string ClubColors { get; set; } = string.Empty;

        [JsonPropertyName("venue")]
        public string Venue { get; set; } = string.Empty;

        [JsonPropertyName("runningCompetitions")]
        public List<Competition> RunningCompetitions { get; set; } = new();

        [JsonPropertyName("coach")]
        public Coach Coach { get; set; } = new();

        [JsonPropertyName("squad")]
        public List<Player> Squad { get; set; } = new();

        [JsonPropertyName("staff")]
        public List<object> Staff { get; set; } = new();

        [JsonPropertyName("lastUpdated")]
        public DateTime LastUpdated { get; set; }
    }

    public class Coach
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("firstName")]
        public string FirstName { get; set; } = string.Empty;

        [JsonPropertyName("lastName")]
        public string LastName { get; set; } = string.Empty;

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("dateOfBirth")]
        public DateTime DateOfBirth { get; set; }

        [JsonPropertyName("nationality")]
        public string Nationality { get; set; } = string.Empty;

        [JsonPropertyName("contract")]
        public Contract Contract { get; set; } = new();
    }

    public class Contract
    {
        [JsonPropertyName("start")]
        public string Start { get; set; } = string.Empty;

        [JsonPropertyName("until")]
        public string Until { get; set; } = string.Empty;
    }

    public class Player
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("position")]
        public string Position { get; set; } = string.Empty;

        [JsonPropertyName("nationality")]
        public string Nationality { get; set; } = string.Empty;
    }
}
