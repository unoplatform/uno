using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace UITests.Microsoft_UI_Xaml_Controls.WebView2Tests
{
	/// <summary>
	/// An empty page that can be used on its own or navigated to within a Frame.
	/// </summary>
	[Uno.UI.Samples.Controls.Sample(
		"WebView",
		IsManualTest = true,
		Description =
			"You should be able to interact with the website (click links, scroll), as well as click the button in the center." +
			"Each click should increment it exactly once. Conversely, clicking the button should not trigger any hyperlink that is currently below it.")]
	public sealed partial class WebView2_ManagedOverlay : Page
	{
		public WebView2_ManagedOverlay()
		{
			this.InitializeComponent();
		}

		private void OnTestClick(object sender, RoutedEventArgs e)
		{
			var btn = (Button)sender;
			var text = btn.Content.ToString();
			if (int.TryParse(text, out var value))
			{
				btn.Content = (value + 1).ToString();
			}
			else
			{
				btn.Content = "0";
			}
		}
	}
}
