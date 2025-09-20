using FplBot.Model;
using FplBot.Utilities;
using System.Text.RegularExpressions;

namespace FplBot.Services
{
    public class FplService : IFplService
    {
        public List<Tuple<string, int>> WinningTeams(List<MatchOddsDto> matches)
        {
            var winningTeams = new List<Tuple<string, int>>();
            foreach (var match in matches)
            {
                if (match.HomePrice < match.AwayPrice)
                {
                    winningTeams.Add(new Tuple<string, int>(match.HomeTeam, match.HomePrice));
                }
                else if (match.AwayPrice < match.HomePrice)
                {
                    winningTeams.Add(new Tuple<string, int>(match.AwayTeam, match.AwayPrice));
                }
            }
            return winningTeams;
        }

        /// <summary>
        /// Returns a list of winning teams with their winning matches and win probabilities
        /// </summary>
        /// <param name="matches"></param>
        /// <returns></returns>
        public List<WinningTeamDto> FindWinningTeams(List<MatchOddsDto> matches)
        {
            var winningTeams = new List<WinningTeamDto>();
            foreach (var match in matches)
            {
                if (match.HomePrice < match.AwayPrice)
                {
                    var winningMatch = new MatchDto()
                    {
                        MatchDay = match.MatchDay,
                        WinProbability = match.HomePrice.CalculateWinPercentage(),
                        Opponent = match.AwayTeam
                    };

                    if (winningTeams.Where(r => r.TeamName == match.HomeTeam).Any())
                    {
                        // get winning team from the list
                        var winningTeamToUpdate = winningTeams.Where(r => r.TeamName.ToLower() == match.HomeTeam.ToLower()).FirstOrDefault();
                        winningTeamToUpdate!.WinningMatches.Add(winningMatch);
                    }
                    else
                    {
                        var weHaveAWinner = new WinningTeamDto()
                        {
                            TeamName = match.HomeTeam,
                            WinningMatches = new List<MatchDto>() { winningMatch }
                        };
                        winningTeams.Add(weHaveAWinner);
                    }
                }
                else if (match.AwayPrice < match.HomePrice)
                {
                    var winningMatch = new MatchDto()
                    {
                        MatchDay = match.MatchDay,
                        WinProbability = match.AwayPrice.CalculateWinPercentage(),
                        Opponent = match.HomeTeam
                    };

                    if (winningTeams.Where(r => r.TeamName == match.AwayTeam).Any())
                    {
                        // get winning team from the list
                        var winningTeamToUpdate = winningTeams.Where(r => r.TeamName.ToLower() == match.AwayTeam.ToLower()).FirstOrDefault();
                        winningTeamToUpdate!.WinningMatches.Add(winningMatch);
                    }
                    else
                    {
                        var weHaveAWinner = new WinningTeamDto()
                        {
                            TeamName = match.AwayTeam,
                            WinningMatches = new List<MatchDto>() { winningMatch }
                        };

                        winningTeams.Add(weHaveAWinner);
                    }
                }
            }
            return winningTeams;
        }
    }

    public interface IFplService
    {
        List<Tuple<string, int>> WinningTeams(List<MatchOddsDto> matches);

        List<WinningTeamDto> FindWinningTeams(List<MatchOddsDto> matches);
    }
}
