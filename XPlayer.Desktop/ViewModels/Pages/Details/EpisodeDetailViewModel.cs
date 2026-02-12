using System.Reactive;
using ReactiveUI;
using XPlayer.Core.Models;
using XPlayer.Desktop.Services;
using XPlayer.Desktop.Services.Media;

namespace XPlayer.Desktop.ViewModels.Pages.Details;

/// <summary>
/// L5: Episode detail page with metadata, subtitles, audio track info, and play button.
/// </summary>
public class EpisodeDetailViewModel : ViewModelBase
{
    private readonly MediaServerService _mediaServerService;
    private readonly ContentNavigationService _contentNav;

    public EpisodeDetailViewModel(
        MediaServerService mediaServerService,
        ContentNavigationService contentNav,
        IMediaItem episode)
    {
        _mediaServerService = mediaServerService;
        _contentNav = contentNav;
        Episode = episode;

        PlayCommand = ReactiveCommand.Create(Play);
    }

    public IMediaItem Episode { get; }

    public ReactiveCommand<Unit, Unit> PlayCommand { get; }

    private void Play()
    {
        // TODO: Trigger playback
    }
}
