using System;
using System.Collections.ObjectModel;
using System.Reactive;
using System.Threading.Tasks;
using ReactiveUI;
using XPlayer.Core.Models;
using XPlayer.Core.Network;
using XPlayer.Desktop.Converters;
using XPlayer.Desktop.Services;
using XPlayer.Desktop.Services.Media;
using XPlayer.Desktop.ViewModels;
using XPlayer.Lang;
using XPlayer.Desktop.ViewModels.Pages.Library;
using XPlayer.Desktop.ViewModels.Pages.Details;
using XPlayer.Desktop.ViewModels.Pages.Search;

namespace XPlayer.Desktop.ViewModels.Pages.Home;

/// <summary>
/// L1 Dashboard: Hero banner, continue watching, next up, latest additions, libraries.
/// </summary>
public class HomeDashboardViewModel : ViewModelBase
{
    private readonly MediaServerService _mediaServerService;
    private readonly ContentNavigationService _contentNav;
    private bool _isLoading;
    private IMediaItem? _heroBannerItem;

    public HomeDashboardViewModel(
        MediaServerService mediaServerService,
        ContentNavigationService contentNav)
    {
        _mediaServerService = mediaServerService;
        _contentNav = contentNav;

        OpenLibraryCommand = ReactiveCommand.Create<ILibrary>(OpenLibrary);
        OpenItemCommand = ReactiveCommand.Create<IMediaItem>(OpenItem);
    }

    public ObservableCollection<IMediaItem> ResumeItems { get; } = new();
    public ObservableCollection<IMediaItem> NextUpItems { get; } = new();
    public ObservableCollection<IMediaItem> LatestItems { get; } = new();
    public ObservableCollection<ILibrary> Libraries { get; } = new();

    public IMediaItem? HeroBannerItem
    {
        get => _heroBannerItem;
        private set => this.RaiseAndSetIfChanged(ref _heroBannerItem, value);
    }

    public bool IsLoading
    {
        get => _isLoading;
        private set => this.RaiseAndSetIfChanged(ref _isLoading, value);
    }

    public bool HasResumeItems => ResumeItems.Count > 0;
    public bool HasNextUpItems => NextUpItems.Count > 0;
    public bool HasLatestItems => LatestItems.Count > 0;
    public bool HasLibraries => Libraries.Count > 0;

    public ReactiveCommand<ILibrary, Unit> OpenLibraryCommand { get; }
    public ReactiveCommand<IMediaItem, Unit> OpenItemCommand { get; }

    public async Task LoadDataAsync()
    {
        if (_mediaServerService.CurrentServer == null) return;

        IsLoading = true;
        try
        {
            var libraryService = _mediaServerService.CurrentServer.Library;

            // Load all data concurrently
            var resumeTask = libraryService.GetResumeItemsAsync();
            var nextUpTask = libraryService.GetNextUpAsync();
            var latestTask = libraryService.GetLatestItemsAsync();
            var librariesTask = libraryService.GetLibrariesAsync();

            await Task.WhenAll(resumeTask, nextUpTask, latestTask, librariesTask);

            // Resume items
            ResumeItems.Clear();
            foreach (var item in await resumeTask)
                ResumeItems.Add(item);

            // Next up
            NextUpItems.Clear();
            foreach (var item in await nextUpTask)
                NextUpItems.Add(item);

            // Latest additions
            LatestItems.Clear();
            foreach (var item in await latestTask)
                LatestItems.Add(item);

            // Libraries
            Libraries.Clear();
            foreach (var lib in await librariesTask)
                Libraries.Add(lib);

            // Hero banner: pick first latest or resume item with backdrop
            HeroBannerItem = null;
            foreach (var item in LatestItems)
            {
                if (!string.IsNullOrEmpty(item.BackdropImageUrl))
                {
                    HeroBannerItem = item;
                    break;
                }
            }

            // Notify has-items changes
            this.RaisePropertyChanged(nameof(HasResumeItems));
            this.RaisePropertyChanged(nameof(HasNextUpItems));
            this.RaisePropertyChanged(nameof(HasLatestItems));
            this.RaisePropertyChanged(nameof(HasLibraries));
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading dashboard data: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void OpenLibrary(ILibrary library)
    {
        var vm = new LibraryDetailViewModel(_mediaServerService, _contentNav, library);
        _contentNav.NavigateTo(vm, library.Name);
        _ = vm.LoadDataAsync();
    }

    private void OpenItem(IMediaItem item)
    {
        var vm = new ItemDetailViewModel(_mediaServerService, _contentNav, item);
        _contentNav.NavigateTo(vm, item.Name);
        _ = vm.LoadDataAsync();
    }
}
