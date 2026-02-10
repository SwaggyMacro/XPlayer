using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Resources;
using System.Threading;

namespace XPlayer.Lang;

public class LocalizationManager : INotifyPropertyChanged
{
    [SuppressMessage("ReSharper", "InconsistentNaming")] 
    private static readonly Lazy<LocalizationManager> _instance = new(() => new LocalizationManager());

    public static LocalizationManager Instance
    {
        get
        {
            if (_instance.Value._resourceManager == null)
                throw new InvalidOperationException("LocalizationManager is not initialized. Call Initialize() before accessing the instance.");
            return _instance.Value;
        }
    }
    
    

    private ResourceManager? _resourceManager;
    private CultureInfo _currentCulture = CultureInfo.InvariantCulture;

    public event PropertyChangedEventHandler? PropertyChanged;

    private LocalizationManager()
    {
        _resourceManager = new ResourceManager("XPlayer.Lang.Resources.Strings", typeof(LocalizationManager).Assembly);
    }

    public void Initialize(ResourceManager resourceManager, CultureInfo initialCulture)
    {
        _resourceManager = resourceManager;
        SetCulture(initialCulture);
    }

    public string this[string key]
    {
        get
        {
            if (_resourceManager == null)
                return $"[{key}]";

            var value = _resourceManager.GetString(key, _currentCulture);
            return value ?? $"[{key}]";
        }
    }

    public CultureInfo CurrentCulture
    {
        get => _currentCulture;
        set => SetCulture(value);
    }

    public void SetCulture(CultureInfo culture)
    {
        if (_currentCulture.Equals(culture))
            return;

        _currentCulture = culture;
        Thread.CurrentThread.CurrentCulture = culture;
        Thread.CurrentThread.CurrentUICulture = culture;

        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(null)); // Refresh all bindings
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(string.Empty));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Item"));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Item[]"));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CurrentCulture)));
    }
}
