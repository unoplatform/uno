using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace UITests.Windows_UI_Xaml_Media_Animation.Showcase;

public sealed partial class VisualStateBox : UserControl
{
	public VisualStateBox()
	{
		this.InitializeComponent();
	}

	private void GoIdle(object sender, RoutedEventArgs e) => VisualStateManager.GoToState(this, "Idle", true);
	private void GoPulse(object sender, RoutedEventArgs e) => VisualStateManager.GoToState(this, "Pulse", true);
	private void GoHighlight(object sender, RoutedEventArgs e) => VisualStateManager.GoToState(this, "Highlight", true);
}
