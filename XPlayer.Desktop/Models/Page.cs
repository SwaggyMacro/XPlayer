using Material.Icons;
using ReactiveUI;
using XPlayer.Desktop.ViewModels;

namespace XPlayer.Desktop.Models;

public abstract class Page : ViewModelBase
{
    private string _displayName;
    // Keep reference to avoid GC if necessary, though event subscription keeps it alive until disposed.
    // Ideally we should make Page IDisposable but ViewModelBase is not always disposed properly by consumers.
    // However, ObservedLocalizedString subscribes to a singleton, so it will live forever unless disposed.
    // For now, let's keep a reference to it.
    private readonly XPlayer.Lang.ObservedLocalizedString _observedTitle;

    private MaterialIconKind _icon;

    private int _index;

    protected Page(string displayName, MaterialIconKind icon, int index = 0)
    {
        _displayName = displayName; 
        _observedTitle = new Lang.ObservedLocalizedString(displayName, val => DisplayName = val);
        _icon = icon;
        _index = index;
    }

    public string DisplayName
    {
        get => _displayName;
        set => this.RaiseAndSetIfChanged(ref _displayName, value);
    }

    public MaterialIconKind Icon
    {
        get => _icon;
        set => this.RaiseAndSetIfChanged(ref _icon, value);
    }

    public int Index
    {
        get => _index;
        set => this.RaiseAndSetIfChanged(ref _index, value);
    }
}