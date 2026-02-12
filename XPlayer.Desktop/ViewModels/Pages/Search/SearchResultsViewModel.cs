using System;
using System.Collections.ObjectModel;
using System.Reactive;
using System.Threading.Tasks;
using ReactiveUI;
using XPlayer.Core.Models;
using XPlayer.Desktop.Services;
using XPlayer.Desktop.Services.Media;
using XPlayer.Desktop.ViewModels;

using XPlayer.Desktop.ViewModels.Pages.Details;

namespace XPlayer.Desktop.ViewModels.Pages.Search;

/// <summary>
/// Search results page showing items matching a query.
/// </summary>
public class SearchResultsViewModel : ViewModelBase
{
    private readonly MediaServerService _mediaServerService;
    private readonly ContentNavigationService _contentNav;
    private string _query = string.Empty;
    private bool _isLoading;

    public SearchResultsViewModel(
        MediaServerService mediaServerService,
        ContentNavigationService contentNav,
        string query = "")
    {
        _mediaServerService = mediaServerService;
        _contentNav = contentNav;
        _query = query;

        OpenItemCommand = ReactiveCommand.Create<IMediaItem>(OpenItem);
        SearchCommand = ReactiveCommand.CreateFromTask<string>(PerformSearchAsync);
    }

    public string Query
    {
        get => _query;
        set => this.RaiseAndSetIfChanged(ref _query, value);
    }

    public ObservableCollection<IMediaItem> Results { get; } = new();

    public bool IsLoading
    {
        get => _isLoading;
        private set => this.RaiseAndSetIfChanged(ref _isLoading, value);
    }

    public ReactiveCommand<IMediaItem, Unit> OpenItemCommand { get; }
    public ReactiveCommand<string, Unit> SearchCommand { get; }

    public async Task PerformSearchAsync(string query)
    {
        if (_mediaServerService.CurrentServer == null || string.IsNullOrWhiteSpace(query))
            return;

        Query = query;
        IsLoading = true;
        try
        {
            Results.Clear();
            var items = await _mediaServerService.CurrentServer.Library
                .SearchAsync(query, 50);
            foreach (var item in items)
                Results.Add(item);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error searching: {ex.Message}");
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
