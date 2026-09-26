using Microsoft.UI.Xaml.Controls;

namespace UnoLibrary2;

public sealed partial class PlatformMarkerControl : UserControl
{
    public PlatformMarkerControl()
    {
        this.InitializeComponent();
    }

    public string XamlMarkers => string.Join(",", Markers.Children.OfType<TextBlock>().Select(t => t.Text));
}
