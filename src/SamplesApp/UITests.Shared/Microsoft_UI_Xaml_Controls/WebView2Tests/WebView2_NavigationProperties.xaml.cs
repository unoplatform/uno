using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
#if !HAS_UNO_WINUI
using Microsoft/* UWP don't rename */.UI.Xaml.Controls;
#endif

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace UITests.Microsoft_UI_Xaml_Controls.WebView2Tests
{
	/// <summary>
	/// An empty page that can be used on its own or navigated to within a Frame.
	/// </summary>
	[Uno.UI.Samples.Controls.Sample("WebView")]
	public sealed partial class WebView2_NavigationProperties : Page
	{
		public WebView2_NavigationProperties()
		{
			this.InitializeComponent();
			this.Loaded += OnLoaded;
		}

		private async void OnLoaded(object sender, RoutedEventArgs e)
		{
			await TestWebView.EnsureCoreWebView2Async();
			TestWebView.CoreWebView2.Navigate("https://nventive.com/en");
			TestWebView.CoreWebView2.HistoryChanged += CoreWebView2_HistoryChanged;
		}

		private void CoreWebView2_HistoryChanged(Microsoft.Web.WebView2.Core.CoreWebView2 sender, object args)
		{
			LastHistoryChangeRun.Text = DateTimeOffset.Now.TimeOfDay.ToString();
			CanGoBackCheckBox.IsChecked = sender.CanGoBack;
			CanGoForwardCheckBox.IsChecked = sender.CanGoForward;
		}

		private void GoForward_Click(object sender, RoutedEventArgs e)
		{
			TestWebView.GoForward();
		}

		private void GoBack_Click(object sender, RoutedEventArgs e)
		{
			TestWebView.GoBack();
		}
	}
}
