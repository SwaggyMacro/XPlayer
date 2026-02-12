using XPlayer.Core.Models;

namespace XPlayer.Core.Network
{
    public interface ILibraryService
    {
        Task<IEnumerable<ILibrary>> GetLibrariesAsync(CancellationToken cancellationToken = default);
        Task<IEnumerable<IMediaItem>> GetItemsAsync(string parentId, int skip = 0, int limit = 100, CancellationToken cancellationToken = default);
        Task<IMediaItem?> GetItemAsync(string id, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Gets items the user has partially watched (for "Continue Watching").
        /// </summary>
        Task<IEnumerable<IMediaItem>> GetResumeItemsAsync(int limit = 12, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Gets the latest added items, optionally filtered by library.
        /// </summary>
        Task<IEnumerable<IMediaItem>> GetLatestItemsAsync(string? parentId = null, int limit = 16, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Gets the next episodes to watch for in-progress series.
        /// </summary>
        Task<IEnumerable<IMediaItem>> GetNextUpAsync(int limit = 12, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Searches for items matching the query.
        /// </summary>
        Task<IEnumerable<IMediaItem>> SearchAsync(string query, int limit = 50, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Gets items associated with a specific person (actor, director, etc.).
        /// </summary>
        Task<IEnumerable<IMediaItem>> GetPersonItemsAsync(string personName, int limit = 50, CancellationToken cancellationToken = default);
    }

    public interface ILibrary
    {
        string Id { get; }
        string Name { get; }
        string Type { get; } // "movies", "tvshows", etc.
    }
}
