using SamplesApp.Windows_UI_Xaml_Controls.WebView;
using Uno.UI.Samples.Controls;
using Windows.UI.Xaml.Controls;

namespace Uno.UI.Samples.Content.UITests.WebView
{
	[SampleControlInfo("WebView", "WebView_Static", typeof(WebViewStaticViewModel), description: "Simple WebView navigation using Source property")]
	public sealed partial class WebView_Static : UserControl
	{
		public WebView_Static()
		{
			this.InitializeComponent();
		}
	}
}
