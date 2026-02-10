using System;
using System.Net.Http;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using ReactiveUI;
using XPlayer.Core.Models;
using XPlayer.Core.Network;
using XPlayer.Desktop.Services.Abstractions;
using XPlayer.Lang;
using Microsoft.Extensions.Logging;
using System.Linq; // For FirstOrDefault

namespace XPlayer.Desktop.Services.Media;

/// <summary>
/// Desktop-side service that wraps IMediaProvider/IMediaServer.
/// Manages connection state and current user information for UI binding.
/// </summary>
public class MediaServerService : ReactiveObject, IDisposable
{
    private readonly IMediaProvider _provider;
    private IMediaServer? _currentServer;
    private IUserInfo? _currentUser;
    private Bitmap? _userAvatar;
    private bool _isConnected;
    private string _displayName;
    private string _statusText;
    private readonly Bitmap _defaultAvatar;
    private readonly IClientProfile _clientProfile;
    private readonly ILogger<MediaServerService> _logger;
    private readonly IConfigurationService _configService;

    public MediaServerService(
        IMediaProvider provider,
        IClientProfile clientProfile,
        IConfigurationService configService,
        ILogger<MediaServerService> logger)
    {
        _provider = provider;
        _clientProfile = clientProfile;
        _configService = configService;
        _logger = logger;
        _displayName = LocalizationManager.Instance["GuestUser"];
        _statusText = LocalizationManager.Instance["NotConnected"];
        
        // Load default avatar
        using var stream = AssetLoader.Open(new Uri("avares://XPlayer.Desktop/Assets/Images/user_default_avatar.jpg"));
        _defaultAvatar = new Bitmap(stream);
        _userAvatar = _defaultAvatar;
        
        // Update display texts when language changes
        LocalizationManager.Instance.PropertyChanged += (_, _) =>
        {
            if (!_isConnected)
            {
                DisplayName = LocalizationManager.Instance["GuestUser"];
                StatusText = LocalizationManager.Instance["NotConnected"];
            }
        };
        
        // Try to auto-connect if there is a default source
        // Do this in background to not block startup
        Task.Run(AutoConnectAsync);
    }
    
    private async Task AutoConnectAsync()
    {
        try
        {
            // Give some time for app to initialize
            await Task.Delay(500);
            
            var sources = _configService.MediaSources?.Sources;
            if (sources == null || sources.Count == 0) return;
            
            var defaultSource = sources.FirstOrDefault(x => x.IsDefault);
            if (defaultSource != null && !string.IsNullOrEmpty(defaultSource.AccessToken))
            {
                 _logger.LogInformation("Auto-connecting to default source: {Name}", defaultSource.Name);
                 // We need to dispatch to UI thread if ConnectWithTokenAsync touches UI bound properties?
                 // MediaServerService uses ReactiveUI properties so raise property change should be on UI thread?
                 // ReactiveUI RaiseAndSetIfChanged handles property change notification, but if bound to UI, 
                 // Avalonia generally handles it, but safer to Dispatch.
                 // However, we are updating non-UI properties first. 
                 // Let's rely on Avalonia handling cross-thread property changes or Wrap in Dispatcher.UIThread if needed.
                 // Actually, ReactiveUI is thread-safe for property changes but binding to UI might want UI thread.
                 // Let's just run it. If it fails, we wrap.
                 await ConnectWithTokenAsync(defaultSource.Url, defaultSource.AccessToken, defaultSource.Username, defaultSource.EncryptedPassword);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to auto-connect to default media source");
        }
    }

    public bool IsConnected
    {
        get => _isConnected;
        private set => this.RaiseAndSetIfChanged(ref _isConnected, value);
    }

    public IMediaServer? CurrentServer
    {
        get => _currentServer;
        private set => this.RaiseAndSetIfChanged(ref _currentServer, value);
    }

    public IUserInfo? CurrentUser
    {
        get => _currentUser;
        private set => this.RaiseAndSetIfChanged(ref _currentUser, value);
    }

    public Bitmap? UserAvatar
    {
        get => _userAvatar;
        private set => this.RaiseAndSetIfChanged(ref _userAvatar, value);
    }

    public string DisplayName
    {
        get => _displayName;
        private set => this.RaiseAndSetIfChanged(ref _displayName, value);
    }

    public string StatusText
    {
        get => _statusText;
        private set => this.RaiseAndSetIfChanged(ref _statusText, value);
    }

    /// <summary>
    /// Connect to a media server and authenticate.
    /// </summary>
    public async Task ConnectAndLoginAsync(string url, string username, string password)
    {
        var server = await _provider.ConnectAsync(url);
        if (server == null || !server.IsConnected)
            throw new InvalidOperationException("Failed to connect to server.");

        var authOk = await server.Authentication.AuthenticateByNameAsync(username, password);
        if (!authOk)
        {
            server.Dispose();
            throw new InvalidOperationException("Authentication failed.");
        }

        CurrentServer = server;
        IsConnected = true;

        var user = server.Authentication.CurrentUser;
        CurrentUser = user;
        DisplayName = user?.Name ?? LocalizationManager.Instance["GuestUser"];
        StatusText = server.Name;

        await LoadUserAvatarAsync();
    }

    /// <summary>
    /// Connect to a media server using an access token (session restore).
    /// </summary>
    public async Task ConnectWithTokenAsync(string url, string accessToken, string? username = null, string? encryptedPassword = null)
    {
        var server = await _provider.ConnectAsync(url);
        if (server == null || !server.IsConnected)
            throw new InvalidOperationException("Failed to connect to server.");

        var authOk = await server.Authentication.AuthenticateByTokenAsync(accessToken);
        
        // If token auth failed and we have credentials, try to re-login
        if (!authOk && !string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(encryptedPassword))
        {
            try 
            {
                var password = Desktop.Common.SecurityUtil.DecryptString(encryptedPassword);
                if (!string.IsNullOrEmpty(password))
                {
                    authOk = await server.Authentication.AuthenticateByNameAsync(username, password);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to re-authenticate with saved password.");
            }
        }

        if (!authOk)
        {
            server.Dispose();
            throw new InvalidOperationException("Authentication failed or session expired.");
        }

        CurrentServer = server;
        IsConnected = true;

        var user = server.Authentication.CurrentUser;
        CurrentUser = user;
        DisplayName = user?.Name ?? LocalizationManager.Instance["GuestUser"];
        StatusText = server.Name;

        await LoadUserAvatarAsync();
    }

    /// <summary>
    /// Logout from the current server.
    /// </summary>
    public async Task LogoutAsync()
    {
        if (_currentServer != null)
        {
            try
            {
                await _currentServer.Authentication.LogoutAsync();
            }
            catch
            {
                // Ignore logout errors
            }
        }

        ResetState();
    }

    /// <summary>
    /// Load user avatar from the server. Falls back to default avatar.
    /// </summary>
    private async Task LoadUserAvatarAsync()
    {
        if (_currentUser == null || !_currentUser.HasPrimaryImage || string.IsNullOrEmpty(_currentUser.AvatarUrl))
        {
            UserAvatar = _defaultAvatar;
            return;
        }

        try
        {
            using var httpClient = new HttpClient();
            var imageBytes = await httpClient.GetByteArrayAsync(_currentUser.AvatarUrl);
            using var memoryStream = new System.IO.MemoryStream(imageBytes);
            UserAvatar = new Bitmap(memoryStream);
        }
        catch
        {
            UserAvatar = _defaultAvatar;
        }
    }

    private void ResetState()
    {
        CurrentServer = null;
        CurrentUser = null;
        IsConnected = false;
        UserAvatar = _defaultAvatar;
        DisplayName = LocalizationManager.Instance["GuestUser"];
        StatusText = LocalizationManager.Instance["NotConnected"];
    }

    public void Dispose()
    {
        _currentServer?.Dispose();
        // Don't dispose _defaultAvatar here as it's shared
    }
}
