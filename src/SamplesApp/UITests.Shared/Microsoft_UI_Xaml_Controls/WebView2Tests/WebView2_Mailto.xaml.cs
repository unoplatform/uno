using Uno.UI.Samples.Controls;
using Microsoft.UI.Xaml.Controls;

namespace SamplesApp.Microsoft_UI_Xaml_Controls.WebView2Tests
{
	[Sample("WebView", "WebView2_Mailto", description: "This sample will open a mailto: link")]
	public sealed partial class WebView2_Mailto : UserControl
	{
		public WebView2_Mailto()
		{
			InitializeComponent();

			var html = "<a href=\"mailto:uno-platform-test@platform.uno?subject=Tests\">open mailto link</a>";
			webView.NavigateToString(html);
		}
	}
}
