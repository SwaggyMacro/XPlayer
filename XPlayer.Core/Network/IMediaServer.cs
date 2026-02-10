namespace XPlayer.Core.Network
{
    /// <summary>
    /// Represents a connected media server instance.
    /// </summary>
    public interface IMediaServer : IDisposable
    {
        string Name { get; }
        string Version { get; }
        string BaseUrl { get; }
        bool IsConnected { get; }

        IAuthenticationService Authentication { get; }
        ILibraryService Library { get; }
        // IPlaybackService Playback { get; }
        
        /// <summary>
        /// Disconnects from the server.
        /// </summary>
        Task DisconnectAsync(CancellationToken cancellationToken = default);
    }
}
