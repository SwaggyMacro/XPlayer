using Avalonia.Data;
using Avalonia.Markup.Xaml;
using System;
using System.ComponentModel;
using System.Reflection;

namespace XPlayer.Lang;

public class LocalizeExtension : MarkupExtension
{
    public string Key { get; set; }

    public LocalizeExtension(string key)
    {
        Key = key;
    }

    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        var binding = new Binding
        {
            Source = LocalizationManager.Instance,
            Path = $"[{Key}]",
            Mode = BindingMode.OneWay
        };

        return binding;
    }
}
