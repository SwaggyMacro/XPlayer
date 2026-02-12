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

using XPlayer.Desktop.ViewModels.Pages.Details;

namespace XPlayer.Desktop.ViewModels.Pages.Library;

/// <summary>
/// L2: Library detail page showing all items in a library with pagination.
/// </summary>
public class LibraryDetailViewModel : ViewModelBase
{
    private readonly MediaServerService _mediaServerService;
    private readonly ContentNavigationService _contentNav;
    private bool _isLoading;
    private bool _hasMore = true;
    private int _loadedCount;
    private const int PageSize = 50;

    public LibraryDetailViewModel(
        MediaServerService mediaServerService,
        ContentNavigationService contentNav,
        ILibrary library)
    {
        _mediaServerService = mediaServerService;
        _contentNav = contentNav;
        Library = library;

        OpenItemCommand = ReactiveCommand.Create<IMediaItem>(OpenItem);
        LoadMoreCommand = ReactiveCommand.CreateFromTask(LoadMoreAsync);
    }

    public ILibrary Library { get; }
    public ObservableCollection<IMediaItem> Items { get; } = new();

    public bool IsLoading
    {
        get => _isLoading;
        private set => this.RaiseAndSetIfChanged(ref _isLoading, value);
    }

    public bool HasMore
    {
        get => _hasMore;
        private set => this.RaiseAndSetIfChanged(ref _hasMore, value);
    }

    public ReactiveCommand<IMediaItem, Unit> OpenItemCommand { get; }
    public ReactiveCommand<Unit, Unit> LoadMoreCommand { get; }

    public async Task LoadDataAsync()
    {
        Items.Clear();
        _loadedCount = 0;
        HasMore = true;
        await LoadMoreAsync();
    }

    private async Task LoadMoreAsync()
    {
        if (_mediaServerService.CurrentServer == null || IsLoading) return;

        IsLoading = true;
        try
        {
            var items = await _mediaServerService.CurrentServer.Library
                .GetItemsAsync(Library.Id, _loadedCount, PageSize);

            int count = 0;
            foreach (var item in items)
            {
                Items.Add(item);
                count++;
            }

            _loadedCount += count;
            HasMore = count >= PageSize;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading library items: {ex.Message}");
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
