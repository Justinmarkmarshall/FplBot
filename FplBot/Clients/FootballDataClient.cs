using FplBot.Model;
using System.Text.Json;

namespace FplBot.Clients
{
    public class FootballDataClient(IHttpClientFactory factory) : IFootballDataClient
    {
        public async Task<List<Model.Match>> GetMatchesInTheNext2Weeks()
        {
            List<Model.Match> matches = new List<Model.Match>();
            var http = factory.CreateClient("FootballDataClient");

            var from = DateTime.Today.Date;
            var to = from.AddDays(14);

            var url = $"competitions/PL/matches?dateFrom={from:yyyy-MM-dd}&dateTo={to:yyyy-MM-dd}";
            var response = await http.GetAsync(url);

            if (response.IsSuccessStatusCode)
            {
                //deserialize into list of match
                var content = await response.Content.ReadAsStringAsync();
                var matchesResponse = JsonSerializer.Deserialize<MatchesResponse>(content);

                foreach (var match in matchesResponse!.Matches)
                {
                    matches.Add(match);
                }
            }
            else
            {
                Console.WriteLine($"Error fetching data from {url}: {response.ReasonPhrase}");
                return new List<Model.Match>();
            }

            return matches.OrderBy(m => m.UtcDate).ToList();
        }

        public async Task<List<PlayerDto>> GetPlayersOnWinningTeams(List<Tuple<string, int>> winningTeams)
        {
            var players = new List<PlayerDto>();

            #region live
            var url = "competitions/PL/teams?season=2025";
            var http = factory.CreateClient("FootballDataClient");
            var response = await http.GetAsync(url);
            var content = await response.Content.ReadAsStringAsync();
            var teamsResponse = JsonSerializer.Deserialize<PremierLeagueTeamsResponse>(content);

            #endregion live

            #region test

            // read in data from Json file
            //var json = File.ReadAllText("MockData/PlayersInTeams.json");
            //var teamsResponse = JsonSerializer.Deserialize<PremierLeagueTeamsResponse>(json, Utilities.options);

            #endregion test

            foreach (var team in winningTeams)
            {
                var teamPlayers = teamsResponse!.Teams.FirstOrDefault(r => r.Name == team.Item1)?.Squad.Where(p => p.Position != null &&
                (p.Position.Contains("attacking", StringComparison.OrdinalIgnoreCase) ||
                    p.Position.Contains("forward", StringComparison.OrdinalIgnoreCase) || p.Position.Contains("winger", StringComparison.OrdinalIgnoreCase))).ToList().Take(10);

                if (teamPlayers == null)
                    continue;

                //var url = $"fixtures?team={team.Item2}&last5";
                foreach (var player in teamPlayers!)
                {
                    var play = new PlayerDto();

                    play.Name = player.Name;
                    play.Club = team.Item1;
                    play.Position = player.Position;

                    players.Add(play);
                }

                var defenders = teamsResponse!.Teams.FirstOrDefault(r => r.Name == team.Item1)?.Squad.Where(p => p.Position != null &&
        (p.Position.Contains("back", StringComparison.OrdinalIgnoreCase) ||
         p.Position.Contains("forward", StringComparison.OrdinalIgnoreCase) || p.Position.Contains("winger", StringComparison.OrdinalIgnoreCase))).ToList().Take(10);

                if (defenders == null)
                    continue;

                foreach (var player in defenders)
                {
                    var play = new PlayerDto();
                    play.Name = player.Name;
                    play.Club = team.Item1;
                    play.Position = player.Position;
                    players.Add(play);
                }

                var goalkeepers = teamsResponse!.Teams.FirstOrDefault(r => r.Id == team.Item2)?.Squad.Where(p => p.Position != null &&
                p.Position.Contains("goalkeeper", StringComparison.OrdinalIgnoreCase));

                if (goalkeepers == null)
                    continue;

                foreach (var player in goalkeepers)
                {
                    var play = new PlayerDto();
                    play.Name = player.Name;
                    play.Club = team.Item1;
                    play.Position = player.Position;
                    players.Add(play);
                }
            }

            return players;
        }
    }

    public interface IFootballDataClient
    {
        /// <summary>
        /// Default to two weeks - odds won't go further than that
        /// </summary>
        /// <returns></returns>
        Task<List<Model.Match>> GetMatchesInTheNext2Weeks();
        Task<List<PlayerDto>> GetPlayersOnWinningTeams(List<Tuple<string, int>> winningTeams);
    }
}
