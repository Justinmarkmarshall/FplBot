namespace FplBot.Model
{
    /// <summary>
    /// Return to the UI a Winning Team with their upcoming matches and the probability of winning
    /// </summary>
    public class WinningTeamDto
    {
        public string TeamName { get; set; }

        public List<MatchDto> WinningMatches { get; set; } = new List<MatchDto>();
    }
}
