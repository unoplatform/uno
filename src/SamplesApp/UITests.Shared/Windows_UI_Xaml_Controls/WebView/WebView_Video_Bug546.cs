using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Uno.UI.Samples.Controls;

namespace UITests.Windows_UI_Xaml_Controls.WebView
{
	[Sample("WebView", IgnoreInSnapshotTests = true)]
	public sealed partial class WebView_Video_Bug546 : Page
	{
		public WebView_Video_Bug546()
		{
			this.InitializeComponent();
		}

		private void ButtonBase_OnClick(object sender, RoutedEventArgs e)
		{
			webview.Navigate(new Uri(addr.Text));
		}
	}
}
