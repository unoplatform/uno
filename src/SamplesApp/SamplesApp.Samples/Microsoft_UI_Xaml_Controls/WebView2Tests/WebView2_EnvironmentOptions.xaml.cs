using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Web.WebView2.Core;

namespace UITests.Microsoft_UI_Xaml_Controls.WebView2Tests
{
	[Uno.UI.Samples.Controls.Sample("WebView", IsManualTest = true, IgnoreInSnapshotTests = true,
		Description = "Wires CoreWebView2Environment + CoreWebView2ControllerOptions through EnsureCoreWebView2Async.")]
	public sealed partial class WebView2_EnvironmentOptions : Page
	{
		public WebView2_EnvironmentOptions()
		{
			this.InitializeComponent();
		}

		private async void OnApplyClick(object sender, RoutedEventArgs e)
		{
			try
			{
				var envOptions = new CoreWebView2EnvironmentOptions
				{
					Language = string.IsNullOrEmpty(LanguageInput.Text) ? null : LanguageInput.Text,
					AdditionalBrowserArguments = string.IsNullOrEmpty(ArgsInput.Text) ? null : ArgsInput.Text,
					AllowSingleSignOnUsingOSPrimaryAccount = SsoToggle.IsChecked == true,
				};
				var userData = string.IsNullOrEmpty(UserDataInput.Text) ? null : UserDataInput.Text;

				var env = await CoreWebView2Environment.CreateWithOptionsAsync(
					browserExecutableFolder: null,
					userDataFolder: userData,
					options: envOptions);

				var controllerOptions = env.CreateCoreWebView2ControllerOptions();
				controllerOptions.IsInPrivateModeEnabled = InPrivateToggle.IsChecked == true;
				controllerOptions.ProfileName = string.IsNullOrEmpty(ProfileInput.Text) ? null : ProfileInput.Text;

				var webView = new WebView2();
				var initialization = webView.EnsureCoreWebView2Async(env, controllerOptions);
				WebViewHost.Content = webView;
				await initialization;
				webView.Source = new Uri("https://www.bing.com");
				StatusText.Text = $"OK (browser={env.BrowserVersionString})";
			}
			catch (Exception ex)
			{
				StatusText.Text = $"Failed: {ex.Message}";
			}
		}
	}
}
