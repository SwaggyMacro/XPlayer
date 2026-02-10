using System;
using System.Net.Http;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using ReactiveUI;
using XPlayer.Core.Models;
using XPlayer.Core.Network;
using XPlayer.Lang;

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

    public MediaServerService(IMediaProvider provider)
    {
        _provider = provider;
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
