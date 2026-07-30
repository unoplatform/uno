using Microsoft.UI.Xaml.Controls;
using Uno.UI.Samples.Controls;

namespace SamplesApp.Microsoft_UI_Xaml_Controls.WebView2Tests
{
	[Sample("WebView", Name = "WebView2_EnableDevTools", Description = "Demonstrates Uno.UI.FeatureConfiguration.WebView2.EnableDevTools, which toggles the platform-native developer tools on the underlying web engine.")]
	public sealed partial class WebView2_EnableDevTools : Page
	{
		public WebView2_EnableDevTools()
		{
			this.InitializeComponent();
			StatusText.Text = $"FeatureConfiguration.WebView2.EnableDevTools = {Uno.UI.FeatureConfiguration.WebView2.EnableDevTools}";
		}
	}
}
