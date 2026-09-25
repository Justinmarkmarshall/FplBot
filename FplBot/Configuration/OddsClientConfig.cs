namespace FplBot.Configuration
{
    public class OddsClientConfig
    {
        public string BaseUrl { get; set; } = string.Empty;
        public string ApiToken { get; set; } = string.Empty;
        public int BttsCacheMinutes { get; set; } = 30;
        public int BttsMaxConcurrency { get; set; } = 4;
    }
}
