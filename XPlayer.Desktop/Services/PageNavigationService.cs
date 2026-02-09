using System;
using XPlayer.Desktop.Models;

namespace XPlayer.Desktop.Services;

public class PageNavigationService
{
    public Action<Type>? NavigationRequested { get; set; }

    public void RequestNavigation<T>() where T : Page
    {
        NavigationRequested?.Invoke(typeof(T));
    }
}