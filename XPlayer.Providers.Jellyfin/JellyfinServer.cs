using XPlayer.Core.Models;
using XPlayer.Core.Network;

namespace XPlayer.Providers.Jellyfin
{
    public class JellyfinServer : IMediaServer
    {
        private readonly HttpClient _httpClient;

        public JellyfinServer(string baseUrl, HttpClient httpClient, PublicSystemInfo info, IClientProfile clientProfile)
        {
            BaseUrl = baseUrl;
            _httpClient = httpClient;
            Name = info.ServerName ?? "Jellyfin Server";
            Version = info.Version ?? "Unknown";
            
            Authentication = new JellyfinAuthenticationService(_httpClient, clientProfile);
            Library = new JellyfinLibraryService(_httpClient, Authentication);
        }

        public string Name { get; }
        public string Version { get; }
        public string BaseUrl { get; }
        public bool IsConnected => true; // Simplified for now

        public IAuthenticationService Authentication { get; }
        public ILibraryService Library { get; }

        public Task DisconnectAsync(CancellationToken cancellationToken = default)
        {
            _httpClient.Dispose();
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            _httpClient.Dispose();
        }
    }
}
