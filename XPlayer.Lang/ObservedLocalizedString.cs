using System.ComponentModel;

namespace XPlayer.Lang;

public class ObservedLocalizedString : IDisposable
{
    private readonly string _key;
    private readonly Action<string> _onUpdate;

    public ObservedLocalizedString(string key, Action<string> onUpdate)
    {
        _key = key;
        _onUpdate = onUpdate;
        
        // Initial Update
        Update();
        
        LocalizationManager.Instance.PropertyChanged += OnPropertyChanged;
    }

    private void Update()
    {
        _onUpdate(LocalizationManager.Instance[_key]);
    }

    private void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (string.IsNullOrEmpty(e.PropertyName) || e.PropertyName == "Item" || e.PropertyName == "Item[]")
        {
            Update();
        }
    }

    public void Dispose()
    {
        LocalizationManager.Instance.PropertyChanged -= OnPropertyChanged;
    }
}
