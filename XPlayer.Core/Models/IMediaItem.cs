namespace XPlayer.Core.Models;

public interface IMediaItem
{
    string Id { get; }
    string Name { get; }
    string Overview { get; }
    string? ImageUrl { get; }
    string? BackdropImageUrl { get; }
    string Type { get; } // "Movie", "Series", "Season", "Episode"

    // Metadata
    int? ProductionYear { get; }
    float? CommunityRating { get; }  // e.g. TMDb rating
    float? CriticRating { get; }     // e.g. Rotten Tomatoes
    string? OfficialRating { get; }  // e.g. "PG-13", "R"
    IReadOnlyList<string> Genres { get; }
    IReadOnlyList<string> Tags { get; }
    IReadOnlyList<string> Studios { get; }
    IReadOnlyList<IPerson> People { get; }
    DateTime? PremiereDate { get; }
    DateTime? DateCreated { get; }
    
    /// <summary>
    /// Percentage of the item that has been played (0-100). Null if not tracked.
    /// Used for "Continue Watching" progress bars.
    /// </summary>
    double? PlayedPercentage { get; }
}

/// <summary>
/// 人物信息 (演员、导演等)
/// </summary>
public interface IPerson
{
    string Name { get; }
    string? Role { get; }      // e.g. "Tony Stark" (角色名)
    string? Type { get; }      // "Actor", "Director", "Writer", etc.
    string? ImageUrl { get; }
}