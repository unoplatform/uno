using Uno.UI.Samples.Controls;
using Windows.UI.ViewManagement;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace UITests.Shared.Windows_UI_ViewManagement
{
	[Sample("Windows.UI.ViewManagement", "FullScreenMode", Description: "Showcases entering/exiting full screen mode.")]
	public sealed partial class FullScreenModeTests : UserControl
	{
		public FullScreenModeTests()
		{
			this.InitializeComponent();
		}

		public void EnterFullScreen_Click(object sender, RoutedEventArgs e)
		{
			ApplicationView.GetForCurrentView().TryEnterFullScreenMode();
		}

		public void ExitFullScreen_Click(object sender, RoutedEventArgs e)
		{
			ApplicationView.GetForCurrentView().ExitFullScreenMode();
		}
	}
}
