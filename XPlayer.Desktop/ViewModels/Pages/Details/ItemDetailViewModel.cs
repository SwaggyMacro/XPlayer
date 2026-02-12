using System;
using System.Collections.ObjectModel;
using System.Reactive;
using System.Threading.Tasks;
using ReactiveUI;
using XPlayer.Core.Models;
using XPlayer.Core.Network;
using XPlayer.Desktop.Services;
using XPlayer.Desktop.Services.Media;
using XPlayer.Desktop.ViewModels;

namespace XPlayer.Desktop.ViewModels.Pages.Details;

/// <summary>
/// L3: Item detail page for movies and series.
/// Shows backdrop, metadata, cast, and seasons (for series).
/// </summary>
public class ItemDetailViewModel : ViewModelBase
{
    private readonly MediaServerService _mediaServerService;
    private readonly ContentNavigationService _contentNav;
    private IMediaItem _item;
    private bool _isLoading;

    public ItemDetailViewModel(
        MediaServerService mediaServerService,
        ContentNavigationService contentNav,
        IMediaItem item)
    {
        _mediaServerService = mediaServerService;
        _contentNav = contentNav;
        _item = item;

        OpenSeasonCommand = ReactiveCommand.Create<IMediaItem>(OpenSeason);
        OpenPersonCommand = ReactiveCommand.Create<IPerson>(OpenPerson);
        OpenGenreCommand = ReactiveCommand.Create<string>(OpenGenre);
        PlayCommand = ReactiveCommand.Create(Play);
    }

    public IMediaItem Item
    {
        get => _item;
        private set => this.RaiseAndSetIfChanged(ref _item, value);
    }

    /// <summary>
    /// Seasons for series items.
    /// </summary>
    public ObservableCollection<IMediaItem> Seasons { get; } = new();

    public bool IsSeries => Item.Type == "Series";
    public bool IsMovie => Item.Type == "Movie";

    public bool IsLoading
    {
        get => _isLoading;
        private set => this.RaiseAndSetIfChanged(ref _isLoading, value);
    }

    public ReactiveCommand<IMediaItem, Unit> OpenSeasonCommand { get; }
    public ReactiveCommand<IPerson, Unit> OpenPersonCommand { get; }
    public ReactiveCommand<string, Unit> OpenGenreCommand { get; }
    public ReactiveCommand<Unit, Unit> PlayCommand { get; }

    public async Task LoadDataAsync()
    {
        if (_mediaServerService.CurrentServer == null) return;

        IsLoading = true;
        try
        {
            // Reload full item details
            var fullItem = await _mediaServerService.CurrentServer.Library
                .GetItemAsync(Item.Id);
            if (fullItem != null)
                Item = fullItem;

            // For series: load seasons
            if (IsSeries)
            {
                Seasons.Clear();
                var seasons = await _mediaServerService.CurrentServer.Library
                    .GetItemsAsync(Item.Id, 0, 100);
                foreach (var season in seasons)
                {
                    if (season.Type == "Season")
                        Seasons.Add(season);
                }
            }

            this.RaisePropertyChanged(nameof(IsSeries));
            this.RaisePropertyChanged(nameof(IsMovie));
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading item details: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void OpenSeason(IMediaItem season)
    {
        var vm = new SeasonDetailViewModel(_mediaServerService, _contentNav, season);
        _contentNav.NavigateTo(vm, season.Name);
        _ = vm.LoadDataAsync();
    }

    private void OpenPerson(IPerson person)
    {
        var vm = new PersonDetailViewModel(_mediaServerService, _contentNav, person);
        _contentNav.NavigateTo(vm, person.Name);
        _ = vm.LoadDataAsync();
    }

    private void OpenGenre(string genre)
    {
        var vm = new GenreDetailViewModel(_mediaServerService, _contentNav, genre);
        _contentNav.NavigateTo(vm, genre);
        _ = vm.LoadDataAsync();
    }

    private void Play()
    {
        // TODO: Trigger playback
    }
}
