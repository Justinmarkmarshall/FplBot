using FplBot.Clients;
using FplBot.Model;
using FplBot.Services;
using Microsoft.AspNetCore.Mvc;

namespace FplBot.Controllers
{

    [ApiController]
    [Route("[controller]")]
    public class PremController(IFootballDataClient footballDataClient, IOddsClient oddsClient, IFplService fplService) : ControllerBase
    {

        [HttpGet("WinningTeams")]
        public async Task<ActionResult<List<WinningTeamDto>>> GetMostWinningTeamsInTheNext2Weeks()
        {
            var result = await footballDataClient.GetMatchesInTheNext2Weeks();

            var listOfMatches = new List<MatchOddsDto>();

            foreach (var match in result.DistinctBy(m => (m.HomeTeam.Id, m.AwayTeam.Id)))
            {
                var matchDto = new MatchOddsDto()
                {
                    AwayTeam = match.AwayTeam.Name,
                    HomeTeam = match.HomeTeam.Name,
                    MatchDay = match.UtcDate,
                    HomeTeamId = match.HomeTeam.Id,
                    AwayTeamId = match.AwayTeam.Id
                };

                listOfMatches.Add(matchDto);
            }

            await oddsClient.GetOddsForMatches(listOfMatches);

            var winningTeams = fplService.FindWinningTeams(listOfMatches);

            return Ok(winningTeams);
        }

        [HttpPost("Chunk")]
        public async Task<ActionResult<List<PlayerDto>>> CalculateBestPlayersForTheUpcomingDays(int? number)
        {
            if (number is null or <= 0)
                number = 2; // Default to 2 posts if not specified
                            // get all of the matches in the next game days 10 days

            var result = await footballDataClient.GetMatchesInTheNext2Weeks();

            var listOfMatches = new List<MatchOddsDto>();

            foreach (var match in result)
            {
                var matchDto = new MatchOddsDto()
                {
                    AwayTeam = match.AwayTeam.Name,
                    HomeTeam = match.HomeTeam.Name,
                    MatchDay = match.UtcDate,
                    HomeTeamId = match.HomeTeam.Id,
                    AwayTeamId = match.AwayTeam.Id
                };

                listOfMatches.Add(matchDto);
            }

            await oddsClient.GetOddsForMatches(listOfMatches);

            var winningTeams = fplService.WinningTeams(listOfMatches);



            var goalscorers = await footballDataClient.GetPlayersOnWinningTeams(winningTeams);

            foreach (var goalScorer in goalscorers)
            {
                var match = listOfMatches.Where(r => (r.HomeTeam == goalScorer.Club) || (r.AwayTeam == goalScorer.Club)).FirstOrDefault();
                goalScorer.PlayingAgainst = $"{match.HomeTeam}(H) vs {match.AwayTeam}(A)";
            }

            return Ok(goalscorers.DistinctBy(p => new { p.Name, p.Club }).ToList());
        }
    }
}

