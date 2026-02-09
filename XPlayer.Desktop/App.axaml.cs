using System;
using System.Globalization;
using System.IO;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Material.Icons;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ReactiveUI;
using Serilog;
using SukiUI.Dialogs;
using SukiUI.Enums;
using SukiUI.Toasts;
using XPlayer.Desktop.Common;
using XPlayer.Desktop.Models;
using XPlayer.Desktop.Services;
using XPlayer.Desktop.Services.Abstractions;
using XPlayer.Desktop.ViewModels;
using XPlayer.Desktop.ViewModels.Pages;
using XPlayer.Desktop.Views;
using XPlayer.Lang;

namespace XPlayer.Desktop;

public class App : Application
{
    private TrayIcon? _trayIcon;
    
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
        // LocalizationManager.Instance.CurrentCulture = new CultureInfo("zh-CN");
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // Configure Serilog
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.File(
                    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs", "log_.txt"),
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: 7,
                    outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss} [{Level:u3}] {SourceContext}: {Message:lj}{NewLine}{Exception}")
                .CreateLogger();

            // Service Collection
            var services = new ServiceCollection();

            // Application Lifetime
            services.AddSingleton<IApplicationLifetime>(desktop);

            // Logging
            services.AddLogging(builder =>
            {
                builder.AddSerilog(dispose: true);
                builder.AddConsole();
                builder.AddDebug();
            });

            // SukiUI Managers
            services.AddSingleton<ISukiToastManager, SukiToastManager>();
            services.AddSingleton<ISukiDialogManager, SukiDialogManager>();
            
            // Configuration Service
            services.AddSingleton<IConfigurationService, ConfigurationService>();
            
            services.AddSingleton<PageNavigationService>();
            
            services.AddSingleton<AutoMapper.IMapper>(sp =>
            {
                var loggerFactory = sp.GetRequiredService<ILoggerFactory>();
                var configExpression = new AutoMapper.MapperConfigurationExpression();
                configExpression.AddMaps(typeof(App).Assembly);
                var mapperConfig = new AutoMapper.MapperConfiguration(configExpression, loggerFactory);
                return mapperConfig.CreateMapper();
            });
            
            // Pages
            services.AddTransient<Page, HomeViewModel>();
            
            services.AddSingleton<MainWindowViewModel>();
            
            // Update Check Service
            services.AddSingleton<UpdateCheckService>();
            
            var provider = services.BuildServiceProvider();
            Global.Services = provider;
            
            // Ensure ConfigurationService are initialized
            provider.GetRequiredService<IConfigurationService>();
            
            // Startup Main Window
            var mainViewModel = provider.GetRequiredService<MainWindowViewModel>();

            desktop.MainWindow = new MainWindow
            {
                DataContext = mainViewModel
            };

            desktop.Exit += (_, _) =>
            {
                _trayIcon?.Dispose();
                
                Log.CloseAndFlush();
                if (provider is IDisposable disposableProvider) disposableProvider.Dispose();
            };
            
            // Setup Reactive Tray Icon Management
            var configService = provider.GetRequiredService<IConfigurationService>();
            
            // Initial check
            if (configService.General != null)
            {
                UpdateTrayIconState(configService.General.ClosingBehavior);

                // Reactive check
                configService.General.WhenAnyValue(x => x.ClosingBehavior)
                    .Subscribe(UpdateTrayIconState);
            }

            // Check for updates on startup
            CheckForUpdatesAsync(provider);
            
            // Listen for theme changes to update icons
            ActualThemeVariantChanged += (_, _) =>
            {
                if (_trayIcon?.Menu != null)
                {
                    // Regenerate icons
                    var menu = _trayIcon.Menu;
                    if (menu.Items.Count >= 3) // Show, Separator, Exit
                    {
                        var showItem = (NativeMenuItem)menu.Items[0];
                        showItem.Icon = CreateMenuIcon(MaterialIconKind.WindowRestore);

                        var exitItem = (NativeMenuItem)menu.Items[2];
                        exitItem.Icon = CreateMenuIcon(MaterialIconKind.ExitToApp);
                    }
                }
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
    
    public void ForceShowTrayIcon()
    {
        if (_trayIcon == null)
        {
            InitializeTrayIcon();
        }
    }
    private void UpdateTrayIconState(Models.Configuration.WindowClosingBehavior behavior)
    {
        // Only show tray icon if behavior is MinimizeToTray
        if (behavior == Models.Configuration.WindowClosingBehavior.MinimizeToTray)
        {
            if (_trayIcon == null)
            {
                InitializeTrayIcon();
            }
        }
        else
        {
            RemoveTrayIcon();
        }
    }

    private void InitializeTrayIcon()
    {
        if (_trayIcon != null) return;

        using var stream = Avalonia.Platform.AssetLoader.Open(new Uri("avares://EasyChat/Assets/easychat-logo.ico"));
        _trayIcon = new TrayIcon
        {
            Icon = new WindowIcon(stream),
            ToolTipText = LocalizationManager.Instance["AppName"]
        };
        _trayIcon.Clicked += TrayIcon_OnClickListener;

        var menu = new NativeMenu();
        var showItem = new NativeMenuItem(LocalizationManager.Instance["TrayShow"]);
        showItem.Icon = CreateMenuIcon(MaterialIconKind.WindowRestore);
        showItem.Click += TrayIcon_OnShow;
        menu.Items.Add(showItem);
        
        menu.Items.Add(new NativeMenuItemSeparator());

        var exitItem = new NativeMenuItem(LocalizationManager.Instance["TrayExit"]);
        exitItem.Icon = CreateMenuIcon(MaterialIconKind.ExitToApp);
        exitItem.Click += TrayIcon_OnExit;
        menu.Items.Add(exitItem);

        _trayIcon.Menu = menu;
        
        if (GetValue(TrayIcon.IconsProperty) is { } icons)
        {
            icons.Add(_trayIcon);
        }
        else 
        {
             var newIcons = new TrayIcons { _trayIcon };
             SetValue(TrayIcon.IconsProperty, newIcons);
        }
    }

    private void RemoveTrayIcon()
    {
        if (_trayIcon == null) return;
        
        _trayIcon.Clicked -= TrayIcon_OnClickListener;
        _trayIcon.Dispose();
        
        if (GetValue(TrayIcon.IconsProperty) is { } icons)
        {
            icons.Remove(_trayIcon);
        }

        _trayIcon = null;
    }

    private void TrayIcon_OnClickListener(object? sender, EventArgs e) => ShowWindow();
    
    private void TrayIcon_OnShow(object? sender, EventArgs e) => ShowWindow();

    private void TrayIcon_OnExit(object? sender, EventArgs e)
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            if (desktop.MainWindow is MainWindow mainWindow)
            {
                mainWindow.IsExiting = true;
            }
            desktop.Shutdown();
        }
    }

    private void ShowWindow()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var window = desktop.MainWindow;
            if (window != null)
            {
                window.Show();
                window.WindowState = WindowState.Normal;
                window.Activate();
            }
        }
    }
    private Bitmap CreateMenuIcon(MaterialIconKind kind)
    {
        IBrush foreground;
        
        // Try to get the dynamic theme brush (standard Fluent theme key)
        if (TryGetResource("SystemControlForegroundBaseHighBrush", ActualThemeVariant, out var res) && res is IBrush brush)
        {
            foreground = brush;
        }
        else
        {
            // Fallback
            foreground = ActualThemeVariant == Avalonia.Styling.ThemeVariant.Dark ? Brushes.White : Brushes.Black;
        }

        var data = MaterialIconDataProvider.GetData(kind);
        var geometry = StreamGeometry.Parse(data);

        var path = new Avalonia.Controls.Shapes.Path
        {
            Data = geometry,
            Fill = foreground,
            Width = 24,
            Height = 24,
            Stretch = Stretch.Uniform
        };
        
        path.Measure(new Size(24, 24));
        path.Arrange(new Rect(0, 0, 24, 24));
        
        var bitmap = new RenderTargetBitmap(new PixelSize(24, 24), new Vector(96, 96));
        bitmap.Render(path);
        return bitmap;
    }

    private async void CheckForUpdatesAsync(IServiceProvider provider)
    {
        try
        {
            var updateService = provider.GetRequiredService<UpdateCheckService>();
            var updateInfo = await updateService.CheckForUpdateAsync();

            if (updateInfo != null)
            {
                Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                {
                    ShowActionToast(updateInfo, updateService);
                });
            }
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Failed to check for updates");
        }
    }
    
    private void ShowActionToast(Velopack.UpdateInfo updateInfo, UpdateCheckService updateService)
    {
        Global.ToastManager.CreateToast()
            .WithTitle(LocalizationManager.Instance["NewVersionAvailable"])
            .WithContent(string.Format(LocalizationManager.Instance["NewVersionContent"], updateInfo.TargetFullRelease.Version))
            .WithActionButton(LocalizationManager.Instance["Later"], _ => { }, true, SukiButtonStyles.Standard)
            .WithActionButton(LocalizationManager.Instance["Update"], _ => ShowUpdatingToast(updateInfo, updateService), true)
            .Queue();
    }

    private async void ShowUpdatingToast(Velopack.UpdateInfo updateInfo, UpdateCheckService updateService)
    {
        var progress = new ProgressBar { Value = 0 , ShowProgressText = true};
        var toast = Global.ToastManager.CreateToast()
            .WithTitle(LocalizationManager.Instance["Updating"])
            .WithContent(progress)
            .Queue();

        try
        {
            await updateService.DownloadAndRestartAsync(updateInfo, (p) =>
            {
                Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                {
                    progress.Value = p;
                });
            });
            
            Global.ToastManager.Dismiss(toast);
        }
        catch (Exception ex)
        {
            Global.ToastManager.Dismiss(toast);
            Log.Error(ex, "Update failed");
            
            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                Global.ToastManager.CreateToast()
                    .WithTitle(LocalizationManager.Instance["UpdateFailed"])
                    .WithContent(LocalizationManager.Instance["CheckNetwork"])
                    .Dismiss().After(TimeSpan.FromSeconds(5))
                    .Queue();
            });
        }
    }
}