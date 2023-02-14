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
	public sealed partial class RefreshContainerNavigationFirstPage : Page
	{
		public RefreshContainerNavigationFirstPage()
		{
			this.InitializeComponent();

			for (int i = 0; i < 40; i++)
			{
				var textBlock = new TextBlock()
				{
					Text = "Hello " + i,
					FontSize = 40,
					Margin = ThicknessHelper.FromUniformLength(20)
				};
				StackParent.Children.Add(textBlock);
			}
		}

		private void ButtonClick(object sender, RoutedEventArgs e)
		{
			Frame.Navigate(typeof(RefreshContainerNavigationSecondPage));
		}
	}
}
