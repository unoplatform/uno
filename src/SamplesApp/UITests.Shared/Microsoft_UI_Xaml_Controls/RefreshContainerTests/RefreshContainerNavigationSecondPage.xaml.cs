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
	public sealed partial class RefreshContainerNavigationSecondPage : Page
	{
		public RefreshContainerNavigationSecondPage()
		{
			this.InitializeComponent();
		}

		private void ButtonClick(object sender, RoutedEventArgs e)
		{
			Frame.GoBack();
		}
	}
}
