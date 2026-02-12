using System;
using System.Collections.ObjectModel;
using System.Reactive;
using System.Threading.Tasks;
using ReactiveUI;
using XPlayer.Core.Models;
using XPlayer.Desktop.Services;
using XPlayer.Desktop.Services.Media;

namespace XPlayer.Desktop.ViewModels.Pages.Details;

/// <summary>
/// Collection detail page showing items in a Jellyfin collection/boxset.
/// </summary>
public class CollectionDetailViewModel : ViewModelBase
{
    private readonly MediaServerService _mediaServerService;
    private readonly ContentNavigationService _contentNav;
    private bool _isLoading;

    public CollectionDetailViewModel(
        MediaServerService mediaServerService,
        ContentNavigationService contentNav,
        IMediaItem collection)
    {
        _mediaServerService = mediaServerService;
        _contentNav = contentNav;
        Collection = collection;

        OpenItemCommand = ReactiveCommand.Create<IMediaItem>(OpenItem);
    }

    public IMediaItem Collection { get; }
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
            var items = await _mediaServerService.CurrentServer.Library
                .GetItemsAsync(Collection.Id, 0, 200);
            foreach (var item in items)
                Items.Add(item);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading collection: {ex.Message}");
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
