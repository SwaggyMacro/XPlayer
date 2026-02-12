using System;
using System.Collections.ObjectModel;
using System.Reactive;
using System.Threading.Tasks;
using ReactiveUI;
using XPlayer.Core.Models;
using XPlayer.Desktop.Services;
using XPlayer.Desktop.Services.Media;
using XPlayer.Desktop.ViewModels;

namespace XPlayer.Desktop.ViewModels.Pages.Details;

/// <summary>
/// L4: Season detail page showing episodes of a season.
/// </summary>
public class SeasonDetailViewModel : ViewModelBase
{
    private readonly MediaServerService _mediaServerService;
    private readonly ContentNavigationService _contentNav;
    private bool _isLoading;

    public SeasonDetailViewModel(
        MediaServerService mediaServerService,
        ContentNavigationService contentNav,
        IMediaItem season)
    {
        _mediaServerService = mediaServerService;
        _contentNav = contentNav;
        Season = season;

        OpenEpisodeCommand = ReactiveCommand.Create<IMediaItem>(OpenEpisode);
        PlayEpisodeCommand = ReactiveCommand.Create<IMediaItem>(PlayEpisode);
    }

    public IMediaItem Season { get; }
    public ObservableCollection<IMediaItem> Episodes { get; } = new();

    public bool IsLoading
    {
        get => _isLoading;
        private set => this.RaiseAndSetIfChanged(ref _isLoading, value);
    }

    public ReactiveCommand<IMediaItem, Unit> OpenEpisodeCommand { get; }
    public ReactiveCommand<IMediaItem, Unit> PlayEpisodeCommand { get; }

    public async Task LoadDataAsync()
    {
        if (_mediaServerService.CurrentServer == null) return;

        IsLoading = true;
        try
        {
            Episodes.Clear();
            var episodes = await _mediaServerService.CurrentServer.Library
                .GetItemsAsync(Season.Id, 0, 200);
            foreach (var ep in episodes)
            {
                if (ep.Type == "Episode")
                    Episodes.Add(ep);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading episodes: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void OpenEpisode(IMediaItem episode)
    {
        var vm = new EpisodeDetailViewModel(_mediaServerService, _contentNav, episode);
        _contentNav.NavigateTo(vm, episode.Name);
    }

    private void PlayEpisode(IMediaItem episode)
    {
        // TODO: Trigger playback
    }
}
