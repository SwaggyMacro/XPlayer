using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace XPlayer.Desktop.Views.Dialogs;

public partial class MediaSourceDialogView : UserControl
{
    public MediaSourceDialogView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
