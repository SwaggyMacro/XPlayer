using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Threading;
using Avalonia.Controls.Notifications;
using EasyChat.Services.Languages;
using Material.Icons;
using ReactiveUI;
using SukiUI.Dialogs;
using SukiUI.Toasts;
using XPlayer.Desktop.Common;
using XPlayer.Desktop.Models;
using XPlayer.Desktop.Models.Configuration;
using XPlayer.Desktop.Services.Abstractions;
using XPlayer.Lang;

// Added for Global

namespace XPlayer.Desktop.ViewModels.Pages;

[SuppressMessage("ReSharper", "NotAccessedField.Local")]
public class SettingViewModel : Page
{
    private readonly IConfigurationService _configurationService;
    private readonly ISukiDialogManager _dialogManager;
    private readonly ISukiToastManager _toastManager;
    public General? GeneralConf => _configurationService.General;


    public SettingViewModel(ISukiDialogManager dialogManager, ISukiToastManager toastManager, IConfigurationService configurationService) : base(
        LocalizationManager.Instance["Settings"], MaterialIconKind.Settings, 1)
    {
        _dialogManager = dialogManager;
        _toastManager = toastManager;
        _configurationService = configurationService;
        
    }

    public List<LanguageDefinition> Languages
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    } = [LanguageKeys.English, LanguageKeys.ChineseSimplified];

    public LanguageDefinition? SelectedLanguage
    {
        get => Languages.FirstOrDefault(l => l.EnglishName == GeneralConf?.Language) ?? LanguageKeys.English;
        set
        {
            if (value != null && GeneralConf?.Language != value.EnglishName)
            {
                GeneralConf?.Language = value.EnglishName;
                this.RaisePropertyChanged();

                // Update Culture
                var culture = value.EnglishName switch
                {
                    "Simplified Chinese" => new CultureInfo("zh-CN"),
                    _ => new CultureInfo("en-US")
                };
                LocalizationManager.Instance.CurrentCulture = culture;
                Thread.CurrentThread.CurrentUICulture = culture;
                Thread.CurrentThread.CurrentCulture = culture;

                // Save Config handled by service automatically

                // Notify User
                var title = LocalizationManager.Instance["LanguageChanged"];
                var content = LocalizationManager.Instance["LanguageChanged"];

                Global.ToastManager.CreateSimpleInfoToast()
                    .OfType(NotificationType.Success)
                    .WithTitle(title)
                    .WithContent(content)
                    .Queue();
            }
        }
    }

    public List<WindowClosingBehavior> ClosingBehaviors { get; } = Enum.GetValues<WindowClosingBehavior>().ToList();

    public WindowClosingBehavior SelectedClosingBehavior
    {
        get => GeneralConf!.ClosingBehavior;
        set
        {
            if (GeneralConf!.ClosingBehavior == value) return;
            GeneralConf.ClosingBehavior = value;
            this.RaisePropertyChanged();
        }
    }
}