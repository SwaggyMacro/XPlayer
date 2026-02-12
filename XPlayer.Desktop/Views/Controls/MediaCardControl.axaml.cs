using Avalonia;
using Avalonia.Controls.Primitives;

namespace XPlayer.Desktop.Views.Controls;

/// <summary>
/// Reusable media card control displaying a poster image,
/// title, subtitle, and optional progress bar.
/// </summary>
public class MediaCardControl : TemplatedControl
{
    public static readonly StyledProperty<string?> ImageUrlProperty =
        AvaloniaProperty.Register<MediaCardControl, string?>(nameof(ImageUrl));

    public static readonly StyledProperty<string?> TitleProperty =
        AvaloniaProperty.Register<MediaCardControl, string?>(nameof(Title));

    public static readonly StyledProperty<string?> SubtitleProperty =
        AvaloniaProperty.Register<MediaCardControl, string?>(nameof(Subtitle));

    public static readonly StyledProperty<double> ProgressProperty =
        AvaloniaProperty.Register<MediaCardControl, double>(nameof(Progress));

    public static readonly StyledProperty<bool> ShowProgressProperty =
        AvaloniaProperty.Register<MediaCardControl, bool>(nameof(ShowProgress));
    
    public static readonly StyledProperty<double> CardWidthProperty =
        AvaloniaProperty.Register<MediaCardControl, double>(nameof(CardWidth), 160);
    
    public static readonly StyledProperty<double> CardHeightProperty =
        AvaloniaProperty.Register<MediaCardControl, double>(nameof(CardHeight), 240);

    public string? ImageUrl
    {
        get => GetValue(ImageUrlProperty);
        set => SetValue(ImageUrlProperty, value);
    }

    public string? Title
    {
        get => GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public string? Subtitle
    {
        get => GetValue(SubtitleProperty);
        set => SetValue(SubtitleProperty, value);
    }

    public double Progress
    {
        get => GetValue(ProgressProperty);
        set => SetValue(ProgressProperty, value);
    }

    public bool ShowProgress
    {
        get => GetValue(ShowProgressProperty);
        set => SetValue(ShowProgressProperty, value);
    }
    
    public double CardWidth
    {
        get => GetValue(CardWidthProperty);
        set => SetValue(CardWidthProperty, value);
    }
    
    public double CardHeight
    {
        get => GetValue(CardHeightProperty);
        set => SetValue(CardHeightProperty, value);
    }
}
