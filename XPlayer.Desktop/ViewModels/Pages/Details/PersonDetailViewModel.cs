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
/// Person detail page showing their filmography.
/// </summary>
public class PersonDetailViewModel : ViewModelBase
{
    private readonly MediaServerService _mediaServerService;
    private readonly ContentNavigationService _contentNav;
    private bool _isLoading;

    public PersonDetailViewModel(
        MediaServerService mediaServerService,
        ContentNavigationService contentNav,
        IPerson person)
    {
        _mediaServerService = mediaServerService;
        _contentNav = contentNav;
        Person = person;

        OpenItemCommand = ReactiveCommand.Create<IMediaItem>(OpenItem);
    }

    public IPerson Person { get; }
    public ObservableCollection<IMediaItem> Filmography { get; } = new();

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
            Filmography.Clear();
            var items = await _mediaServerService.CurrentServer.Library
                .GetPersonItemsAsync(Person.Name);
            foreach (var item in items)
                Filmography.Add(item);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading filmography: {ex.Message}");
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
