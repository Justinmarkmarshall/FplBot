namespace FplBot.Model
{
    public class MatchOddsDto
    {
        public string HomeTeam { get; set; }

        public string AwayTeam { get; set; }

        public DateTime MatchDay;

        public int HomePrice { get; set; } = 0;

        public int AwayPrice { get; set; } = 0;

        public int HomeTeamId { get; set; } = 0;

        public int AwayTeamId { get; set; } = 0;
    }
}
