using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using Avalonia.Collections;
using Avalonia.Styling;
using ReactiveUI;
using SukiUI;
using SukiUI.Dialogs;
using SukiUI.Models;
using SukiUI.Toasts;
using XPlayer.Desktop.Common;
using XPlayer.Desktop.Models;
using XPlayer.Desktop.Services;
using XPlayer.Lang;

namespace XPlayer.Desktop.ViewModels;

public class MainWindowViewModel : ViewModelBase
{
    private readonly SukiTheme _theme;

    private Page? _activePage;
    private IAvaloniaReadOnlyList<Page> _pages;
    
    public ISukiDialogManager DialogManager { get; }
    public ISukiToastManager ToastManager { get; }
    
    public IAvaloniaReadOnlyList<Page> Pages
    {
        get => _pages;
        set => this.RaiseAndSetIfChanged(ref _pages, value);
    }

    public IAvaloniaReadOnlyList<SukiColorTheme> Themes { get; }
    
    public bool TitleBarVisible
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    } = true;

    public Page? ActivePage
    {
        get => _activePage;
        set => this.RaiseAndSetIfChanged(ref _activePage, value);
    }

    public ThemeVariant BaseTheme
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }
    
    public ReactiveCommand<Unit, Unit> ToggleBaseThemeCommand { get; }
    public ReactiveCommand<Unit, Unit> CreateCustomThemeCommand { get; }
    public ReactiveCommand<Unit, Unit> ToggleTitleBarCommand { get; }
    public ReactiveCommand<string, Unit> OpenUrlCommand { get; }
    
    public MainWindowViewModel(IEnumerable<Page> pages,
        PageNavigationService pageNavigationService,
        ISukiToastManager toastManager,
        ISukiDialogManager dialogManager)
    {
        // Sort and assign pages
        var sortedPages = pages.OrderBy(x => x.Index).ThenBy(x => x.DisplayName).ToList();
        _pages = new AvaloniaList<Page>(sortedPages);

        // Use the first page as default active page if available
        if (sortedPages.Any()) _activePage = sortedPages.First();
        
        DialogManager = dialogManager;
        ToastManager = toastManager;
        
        _theme = SukiTheme.GetInstance();
        Themes = _theme.ColorThemes;
        // BackgroundStyles = _theme.BackgroundStyles; // Removed as it might not exist in this version
        BaseTheme = _theme.ActiveBaseTheme;

        // Commands
        ToggleBaseThemeCommand = ReactiveCommand.Create(ToggleBaseTheme);
        CreateCustomThemeCommand = ReactiveCommand.Create(CreateCustomTheme);
        ToggleTitleBarCommand = ReactiveCommand.Create(ToggleTitleBar);
        OpenUrlCommand = ReactiveCommand.Create<string>(OpenUrl);

        // Navigation
        pageNavigationService.NavigationRequested += pageType =>
        {
            var page = Pages.FirstOrDefault(x => x.GetType() == pageType);
            if (page is null || ActivePage?.GetType() == pageType) return;
            ActivePage = page;
        };

        // Theme Events
        _theme.OnBaseThemeChanged += variant =>
        {
            BaseTheme = variant;
            ToastManager.CreateSimpleInfoToast()
                .WithTitle(LocalizationManager.Instance["ThemeChangedTitle"])
                .WithContent($"{LocalizationManager.Instance["ThemeChangedContent"]} {variant}.")
                .Queue();
        };

        _theme.OnColorThemeChanged += theme =>
        {
            ToastManager.CreateSimpleInfoToast()
                .WithTitle(LocalizationManager.Instance["ColorChangedTitle"])
                .WithContent($"{LocalizationManager.Instance["ColorChangedContent"]} {theme.DisplayName}.")
                .Queue();
        };
    }
    
    private void ToggleBaseTheme()
    {
        _theme.SwitchBaseTheme();
    }

    public void ChangeTheme(SukiColorTheme theme)
    {
        _theme.ChangeColorTheme(theme);
    }

    private void CreateCustomTheme()
    {
        /*DialogManager.CreateDialog()
            .WithViewModel(dialog => new CustomThemeDialogViewModel(_theme, dialog))
            .TryShow();*/
    }

    private void ToggleTitleBar()
    {
        TitleBarVisible = !TitleBarVisible;
        ToastManager.CreateSimpleInfoToast()
            .WithTitle($"{LocalizationManager.Instance["TitleBarTitle"]} {(TitleBarVisible ? "Visible" : "Hidden")}")
            .WithContent($"{LocalizationManager.Instance["TitleBarContent"]} {(TitleBarVisible ? "shown" : "hidden")}.")
            .Queue();
    }
    
    private static void OpenUrl(string url)
    {
        UrlUtilities.OpenUrl(url);
    }
}