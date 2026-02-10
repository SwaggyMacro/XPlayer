using XPlayer.Desktop.Models.Configuration;

namespace XPlayer.Desktop.Services.Abstractions;

public interface IConfigurationService
{
    General? General { get; }
    MediaSources? MediaSources { get; }
    void SaveMediaSources();
}