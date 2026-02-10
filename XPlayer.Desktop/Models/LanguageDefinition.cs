using System.Collections.Generic;
using System.Threading;
using Newtonsoft.Json;
using EasyChat.Services.Languages;

namespace XPlayer.Desktop.Models;

/// <summary>
/// Represents a specific language with supported codes for various providers.
/// </summary>
public class LanguageDefinition : System.ComponentModel.INotifyPropertyChanged
{
    /// <summary>
    /// Internal Unique Identifier for the language (e.g., "en-US", "zh-CN").
    /// </summary>
    [JsonProperty]
    public string Id { get; private set; }

    /// <summary>
    /// Chinese Name of the language (e.g., "简体中文", "英语").
    /// </summary>
    [JsonProperty]
    public string ChineseName { get; private set; }

    /// <summary>
    /// English Name used for AI prompts and English UI (e.g., "Simplified Chinese", "English").
    /// </summary>
    [JsonProperty]
    public string EnglishName { get; private set; }

    /// <summary>
    /// Filename of the flag icon (e.g., "cn.png").
    /// </summary>
    [JsonProperty]
    public string Icon { get; private set; }

    /// <summary>
    /// Returns the appropriate localized name based on current UI culture.
    /// Returns ChineseName if culture is Chinese, otherwise EnglishName.
    /// </summary>
    public string LocalizedName
    {
        get
        {
            var culture = Thread.CurrentThread.CurrentUICulture;
            return culture.Name switch{
                LanguageKeys.ChineseSimplifiedId => ChineseName,
                _ => EnglishName
            };
        }
    }

    /// <summary>
    /// Alias for LocalizedName for backward compatibility and XAML binding.
    /// </summary>
    public string DisplayName => LocalizedName;

    /// <summary>
    /// Dictionary of Provider Name -> Short Code.
    /// </summary>
    [JsonProperty]
    public Dictionary<string, string> ProviderCodes { get; private set; } = new();

    /// <summary>
    /// Private constructor for JSON deserialization.
    /// </summary>
    [JsonConstructor]
    private LanguageDefinition()
    {
        Id = string.Empty;
        ChineseName = string.Empty;
        EnglishName = string.Empty;
        Icon = string.Empty;
    }

    public LanguageDefinition(string id, string chineseName, string englishName, string icon)
    {
        Id = id;
        ChineseName = chineseName;
        EnglishName = englishName;
        Icon = icon;
        
        XPlayer.Lang.LocalizationManager.Instance.PropertyChanged += (_, e) =>
        {
             if (string.IsNullOrEmpty(e.PropertyName) || e.PropertyName == "Item" || e.PropertyName == "Item[]" || e.PropertyName == nameof(XPlayer.Lang.LocalizationManager.CurrentCulture))
             {
                 OnPropertyChanged(nameof(LocalizedName));
                 OnPropertyChanged(nameof(DisplayName));
             }
        };
    }
    
    public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;
    
    protected virtual void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(propertyName));
    }
}
