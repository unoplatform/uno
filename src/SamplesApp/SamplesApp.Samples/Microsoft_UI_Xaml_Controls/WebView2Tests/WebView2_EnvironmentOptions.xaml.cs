using Microsoft.UI.Xaml.Controls;
using Uno.UI.Samples.Controls;

namespace SamplesApp.Microsoft_UI_Xaml_Controls.WebView2Tests
{
	[Sample("WebView", Name = "WebView2_EnvironmentOptions", Description = "Demonstrates the Windows-only Uno.UI.FeatureConfiguration.WebView2 environment options (AllowSingleSignOnUsingOSPrimaryAccount and AdditionalBrowserArguments), which must be set at application startup.")]
	public sealed partial class WebView2_EnvironmentOptions : Page
	{
		public WebView2_EnvironmentOptions()
		{
			this.InitializeComponent();
			SsoStatusText.Text = $"FeatureConfiguration.WebView2.AllowSingleSignOnUsingOSPrimaryAccount = {Uno.UI.FeatureConfiguration.WebView2.AllowSingleSignOnUsingOSPrimaryAccount}";
			ArgsStatusText.Text = $"FeatureConfiguration.WebView2.AdditionalBrowserArguments = {Uno.UI.FeatureConfiguration.WebView2.AdditionalBrowserArguments ?? "(null)"}";
		}
	}
}
