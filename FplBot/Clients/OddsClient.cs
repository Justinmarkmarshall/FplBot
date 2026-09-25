using FplBot.Configuration;
using FplBot.Model;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace FplBot.Clients
{
    public class OddsClient(IOptions<OddsClientConfig> options, IHttpClientFactory factory) : IOddsClient
    {
        //private string apiKey;

        //public OddsClient()
        //{ 
        //    apiKey = config["OddsAPI:APIToken"];
        //}

        private readonly OddsClientConfig config = options.Value;

        public Task<List<MatchOddsResponse>> GetFixtureOdds(CancellationToken cancellationToken) =>
            GetAnalysisOdds<List<MatchOddsResponse>>("?", "h2h,totals", cancellationToken);

        public Task<MatchOddsResponse> GetBothTeamsToScoreOdds(string eventId, CancellationToken cancellationToken)
        {
            // Resolve alongside /odds, supporting configured URLs with or without a trailing slash.
            var root = new Uri(config.BaseUrl.TrimEnd('/'));
            var eventUrl = new Uri(root, $"events/{Uri.EscapeDataString(eventId)}/odds");
            return GetAnalysisOdds<MatchOddsResponse>(eventUrl.AbsoluteUri + "?", "btts", cancellationToken);
        }

        private async Task<T> GetAnalysisOdds<T>(string path, string markets, CancellationToken cancellationToken)
        {
            try
            {
                var http = factory.CreateClient("OddsClient");
                using var response = await http.GetAsync(
                    $"{path}apiKey={Uri.EscapeDataString(config.ApiToken)}&regions=uk&oddsFormat=decimal&markets={markets}",
                    cancellationToken);
                if (!response.IsSuccessStatusCode) throw new ProviderUnavailableException("OddsAPI");
                var data = await response.Content.ReadFromJsonAsync<T>(cancellationToken);
                return data ?? throw new ProviderUnavailableException("OddsAPI");
            }
            catch (Exception ex) when (ex is HttpRequestException or JsonException
                || (ex is OperationCanceledException && !cancellationToken.IsCancellationRequested))
            {
                throw new ProviderUnavailableException("OddsAPI");
            }
        }

        public async Task<List<MatchOddsDto>> GetOddsForMatches(List<MatchOddsDto> matches)
        {
            try
            {
                // this odds client only seems to get me data for the next two weeks.
                #region live

                var url = $"?apiKey={config.ApiToken}&regions=uk&oddsFormat=american";
                var http = factory.CreateClient("OddsClient");
                var oddsResponse = new List<MatchOddsResponse>();

                var response = await http.GetAsync(url);
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    oddsResponse = JsonSerializer.Deserialize<List<MatchOddsResponse>>(json, JsonSerializerConfig.options);
                }

                #endregion live

                #region test

                //read in data from Json file
                //var json = File.ReadAllText("MockData/OddsForMatches.json");
                //var oddsResponse = JsonSerializer.Deserialize<List<MatchOdds>>(json, Utilities.options);

                #endregion test

                foreach (var match in matches)
                {
                    var odds = oddsResponse.FirstOrDefault(o => match.HomeTeam.Contains(o.HomeTeam) && match.AwayTeam.Contains(o.AwayTeam));
                    if (odds != null)
                    {
                        match.AwayPrice = (int)odds.Bookmakers.First().Markets.First().Outcomes.Where(r => match.AwayTeam.Contains(r.Name)).First().Price;
                        match.HomePrice = (int)odds.Bookmakers.First().Markets.First().Outcomes.Where(r => match.HomeTeam.Contains(r.Name)).First().Price;
                    }
                }

                return matches;
            }
            catch (Exception ex)
            {
                return new List<MatchOddsDto>();
            }
        }
    }

    public interface IOddsClient
    {
        Task<List<MatchOddsResponse>> GetFixtureOdds(CancellationToken cancellationToken);
        Task<MatchOddsResponse> GetBothTeamsToScoreOdds(string eventId, CancellationToken cancellationToken);
        public Task<List<MatchOddsDto>> GetOddsForMatches(List<MatchOddsDto> matches);
    }
}
