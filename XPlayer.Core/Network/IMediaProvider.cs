namespace XPlayer.Core.Network
{
    /// <summary>
    /// Represents a media provider (e.g. Jellyfin, Emby, Plex).
    /// </summary>
    public interface IMediaProvider
    {
        string Name { get; }
        
        /// <summary>
        /// Attempts to connect to a media server at the given URL.
        /// </summary>
        /// <param name="url">The server URL.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A connected media server instance, or null if connection failed.</returns>
        Task<IMediaServer?> ConnectAsync(string url, CancellationToken cancellationToken = default);

        /// <summary>
        /// Discovers available servers on the local network.
        /// </summary>
        Task<IEnumerable<ServerDiscoveryResult>> DiscoverServersAsync(CancellationToken cancellationToken = default);
    }

    public record ServerDiscoveryResult(string Name, string Url, string Version);
}
