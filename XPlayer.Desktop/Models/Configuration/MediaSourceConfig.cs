using System;
using Newtonsoft.Json;
using ReactiveUI;
using XPlayer.Core.Models;

namespace XPlayer.Desktop.Models.Configuration;

[JsonObject(MemberSerialization.OptIn)]
public class MediaSourceConfig : ReactiveObject
{
    [JsonProperty]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    private string _name = string.Empty;
    [JsonProperty]
    public string Name
    {
        get => _name;
        set => this.RaiseAndSetIfChanged(ref _name, value);
    }

    private string _url = string.Empty;
    [JsonProperty]
    public string Url
    {
        get => _url;
        set => this.RaiseAndSetIfChanged(ref _url, value);
    }

    private MediaProviderType _providerType = MediaProviderType.Jellyfin;
    [JsonProperty]
    [JsonConverter(typeof(Newtonsoft.Json.Converters.StringEnumConverter))]
    public MediaProviderType ProviderType
    {
        get => _providerType;
        set => this.RaiseAndSetIfChanged(ref _providerType, value);
    }

    private string? _username;
    [JsonProperty]
    public string? Username
    {
        get => _username;
        set => this.RaiseAndSetIfChanged(ref _username, value);
    }

    private string? _accessToken;
    [JsonProperty]
    public string? AccessToken
    {
        get => _accessToken;
        set => this.RaiseAndSetIfChanged(ref _accessToken, value);
    }

    private string? _encryptedPassword;
    [JsonProperty]
    public string? EncryptedPassword
    {
        get => _encryptedPassword;
        set => this.RaiseAndSetIfChanged(ref _encryptedPassword, value);
    }

    private bool _isDefault;
    [JsonProperty]
    public bool IsDefault
    {
        get => _isDefault;
        set => this.RaiseAndSetIfChanged(ref _isDefault, value);
    }
}
