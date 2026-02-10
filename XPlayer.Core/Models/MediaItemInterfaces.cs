namespace XPlayer.Core.Models;

public interface IVideo : IMediaItem
{
    TimeSpan Duration { get; }
    string? StreamUrl { get; }
}

public interface ISeries : IMediaItem
{
    int SeasonCount { get; }
    int EpisodeCount { get; }
    string? Status { get; }
}

public interface ISeason : IMediaItem
{
    int Index { get; }
    string SeriesId { get; }
}

public interface IEpisode : IVideo
{
    int Index { get; }
    string SeasonId { get; }
    string SeriesId { get; }
}