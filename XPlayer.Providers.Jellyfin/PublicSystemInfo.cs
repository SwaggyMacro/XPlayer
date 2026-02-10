using System.Text.Json.Serialization;

namespace XPlayer.Providers.Jellyfin
{
    public class PublicSystemInfo
    {
        [JsonPropertyName("ServerName")]
        public string? ServerName { get; set; }

        [JsonPropertyName("Version")]
        public string? Version { get; set; }

        [JsonPropertyName("Id")]
        public string? Id { get; set; }
    }
}
