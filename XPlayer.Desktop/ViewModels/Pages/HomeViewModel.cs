using System;
using System.Reactive;
using System.Reactive.Linq;
using Material.Icons;
using ReactiveUI;
using SukiUI.Dialogs;
using XPlayer.Desktop.Common;
using XPlayer.Desktop.Models;
using XPlayer.Desktop.Services;
using XPlayer.Desktop.Services.Media;
using XPlayer.Desktop.ViewModels.Dialogs;
using XPlayer.Desktop.ViewModels.Pages.Home;

namespace XPlayer.Desktop.ViewModels.Pages;

/// <summary>
/// Home page ViewModel that acts as a navigation host.
/// Contains a ContentNavigationService for multi-level drill-down.
/// Shows either the connected dashboard or a welcome/disconnected state.
/// </summary>
public class HomeViewModel : Page
{
    private readonly MediaServerService _mediaServerService;
    private bool _isConnected;

    public HomeViewModel(MediaServerService mediaServerService) 
        : base("Home", MaterialIconKind.Home, 0)
    {
        _mediaServerService = mediaServerService;
        ContentNav = new ContentNavigationService();

        // React to connection state changes
        _mediaServerService.WhenAnyValue(x => x.IsConnected)
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(connected =>
            {
                IsConnected = connected;
                if (connected)
                    OnConnected();
                else
                    OnDisconnected();
            });

        // Connect server command
        ConnectServerCommand = ReactiveCommand.Create(() =>
        {
            Global.DialogManager
                .CreateDialog()
                .WithViewModel(d => new MediaSourceDialogViewModel(d, _mediaServerService))
                .TryShow();
        });

        RefreshCommand = ReactiveCommand.CreateFromTask(async () =>
        {
            if (ContentNav.CurrentContent is HomeDashboardViewModel dashboard)
            {
                await dashboard.LoadDataAsync();
            }
        });
    }

    /// <summary>
    /// Content navigation service for multi-level drill-down.
    /// </summary>
    public ContentNavigationService ContentNav { get; }

    public bool IsConnected
    {
        get => _isConnected;
        private set => this.RaiseAndSetIfChanged(ref _isConnected, value);
    }

    public ReactiveCommand<Unit, Unit> ConnectServerCommand { get; }
    public ReactiveCommand<Unit, Unit> RefreshCommand { get; }

    private void OnConnected()
    {
        var dashboard = new HomeDashboardViewModel(
            _mediaServerService,
            ContentNav);
        ContentNav.NavigateToRoot(dashboard, "Home");

        // Trigger initial data load
        _ = dashboard.LoadDataAsync();
    }

    private void OnDisconnected()
    {
        ContentNav.Clear();
    }
}