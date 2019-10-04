using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Uno.UI.Sample.Views.Helper;
using Uno.UI.Samples.Controls;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace UITests.Shared.Windows_UI_Xaml.Clip
{

	[SampleControlInfo("Clip", nameof(DoubleAnimationClipping), description: "[Android]When going to `Ready` state the WebView expand to full screen and cover other control when it shouldn't be.")]
	public sealed partial class DoubleAnimationClipping : UserControl
	{
		private static readonly Random random = new Random();

		public DoubleAnimationClipping()
		{
			this.InitializeComponent();
		}

		private void GotoReadyState(object sender, RoutedEventArgs e)
		{
			//var rootGrid = MainContent.FindFirstChild<Grid>(x => x.Name == "RootGrid");
			var webView = MainContent.FindFirstChild<WebView>(x => x.Name == "WebView");

			var colors = "Blue,Pink,Yellow,Lime".Split(',');
			var color = colors[random.Next(colors.Length)];

			webView.NavigateToString($@"
				<html>
					<body style='background: {color};' />
				</html>
			");
			VisualStateManager.GoToState(MainContent, "Ready", true);
		}

		private void GotoNotReadyState(object sender, RoutedEventArgs e) => VisualStateManager.GoToState(MainContent, "NotReady", true);
	}
}
