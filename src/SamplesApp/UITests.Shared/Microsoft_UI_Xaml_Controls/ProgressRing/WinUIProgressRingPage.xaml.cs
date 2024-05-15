using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
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
