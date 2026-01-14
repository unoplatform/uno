using Uno.UI.Samples.Controls;
using Microsoft.UI.Xaml.Controls;

namespace Uno.UI.Samples.Content.UITests.WebView
{
	[Sample("WebView", "WebView_Mailto", Description: "This sample will open a mailto: link")]
	public sealed partial class WebView_Mailto : UserControl
	{
		public WebView_Mailto()
		{
			InitializeComponent();

#if HAS_UNO
			var html = "<a href=\"mailto:uno-platform-test@platform.uno?subject=Tests\">open mailto link</a>";
			webView.NavigateToString(html);
#endif
		}
	}
}
