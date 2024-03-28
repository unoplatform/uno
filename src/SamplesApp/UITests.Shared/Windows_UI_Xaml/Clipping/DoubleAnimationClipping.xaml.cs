using System;
using Windows.UI;
using Uno.UI.Extensions;
using Uno.UI.Samples.Controls;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Shapes;

namespace SamplesApp.Windows_UI_Xaml.Clipping
{
	[SampleControlInfo(category: "Clipping", description: "[Android]When going to `Ready` state the WebView expand to full screen and cover other control when it shouldn't be.")]
	public sealed partial class DoubleAnimationClipping : UserControl
	{
		private static readonly Random random = new Random();

		public DoubleAnimationClipping()
		{
			this.InitializeComponent();
		}

		private void GotoReadyState(object sender, RoutedEventArgs e)
		{
			var sut = MainContent.FindFirstChild<Rectangle>(x => x.Name == "SUT");

			var colors = new[] { Colors.Blue, Colors.Pink, Colors.Yellow, Colors.Lime };
			var color = colors[random.Next(colors.Length)];

			sut.Fill = new SolidColorBrush(color);

			VisualStateManager.GoToState(MainContent, "Ready", true);
		}

		private void GotoNotReadyState(object sender, RoutedEventArgs e) => VisualStateManager.GoToState(MainContent, "NotReady", true);
	}
}
