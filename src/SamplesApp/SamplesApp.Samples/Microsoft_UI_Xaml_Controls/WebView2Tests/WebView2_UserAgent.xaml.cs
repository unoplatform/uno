using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace UITests.Microsoft_UI_Xaml_Controls.WebView2Tests
{
	[Uno.UI.Samples.Controls.Sample("WebView", IsManualTest = true, IgnoreInSnapshotTests = true,
		Description = "Sets a custom User-Agent via CoreWebView2Settings.UserAgent and reads it back via navigator.userAgent.")]
	public sealed partial class WebView2_UserAgent : Page
	{
		public WebView2_UserAgent()
		{
			this.InitializeComponent();
			this.Loaded += OnLoaded;
		}

		private async void OnLoaded(object sender, RoutedEventArgs e)
		{
			await WebView.EnsureCoreWebView2Async();
			UpdateStatus();
		}

		private void OnApplyClick(object sender, RoutedEventArgs e)
		{
			if (WebView.CoreWebView2 is null)
			{
				StatusText.Text = "CoreWebView2 not initialized yet.";
				return;
			}

			try
			{
				WebView.CoreWebView2.Settings.UserAgent = UserAgentInput.Text;
				WebView.CoreWebView2.Reload();
				UpdateStatus();
			}
			catch (Exception ex)
			{
				StatusText.Text = $"Failed to apply UserAgent: {ex.Message}";
			}
		}

		private void OnResetClick(object sender, RoutedEventArgs e)
		{
			if (WebView.CoreWebView2 is null)
			{
				return;
			}

			WebView.CoreWebView2.Settings.UserAgent = null;
			WebView.CoreWebView2.Reload();
			UpdateStatus();
		}

		private async void OnShowClick(object sender, RoutedEventArgs e)
		{
			if (WebView.CoreWebView2 is null)
			{
				return;
			}

			try
			{
				var result = await WebView.CoreWebView2.ExecuteScriptAsync("navigator.userAgent");
				StatusText.Text = $"navigator.userAgent = {result}";
			}
			catch (Exception ex)
			{
				StatusText.Text = $"ExecuteScriptAsync failed: {ex.Message}";
			}
		}

		private void UpdateStatus()
		{
			if (WebView.CoreWebView2 is null)
			{
				StatusText.Text = "(not initialized)";
				return;
			}

			var configured = WebView.CoreWebView2.Settings.UserAgent;
			StatusText.Text = configured is null
				? "Settings.UserAgent = (default — platform user-agent)"
				: $"Settings.UserAgent = {configured}";
		}
	}
}
