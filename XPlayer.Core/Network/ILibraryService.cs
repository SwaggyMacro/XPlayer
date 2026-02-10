using XPlayer.Core.Models;

namespace XPlayer.Core.Network
{
    public interface ILibraryService
    {
        Task<IEnumerable<ILibrary>> GetLibrariesAsync(CancellationToken cancellationToken = default);
        Task<IEnumerable<IMediaItem>> GetItemsAsync(string parentId, int skip = 0, int limit = 100, CancellationToken cancellationToken = default);
        Task<IMediaItem?> GetItemAsync(string id, CancellationToken cancellationToken = default);
    }

    public interface ILibrary
    {
        string Id { get; }
        string Name { get; }
        string Type { get; } // "movies", "tvshows", etc.
    }
}
