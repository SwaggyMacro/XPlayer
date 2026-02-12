using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reactive;
using System.Reactive.Linq;
using ReactiveUI;
using XPlayer.Desktop.ViewModels;

namespace XPlayer.Desktop.Services;

/// <summary>
/// Stack-based content navigation service for multi-level drill-down within a page.
/// Supports back/forward navigation and breadcrumb trail.
/// Independent of the sidebar PageNavigationService.
/// </summary>
public class ContentNavigationService : ReactiveObject
{
    private readonly Stack<NavigationEntry> _backStack = new();
    private readonly Stack<NavigationEntry> _forwardStack = new();

    private ViewModelBase? _currentContent;

    public ContentNavigationService()
    {
        var canGoBack = this.WhenAnyValue(x => x.CanGoBack);
        var canGoForward = this.WhenAnyValue(x => x.CanGoForward);

        GoBackCommand = ReactiveCommand.Create(GoBack, canGoBack);
        GoForwardCommand = ReactiveCommand.Create(GoForward, canGoForward);
    }

    /// <summary>
    /// The currently displayed content ViewModel.
    /// Bind a ContentControl to this property.
    /// </summary>
    public ViewModelBase? CurrentContent
    {
        get => _currentContent;
        private set => this.RaiseAndSetIfChanged(ref _currentContent, value);
    }

    /// <summary>
    /// Breadcrumb trail representing the navigation path.
    /// </summary>
    public ObservableCollection<BreadcrumbItem> Breadcrumbs { get; } = new();

    private bool _canGoBack;
    private bool _canGoForward;
    
    public bool CanGoBack
    {
        get => _canGoBack;
        private set => this.RaiseAndSetIfChanged(ref _canGoBack, value);
    }

    public bool CanGoForward
    {
        get => _canGoForward;
        private set => this.RaiseAndSetIfChanged(ref _canGoForward, value);
    }

    public ReactiveCommand<Unit, Unit> GoBackCommand { get; }
    public ReactiveCommand<Unit, Unit> GoForwardCommand { get; }

    /// <summary>
    /// Navigate to a new sub-page. Pushes the current content onto the back stack
    /// and clears the forward stack.
    /// </summary>
    /// <param name="viewModel">The ViewModel to navigate to.</param>
    /// <param name="breadcrumbLabel">Label displayed in the breadcrumb trail.</param>
    public void NavigateTo(ViewModelBase viewModel, string breadcrumbLabel)
    {
        if (_currentContent != null)
        {
            // Push current state onto back stack
            _backStack.Push(new NavigationEntry(_currentContent, GetCurrentBreadcrumbLabel()));
        }

        // Clear forward stack on new navigation
        _forwardStack.Clear();

        // Set new content
        CurrentContent = viewModel;
        Breadcrumbs.Add(new BreadcrumbItem(breadcrumbLabel, viewModel));

        UpdateCanNavigate();
    }

    /// <summary>
    /// Navigate to the root page, clearing all history.
    /// </summary>
    /// <param name="rootViewModel">The root ViewModel.</param>
    /// <param name="breadcrumbLabel">Root breadcrumb label (e.g., "Home").</param>
    public void NavigateToRoot(ViewModelBase rootViewModel, string breadcrumbLabel)
    {
        _backStack.Clear();
        _forwardStack.Clear();
        Breadcrumbs.Clear();

        CurrentContent = rootViewModel;
        Breadcrumbs.Add(new BreadcrumbItem(breadcrumbLabel, rootViewModel));

        UpdateCanNavigate();
    }

    /// <summary>
    /// Navigate to a specific breadcrumb level. Trims breadcrumbs after that level
    /// and pushes intermediate entries onto the back stack.
    /// </summary>
    /// <param name="breadcrumbItem">The breadcrumb to navigate to.</param>
    public void NavigateToBreadcrumb(BreadcrumbItem breadcrumbItem)
    {
        var index = Breadcrumbs.IndexOf(breadcrumbItem);
        if (index < 0 || breadcrumbItem.ViewModel == _currentContent) return;

        // Push current state onto back stack
        if (_currentContent != null)
        {
            _backStack.Push(new NavigationEntry(_currentContent, GetCurrentBreadcrumbLabel()));
        }

        // Clear forward stack
        _forwardStack.Clear();

        // Remove breadcrumbs after the target
        while (Breadcrumbs.Count > index + 1)
        {
            Breadcrumbs.RemoveAt(Breadcrumbs.Count - 1);
        }

        CurrentContent = breadcrumbItem.ViewModel;
        UpdateCanNavigate();
    }

    /// <summary>
    /// Go back to the previous content.
    /// </summary>
    public void GoBack()
    {
        if (_backStack.Count == 0) return;

        // Push current onto forward stack
        if (_currentContent != null)
        {
            _forwardStack.Push(new NavigationEntry(_currentContent, GetCurrentBreadcrumbLabel()));
        }

        var entry = _backStack.Pop();
        CurrentContent = entry.ViewModel;

        // Restore breadcrumbs: remove the last one
        if (Breadcrumbs.Count > 1)
        {
            Breadcrumbs.RemoveAt(Breadcrumbs.Count - 1);
        }

        UpdateCanNavigate();
    }

    /// <summary>
    /// Go forward to the next content (available after GoBack).
    /// </summary>
    public void GoForward()
    {
        if (_forwardStack.Count == 0) return;

        // Push current onto back stack
        if (_currentContent != null)
        {
            _backStack.Push(new NavigationEntry(_currentContent, GetCurrentBreadcrumbLabel()));
        }

        var entry = _forwardStack.Pop();
        CurrentContent = entry.ViewModel;

        // Add breadcrumb for forward destination
        Breadcrumbs.Add(new BreadcrumbItem(entry.BreadcrumbLabel, entry.ViewModel));

        UpdateCanNavigate();
    }

    /// <summary>
    /// Clear all navigation state.
    /// </summary>
    public void Clear()
    {
        _backStack.Clear();
        _forwardStack.Clear();
        Breadcrumbs.Clear();
        CurrentContent = null;
        UpdateCanNavigate();
    }

    private string GetCurrentBreadcrumbLabel()
    {
        if (Breadcrumbs.Count > 0)
        {
            return Breadcrumbs[^1].Label;
        }
        return string.Empty;
    }

    private void UpdateCanNavigate()
    {
        CanGoBack = _backStack.Count > 0;
        CanGoForward = _forwardStack.Count > 0;
    }

    private record NavigationEntry(ViewModelBase ViewModel, string BreadcrumbLabel);
}

/// <summary>
/// Represents a single breadcrumb entry in the navigation trail.
/// </summary>
public record BreadcrumbItem(string Label, ViewModelBase ViewModel);
