using System.Text.Json;

namespace FplBot.Configuration
{
    public class JsonSerializerConfig
    {
        public static JsonSerializerOptions options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }
}
