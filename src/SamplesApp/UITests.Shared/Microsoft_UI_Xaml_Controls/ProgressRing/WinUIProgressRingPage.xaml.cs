using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Uno.UI.Samples.Controls;

namespace UITests.Microsoft_UI_Xaml_Controls.ProgressRing
{
	[Sample("Progress", "MUX", IgnoreInSnapshotTests = true)]
	public sealed partial class WinUIProgressRingPage : Page
	{
		public WinUIProgressRingPage()
		{
			this.InitializeComponent();
		}

		private UIElement _loadUnloadProgressBar = new Microsoft/* UWP don't rename */.UI.Xaml.Controls.ProgressRing();

		private void LoadUnload(object sender, RoutedEventArgs e)
		{
			var isLoaded = (sender as ToggleButton)?.IsChecked ?? false;

			container.Child = isLoaded ? _loadUnloadProgressBar : null;
		}
	}
}
