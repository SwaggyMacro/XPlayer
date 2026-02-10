using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.Http;
using System.Reactive;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using ReactiveUI;
using SukiUI.Dialogs;
using SukiUI.Toasts;
using XPlayer.Desktop.Common;
using XPlayer.Desktop.Models.Configuration;
using XPlayer.Desktop.Services.Abstractions;
using XPlayer.Desktop.Services.Media;
using XPlayer.Core.Models;
using System.Collections.Generic;
using XPlayer.Lang;

namespace XPlayer.Desktop.ViewModels.Dialogs;

public class MediaSourceDialogViewModel : ViewModelBase
{
    private readonly ISukiDialog _dialog;
    private readonly MediaServerService _mediaServerService;
    private readonly IConfigurationService _configService;
    private readonly ISukiToastManager _toastManager;

    public ObservableCollection<MediaSourceConfig> Sources => _configService.MediaSources?.Sources ?? new ObservableCollection<MediaSourceConfig>();

    private bool _isEditing;
    public bool IsEditing
    {
        get => _isEditing;
        set => this.RaiseAndSetIfChanged(ref _isEditing, value);
    }

    private string _dialogTitle = LocalizationManager.Instance["MediaSourceManagement"];
    public string DialogTitle
    {
        get => _dialogTitle;
        set => this.RaiseAndSetIfChanged(ref _dialogTitle, value);
    }

    // Form Properties
    private string _editId = string.Empty;
    public string EditId
    {
        get => _editId;
        set => this.RaiseAndSetIfChanged(ref _editId, value);
    }
    
    private string _editName = string.Empty;
    public string EditName
    {
        get => _editName;
        set => this.RaiseAndSetIfChanged(ref _editName, value);
    }

    private string _editUrl = "http://";
    public string EditUrl
    {
        get => _editUrl;
        set => this.RaiseAndSetIfChanged(ref _editUrl, value);
    }

    private string _editUsername = string.Empty;
    public string EditUsername
    {
        get => _editUsername;
        set => this.RaiseAndSetIfChanged(ref _editUsername, value);
    }

    private string _editPassword = string.Empty;
    public string EditPassword
    {
        get => _editPassword;
        set => this.RaiseAndSetIfChanged(ref _editPassword, value);
    }

    private MediaProviderType _editProvider = MediaProviderType.Jellyfin;
    public MediaProviderType EditProvider
    {
        get => _editProvider;
        set => this.RaiseAndSetIfChanged(ref _editProvider, value);
    }
    
    public IEnumerable<MediaProviderType> ProviderTypes => Enum.GetValues<MediaProviderType>();

    private bool _isBusy;
    public bool IsBusy
    {
        get => _isBusy;
        set => this.RaiseAndSetIfChanged(ref _isBusy, value);
    }

    private bool _isPasswordVisible;
    public bool IsPasswordVisible
    {
        get => _isPasswordVisible;
        set => this.RaiseAndSetIfChanged(ref _isPasswordVisible, value);
    }

    // Commands
    public ReactiveCommand<Unit, Unit> CloseCommand { get; }
    public ReactiveCommand<Unit, Unit> AddNewCommand { get; }
    public ReactiveCommand<MediaSourceConfig, Unit> EditSourceCommand { get; }
    public ReactiveCommand<MediaSourceConfig, Unit> DeleteSourceCommand { get; }
    public ReactiveCommand<MediaSourceConfig, Unit> ConnectSourceCommand { get; }
    public ReactiveCommand<Unit, Unit> CancelEditCommand { get; }
    public ReactiveCommand<Unit, Unit> TestConnectionCommand { get; }
    public ReactiveCommand<Unit, Unit> SaveAndConnectCommand { get; }
    public ReactiveCommand<Unit, Unit> TogglePasswordVisibilityCommand { get; }

    public MediaSourceDialogViewModel(ISukiDialog dialog, MediaServerService mediaServerService)
    {
        _dialog = dialog;
        _mediaServerService = mediaServerService;
        
        // Resolve optional dependencies manually to avoid changing MainWindowViewModel too much right now,
        // though proper injection is preferred.
        _configService = Global.Services?.GetRequiredService<IConfigurationService>()!;
        _toastManager = Global.Services?.GetRequiredService<ISukiToastManager>()!;

        CloseCommand = ReactiveCommand.Create(() => _dialog.Dismiss());
        AddNewCommand = ReactiveCommand.Create(StartAddNew);
        EditSourceCommand = ReactiveCommand.Create<MediaSourceConfig>(StartEdit);
        DeleteSourceCommand = ReactiveCommand.Create<MediaSourceConfig>(DeleteSource);
        ConnectSourceCommand = ReactiveCommand.CreateFromTask<MediaSourceConfig>(ConnectSourceAsync);
        CancelEditCommand = ReactiveCommand.Create(CancelEdit);
        TestConnectionCommand = ReactiveCommand.CreateFromTask(TestConnectionAsync);
        SaveAndConnectCommand = ReactiveCommand.CreateFromTask(SaveAndConnectAsync);
        TogglePasswordVisibilityCommand = ReactiveCommand.Create(() => { IsPasswordVisible = !IsPasswordVisible; });
    }
    
    private void StartAddNew()
    {
        EditId = Guid.NewGuid().ToString();
        EditName = "New Jellyfin";
        EditUrl = "http://";
        EditUsername = "";
        EditPassword = "";
        IsPasswordVisible = false;
        DialogTitle = LocalizationManager.Instance["AddMediaSource"];
        IsEditing = true;
    }

    private void StartEdit(MediaSourceConfig source)
    {
        EditId = source.Id;
        EditName = source.Name;
        EditUrl = source.Url;
        EditUsername = source.Username ?? "";
        
        // Decrypt password if available
        EditPassword = !string.IsNullOrEmpty(source.EncryptedPassword) 
            ? SecurityUtil.DecryptString(source.EncryptedPassword) 
            : "";
            
        IsPasswordVisible = false;
        DialogTitle = LocalizationManager.Instance["EditMediaSource"];
        IsEditing = true;
    }

    private void CancelEdit()
    {
        IsEditing = false;
        IsPasswordVisible = false;
        DialogTitle = LocalizationManager.Instance["MediaSourceManagement"];
    }

    private void DeleteSource(MediaSourceConfig source)
    {
        Sources.Remove(source);
        _configService.SaveMediaSources();
    }

    private async Task ConnectSourceAsync(MediaSourceConfig source)
    {
        try
        {
            IsBusy = true;
            if (!string.IsNullOrEmpty(source.AccessToken))
            {
                // Has token, try connect with token, pass credentials for potential re-auth
                await _mediaServerService.ConnectWithTokenAsync(source.Url, source.AccessToken, source.Username, source.EncryptedPassword);
            }
            else if (!string.IsNullOrEmpty(source.Username))
            {
                // No token (maybe migrated or legacy), prompt user or try to just connect (won't work without pwd)
                // For now, assume if no token, user needs to edit and re-save/re-login
                _toastManager.CreateSimpleInfoToast()
                    .WithTitle(LocalizationManager.Instance["AuthFailed"])
                    .WithContent("Please edit and re-enter password to update token.")
                    .Queue();
                return;
            }

            // Set as default since user explicitly connected
            foreach (var s in Sources) s.IsDefault = false;
            source.IsDefault = true;
            
            // Persist the default selection
            _configService.SaveMediaSources();
            
            _toastManager.CreateSimpleInfoToast()
                .WithTitle(LocalizationManager.Instance["ConnectionSuccess"])
                .WithContent(source.Name)
                .Queue();
            
            _dialog.Dismiss();
        }
        catch (Exception ex)
        {
            _toastManager.CreateSimpleInfoToast()
                .WithTitle(LocalizationManager.Instance["ConnectionFailed"])
                .WithContent(ex.Message)
                .Queue();
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task TestConnectionAsync()
    {
        if (string.IsNullOrWhiteSpace(EditUrl)) return;
        
        IsBusy = true;
        try
        {
            // Just test connection logic here... simplified
            // We can reuse MediaServerService logic but without setting persistent state
            using var client = new HttpClient();
            var response = await client.GetAsync(EditUrl + "/System/Info/Public"); // Jellyfin endpoint
            if (response.IsSuccessStatusCode)
            {
                _toastManager.CreateSimpleInfoToast()
                     .WithTitle("Success")
                     .WithContent("Server reachable.")
                     .Queue();
            }
            else
            {
                throw new Exception($"HTTP {response.StatusCode}");
            }
        }
        catch (Exception ex)
        {
            _toastManager.CreateSimpleInfoToast()
                 .WithTitle(LocalizationManager.Instance["ConnectionFailed"])
                 .WithContent(ex.Message)
                 .Queue();
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task SaveAndConnectAsync()
    {
        if (string.IsNullOrWhiteSpace(EditName) || string.IsNullOrWhiteSpace(EditUrl))
        {
             _toastManager.CreateSimpleInfoToast().WithTitle("Error").WithContent("Name and URL are required.").Queue();
             return;
        }

        IsBusy = true;
        try
        {
            // 1. Verify and Login to get Token
            await _mediaServerService.ConnectAndLoginAsync(EditUrl, EditUsername, EditPassword);
            
            // 2. Get the new token
            // MediaServerService needs to expose the token or we assume it's handled internally.
            // Wait, MediaServerService wraps IMediaServer. Authentication typically happens there.
            // We need to extract the AccessToken from the connected server to save it.
            var token = _mediaServerService.CurrentServer?.Authentication.AccessToken;

            // 3. Save/Update Config
            var existing = Sources.FirstOrDefault(x => x.Id == EditId);
            
            // Encrypt password if provided, otherwise keep existing
            string? encryptedPwd = null;
            if (!string.IsNullOrEmpty(EditPassword))
            {
                encryptedPwd = SecurityUtil.EncryptString(EditPassword);
            }
            
            if (existing != null)
            {
                existing.Name = EditName;
                existing.Url = EditUrl;
                existing.Username = EditUsername;
                existing.AccessToken = token;
                
                // Only update password if user entered a new one
                if (!string.IsNullOrEmpty(encryptedPwd))
                {
                    existing.EncryptedPassword = encryptedPwd;
                }
                
                existing.IsDefault = true;
                
                // Ensure only one default
                foreach (var s in Sources.Where(s => s != existing)) s.IsDefault = false;
            }
            else
            {
                // Ensure others are not default
                foreach (var s in Sources) s.IsDefault = false;
                
                Sources.Add(new MediaSourceConfig
                {
                    Id = EditId,
                    Name = EditName,
                    Url = EditUrl,
                    Username = EditUsername,
                    AccessToken = token,
                    EncryptedPassword = encryptedPwd,
                    IsDefault = true,
                    ProviderType = EditProvider
                });
            }

            // Manually save configuration to persist changes to existing items or new default status
            _configService.SaveMediaSources();

            _toastManager.CreateSimpleInfoToast()
                .WithTitle(LocalizationManager.Instance["ConnectionSuccess"])
                .WithContent(EditName)
                .Queue();

            _dialog.Dismiss();
        }
        catch (Exception ex)
        {
            await _mediaServerService.LogoutAsync(); // Cleanup partial state if failed
            _toastManager.CreateSimpleInfoToast()
                .WithTitle(LocalizationManager.Instance["ConnectionFailed"])
                .WithContent(ex.Message)
                .Queue();
        }
        finally
        {
            IsBusy = false;
        }
    }
}
