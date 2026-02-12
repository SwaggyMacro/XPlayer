using System;
using System.Linq;
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
/// Genre detail page showing all items of a specific genre.
/// </summary>
public class GenreDetailViewModel : ViewModelBase
{
    private readonly MediaServerService _mediaServerService;
    private readonly ContentNavigationService _contentNav;
    private bool _isLoading;

    public GenreDetailViewModel(
        MediaServerService mediaServerService,
        ContentNavigationService contentNav,
        string genre)
    {
        _mediaServerService = mediaServerService;
        _contentNav = contentNav;
        Genre = genre;

        OpenItemCommand = ReactiveCommand.Create<IMediaItem>(OpenItem);
    }

    public string Genre { get; }
    public ObservableCollection<IMediaItem> Items { get; } = new();

    public bool IsLoading
    {
        get => _isLoading;
        private set => this.RaiseAndSetIfChanged(ref _isLoading, value);
    }

    public ReactiveCommand<IMediaItem, Unit> OpenItemCommand { get; }

    public async Task LoadDataAsync()
    {
        if (_mediaServerService.CurrentServer == null) return;

        IsLoading = true;
        try
        {
            Items.Clear();
            // Use search as a workaround; ideally we'd have a genre-filter API
            var items = await _mediaServerService.CurrentServer.Library
                .SearchAsync(Genre, 100);
            foreach (var item in items)
            {
                if (item.Genres.Contains(Genre))
                    Items.Add(item);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading genre items: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void OpenItem(IMediaItem item)
    {
        var vm = new ItemDetailViewModel(_mediaServerService, _contentNav, item);
        _contentNav.NavigateTo(vm, item.Name);
        _ = vm.LoadDataAsync();
    }
}
