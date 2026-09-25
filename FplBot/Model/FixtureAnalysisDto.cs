using System.Text.Json.Serialization;

namespace FplBot.Model;

public class FixtureAnalysisDto
{
    public string Team { get; set; } = string.Empty;
    public string Opponent { get; set; } = string.Empty;
    public bool Home { get; set; }
    public DateTime Kickoff { get; set; }
    [JsonPropertyName("winPercentage")]
    public int? WinProbability { get; set; }
    [JsonPropertyName("threeOrMoreGoalsPercentage")]
    public int? Over25Probability { get; set; }
    [JsonPropertyName("bothTeamsToScorePercentage")]
    public int? BothTeamsToScoreProbability { get; set; }
    public int? AttackingPotential { get; set; }
    public int? DefensivePotential { get; set; }
}
