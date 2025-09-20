using FplBot.Model;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace FplBot.Clients
{
    public class FootballDataClient(IHttpClientFactory factory) : IFootballDataClient
    {
        /// <summary>
        /// Hardcoded list of predefined matchday dates
        /// </summary>
        private Dictionary<DateTime, int> matchdayDates = new Dictionary<DateTime, int>
{
    // MD1 (Fri–Mon)
    { new DateTime(2025, 8, 15), 1 },
    { new DateTime(2025, 8, 16), 1 },
    { new DateTime(2025, 8, 17), 1 },
    { new DateTime(2025, 8, 18), 1 },

    // MD2 (Fri–Mon)
    { new DateTime(2025, 8, 22), 2 },
    { new DateTime(2025, 8, 23), 2 },
    { new DateTime(2025, 8, 24), 2 },
    { new DateTime(2025, 8, 25), 2 },

    // MD3 (Sat–Sun)
    { new DateTime(2025, 8, 30), 3 },
    { new DateTime(2025, 8, 31), 3 },

    // MD4 (Sat–Sun)
    { new DateTime(2025, 9, 13), 4 },
    { new DateTime(2025, 9, 14), 4 },

    // MD5 (Sat–Sun, +Mon window)
    { new DateTime(2025, 9, 20), 5 },
    { new DateTime(2025, 9, 21), 5 },
    { new DateTime(2025, 9, 22), 5 }, // Monday slot if used

    // MD6 (Sat–Sun)
    { new DateTime(2025, 9, 27), 6 },
    { new DateTime(2025, 9, 28), 6 },

    // MD7 (Sat–Sun)
    { new DateTime(2025, 10, 4), 7 },
    { new DateTime(2025, 10, 5), 7 },

    // MD8 (Sat–Sun)
    { new DateTime(2025, 10, 18), 8 },
    { new DateTime(2025, 10, 19), 8 },

    // MD9 (Sat–Sun)
    { new DateTime(2025, 10, 25), 9 },
    { new DateTime(2025, 10, 26), 9 },

    // MD10 (Sat–Sun)
    { new DateTime(2025, 11, 1), 10 },
    { new DateTime(2025, 11, 2), 10 },

    // MD11 (Sat–Sun)
    { new DateTime(2025, 11, 8), 11 },
    { new DateTime(2025, 11, 9), 11 },

    // MD12 (Sat–Sun)
    { new DateTime(2025, 11, 22), 12 },
    { new DateTime(2025, 11, 23), 12 },

    // MD13 (Sat–Sun)
    { new DateTime(2025, 11, 29), 13 },
    { new DateTime(2025, 11, 30), 13 },

    // MD14 (MIDWEEK)
    { new DateTime(2025, 12, 3), 14 },

    // MD15 (Sat–Sun)
    { new DateTime(2025, 12, 6), 15 },
    { new DateTime(2025, 12, 7), 15 },

    // MD16 (Sat–Sun)
    { new DateTime(2025, 12, 13), 16 },
    { new DateTime(2025, 12, 14), 16 },

    // MD17 (Sat–Sun)
    { new DateTime(2025, 12, 20), 17 },
    { new DateTime(2025, 12, 21), 17 },

    // MD18 (Sat–Sun)
    { new DateTime(2025, 12, 27), 18 },
    { new DateTime(2025, 12, 28), 18 },

    // MD19 (MIDWEEK – Festive)
    { new DateTime(2025, 12, 30), 19 },

    // MD20 (Sat–Sun)
    { new DateTime(2026, 1, 3), 20 },
    { new DateTime(2026, 1, 4), 20 },

    // MD21 (MIDWEEK)
    { new DateTime(2026, 1, 7), 21 },

    // MD22 (Sat–Sun)
    { new DateTime(2026, 1, 17), 22 },
    { new DateTime(2026, 1, 18), 22 },

    // MD23 (Sat–Sun)
    { new DateTime(2026, 1, 24), 23 },
    { new DateTime(2026, 1, 25), 23 },

    // MD24 (Sat–Sun)
    { new DateTime(2026, 1, 31), 24 },
    { new DateTime(2026, 2, 1), 24 },

    // MD25 (Sat–Sun)
    { new DateTime(2026, 2, 7), 25 },
    { new DateTime(2026, 2, 8), 25 },

    // MD26 (MIDWEEK)
    { new DateTime(2026, 2, 11), 26 },

    // MD27 (Sat–Sun)
    { new DateTime(2026, 2, 21), 27 },
    { new DateTime(2026, 2, 22), 27 },

    // MD28 (Sat–Sun)
    { new DateTime(2026, 2, 28), 28 },
    { new DateTime(2026, 3, 1), 28 },

    // MD29 (MIDWEEK)
    { new DateTime(2026, 3, 4), 29 },

    // MD30 (Sat–Sun)
    { new DateTime(2026, 3, 14), 30 },
    { new DateTime(2026, 3, 15), 30 },

    // MD31 (Sat–Sun)
    { new DateTime(2026, 3, 21), 31 },
    { new DateTime(2026, 3, 22), 31 },

    // MD32 (Sat–Sun)
    { new DateTime(2026, 4, 11), 32 },
    { new DateTime(2026, 4, 12), 32 },

    // MD33 (Sat–Sun)
    { new DateTime(2026, 4, 18), 33 },
    { new DateTime(2026, 4, 19), 33 },

    // MD34 (Sat–Sun)
    { new DateTime(2026, 4, 25), 34 },
    { new DateTime(2026, 4, 26), 34 },

    // MD35 (Sat–Sun)
    { new DateTime(2026, 5, 2), 35 },
    { new DateTime(2026, 5, 3), 35 },

    // MD36 (Sat–Sun)
    { new DateTime(2026, 5, 9), 36 },
    { new DateTime(2026, 5, 10), 36 },

    // MD37 (Sun)
    { new DateTime(2026, 5, 17), 37 },

    // MD38 (Sun – Final day)
    { new DateTime(2026, 5, 24), 38 },
};

        public async Task<List<Model.Match>> GetMatchesInTheNext2Weeks()
        {
            var gameDays = GetMatchdaysInNext2Weeks();
            List<Model.Match> matches = new List<Model.Match>();
            var http = factory.CreateClient("FootballDataClient");
            #region test

            //read in data from Json file
            //var json = File.ReadAllText("MockData/AllMatchesInNextWeek.json");

            //var matchesResponse = JsonSerializer.Deserialize<MatchesResponse>(json);
            //foreach (var match in matchesResponse.Matches)
            //{
            //    if (match.UtcDate.Date >= today && match.UtcDate.Date <= nextWeek)
            //    {
            //        thisWeekMatches.Add(match);
            //    }
            //}

            #endregion test

            // this is for live and has a limit
            #region live

            foreach (var day in gameDays)
            {

                var url = $"competitions/PL/matches?matchday={day.Value}";
                var response = await http.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    //deserialize into list of match
                    var content = await response.Content.ReadAsStringAsync();
                    var matchesResponse = JsonSerializer.Deserialize<MatchesResponse>(content);

                    foreach (var match in matchesResponse.Matches)
                    {
                        matches.Add(match);
                    }
                }
                else
                {
                    Console.WriteLine($"Error fetching data from {url}: {response.ReasonPhrase}");
                    return new List<Model.Match>();
                }
                #endregion live
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
                var teamPlayers = teamsResponse.Teams.FirstOrDefault(r => r.Id == team.Item2).Squad.Where(p => p.Position != null &&
                (p.Position.Contains("attacking", StringComparison.OrdinalIgnoreCase) ||
                    p.Position.Contains("forward", StringComparison.OrdinalIgnoreCase) || p.Position.Contains("winger", StringComparison.OrdinalIgnoreCase))).ToList().Take(10);

                //var url = $"fixtures?team={team.Item2}&last5";
                foreach (var player in teamPlayers)
                {
                    var play = new PlayerDto();

                    play.Name = player.Name;
                    play.Club = team.Item1;
                    play.Position = player.Position;

                    players.Add(play);
                }

                var defenders = teamsResponse.Teams.FirstOrDefault(r => r.Id == team.Item2).Squad.Where(p => p.Position != null &&
        (p.Position.Contains("back", StringComparison.OrdinalIgnoreCase) ||
         p.Position.Contains("forward", StringComparison.OrdinalIgnoreCase) || p.Position.Contains("winger", StringComparison.OrdinalIgnoreCase))).ToList().Take(10);

                foreach (var player in defenders)
                {
                    var play = new PlayerDto();
                    play.Name = player.Name;
                    play.Club = team.Item1;
                    play.Position = player.Position;
                    players.Add(play);
                }

                var goalkeepers = teamsResponse.Teams.FirstOrDefault(r => r.Id == team.Item2).Squad.Where(p => p.Position != null &&
                p.Position.Contains("goalkeeper", StringComparison.OrdinalIgnoreCase));

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

        public List<KeyValuePair<DateTime, int>> GetMatchdaysInNext2Weeks()
        {
            var today = DateTime.Now.Date;
            var twoWeeksFromToday = today.AddDays(16);

            // Filter matchdayDates for dates within the range [today, fourWeeksFromToday]
            var upcomingMatchdays = matchdayDates
                .Where(kvp => kvp.Key >= today && kvp.Key <= twoWeeksFromToday)
                .OrderBy(kvp => kvp.Key)
                .ToList();

            return upcomingMatchdays;
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
