using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace XPlayer.Desktop.Views.Dialogs;

public partial class CloseBehaviorDialogView : UserControl
{
    public CloseBehaviorDialogView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
