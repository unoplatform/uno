using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace UITests.Microsoft_UI_Xaml_Controls.WebView2Tests
{
	[Uno.UI.Samples.Controls.Sample("WebView", IsManualTest = true, IgnoreInSnapshotTests = true,
		Description = "Sets a custom User-Agent via CoreWebView2Settings.UserAgent and reads it back via navigator.userAgent.")]
	public sealed partial class WebView2_UserAgent : Page
	{
		private string _defaultUserAgent;

		public WebView2_UserAgent()
		{
			this.InitializeComponent();
			this.Loaded += OnLoaded;
		}

		private async void OnLoaded(object sender, RoutedEventArgs e)
		{
			try
			{
				await WebView.EnsureCoreWebView2Async();
				_defaultUserAgent ??= WebView.CoreWebView2?.Settings.UserAgent;
			}
			catch (Exception ex)
			{
				// async void: an unobserved exception would tear the app down.
				StatusText.Text = $"Initialization failed: {ex.Message}";
				return;
			}

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

			if (string.IsNullOrEmpty(_defaultUserAgent))
			{
				StatusText.Text = "This platform does not expose its default user-agent through Settings.UserAgent.";
				return;
			}
			try
			{
				WebView.CoreWebView2.Settings.UserAgent = _defaultUserAgent;
				UserAgentInput.Text = _defaultUserAgent;
				WebView.Reload();
				UpdateStatus();
			}
			catch (Exception ex)
			{
				StatusText.Text = $"Failed to restore UserAgent: {ex.Message}";
			}
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
