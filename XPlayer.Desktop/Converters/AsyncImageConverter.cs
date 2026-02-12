using System;
using System.Collections.Concurrent;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Avalonia.Data.Converters;
using Avalonia.Media.Imaging;
using Avalonia.Threading;

namespace XPlayer.Desktop.Converters;

/// <summary>
/// Converts a URL string to a Bitmap loaded asynchronously.
/// Includes an in-memory cache to avoid redundant downloads.
/// Usage in XAML: Source="{Binding ImageUrl, Converter={x:Static converters:AsyncImageConverter.Instance}}"
/// </summary>
public class AsyncImageConverter : IValueConverter
{
    public static readonly AsyncImageConverter Instance = new();

    private static readonly ConcurrentDictionary<string, Bitmap?> Cache = new();
    private static readonly HttpClient HttpClient = new()
    {
        Timeout = TimeSpan.FromSeconds(15)
    };

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not string url || string.IsNullOrWhiteSpace(url))
            return null;

        // Check cache first
        if (Cache.TryGetValue(url, out var cached))
            return cached;

        // Start async load, return null for now (will update via binding when loaded)
        LoadImageAsync(url);
        return null;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }

    private static async void LoadImageAsync(string url)
    {
        try
        {
            var bytes = await HttpClient.GetByteArrayAsync(url);
            using var stream = new MemoryStream(bytes);
            var bitmap = new Bitmap(stream);
            Cache.TryAdd(url, bitmap);
            
            // Note: The binding will not automatically update because IValueConverter
            // doesn't support notification. For a proper async image solution,
            // consider using an AsyncImageLoader or a custom attached property.
            // This converter works well when used with observable properties that
            // re-trigger the binding.
        }
        catch
        {
            Cache.TryAdd(url, null);
        }
    }

    /// <summary>
    /// Preloads an image into the cache. Returns the loaded bitmap.
    /// </summary>
    public static async Task<Bitmap?> LoadAsync(string? url)
    {
        if (string.IsNullOrWhiteSpace(url)) return null;

        if (Cache.TryGetValue(url, out var cached))
            return cached;

        try
        {
            var bytes = await HttpClient.GetByteArrayAsync(url);
            using var stream = new MemoryStream(bytes);
            var bitmap = new Bitmap(stream);
            Cache.TryAdd(url, bitmap);
            return bitmap;
        }
        catch
        {
            Cache.TryAdd(url, null);
            return null;
        }
    }

    /// <summary>
    /// Clears the image cache.
    /// </summary>
    public static void ClearCache()
    {
        Cache.Clear();
    }
}
