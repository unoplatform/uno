using System;
using Uno.UI.Samples.Controls;
using Windows.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using RefreshVisualizer = Microsoft.UI.Xaml.Controls.RefreshVisualizer;
using RefreshVisualizerState = Microsoft.UI.Xaml.Controls.RefreshVisualizerState;
using RefreshRequestedEventArgs = Microsoft.UI.Xaml.Controls.RefreshRequestedEventArgs;
using RefreshInteractionRatioChangedEventArgs = Microsoft.UI.Xaml.Controls.RefreshInteractionRatioChangedEventArgs;
using RefreshStateChangedEventArgs = Microsoft.UI.Xaml.Controls.RefreshStateChangedEventArgs;
using RefreshPullDirection = Microsoft.UI.Xaml.Controls.RefreshPullDirection;
using System.Threading.Tasks;

namespace UITests.Microsoft_UI_Xaml_Controls.RefreshContainerTests
{
	[Sample("PullToRefresh")]
	public sealed partial class RefreshContainerTheming : Page
	{
		private readonly Random _randomizer = new Random();

		public RefreshContainerTheming()
		{
			this.InitializeComponent();
			this.RefreshContainer.RefreshRequested += RefreshContainer_RefreshRequested;
		}

		private void ChangeColors_Click(object sender, RoutedEventArgs args)
		{
			var foregroundColor = Color.FromArgb((byte)_randomizer.Next(150, 256), (byte)_randomizer.Next(0, 256), (byte)_randomizer.Next(0, 256), (byte)_randomizer.Next(0, 256));
			var backgroundColor = Color.FromArgb((byte)_randomizer.Next(150, 256), (byte)_randomizer.Next(0, 256), (byte)_randomizer.Next(0, 256), (byte)_randomizer.Next(0, 256));

			this.RefreshContainer.Visualizer.Foreground = new SolidColorBrush(foregroundColor);
			this.RefreshContainer.Visualizer.Background = new SolidColorBrush(backgroundColor);
		}

		private void RequestRefresh_Click(object sender, RoutedEventArgs args)
		{
			this.RefreshContainer.RequestRefresh();
		}

		private async void RefreshContainer_RefreshRequested(object sender, RefreshRequestedEventArgs e)
		{
			var deferral = e.GetDeferral();
			await Task.Delay(2000);
			deferral.Complete();
		}
	}
}
