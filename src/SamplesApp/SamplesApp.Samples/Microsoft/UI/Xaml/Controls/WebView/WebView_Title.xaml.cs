using Microsoft.UI.Xaml.Controls;
using Uno.UI.Samples.Controls;
using Microsoft.UI.Xaml;
using System;

namespace UITests.Windows_UI_Xaml_Controls.WebView
{
	[Sample("WebView", IgnoreInSnapshotTests = true)]
	public sealed partial class WebView_Title : Page
	{
		public WebView_Title()
		{
			this.InitializeComponent();
		}

		public void OnGoClicked(object sender, RoutedEventArgs e)
		{
			var uri = new Uri(UriInput.Text, UriKind.Absolute);
			Web.Navigate(uri);
		}
	}
}
