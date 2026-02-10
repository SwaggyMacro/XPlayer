using System.Net.Http.Json;
using XPlayer.Core.Models;
using XPlayer.Core.Network;

namespace XPlayer.Providers.Jellyfin
{
    public class JellyfinMediaProvider : IMediaProvider
    {
        private readonly IClientProfile _clientProfile;

        public JellyfinMediaProvider(IClientProfile clientProfile)
        {
            _clientProfile = clientProfile;
        }

        public string Name => "Jellyfin";

        public async Task<IMediaServer?> ConnectAsync(string url, CancellationToken cancellationToken = default)
        {
            // Basic validation
            if (string.IsNullOrWhiteSpace(url)) return null;
            if (!url.StartsWith("http")) url = "http://" + url;

            try 
            {
                var httpClient = new HttpClient { BaseAddress = new Uri(url) };
                
                // Get System Info
                var info = await httpClient.GetFromJsonAsync<PublicSystemInfo>("System/Info/Public", cancellationToken);
                
                if (info == null) return null;

                return new JellyfinServer(url, httpClient, info, _clientProfile);
            }
            catch
            {
                return null;
            }
        }

        public Task<IEnumerable<ServerDiscoveryResult>> DiscoverServersAsync(CancellationToken cancellationToken = default)
        {
            // UDP discovery implementation would go here
            return Task.FromResult<IEnumerable<ServerDiscoveryResult>>(Array.Empty<ServerDiscoveryResult>());
        }
    }
}
