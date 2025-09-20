namespace FplBot.Model
{
    /// <summary>
    /// Return to the UI associated with a team and the probability of winning
    /// </summary>
    public class MatchDto
    {
        public DateTime MatchDay { get; set; }

        public string Opponent { get; set; } = string.Empty;

        public decimal WinProbability { get; set; } = 0;
    }
}
