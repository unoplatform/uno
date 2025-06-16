using Microsoft.UI.Xaml.Controls;
using Uno.UI.Samples.Controls;
using Microsoft.UI.Xaml;
using System;

namespace UITests.Windows_UI_Xaml_Controls.WebView
{
	[SampleControlInfo("WebView", IgnoreInSnapshotTests = true)]
	public sealed partial class WebView_Title : Page
	{
		public WebView_Title()
		{
			this.InitializeComponent();
		}

		public void OnGoClicked(object sender, RoutedEventArgs e)
		{
			if (Uri.TryCreate(UriInput.Text, UriKind.Absolute, out var uri))
			{
				Web.Navigate(uri);
			}
			else
			{
				throw new ArgumentException("The provided URL is not valid.", nameof(UriInput));
			}
		}
	}
}
