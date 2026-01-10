using SamplesApp.Microsoft_UI_Xaml_Controls.WebView2Tests;
using Uno.UI.Samples.Controls;
using Microsoft.UI.Xaml.Controls;

namespace SamplesApp.Microsoft_UI_Xaml_Controls.WebView2Tests
{
	[Sample("WebView", "WebView2_Static", typeof(WebView2StaticViewModel), description: "Simple WebView2 navigation using Source property")]
	public sealed partial class WebView2_Static : UserControl
	{
		public WebView2_Static()
		{
			this.InitializeComponent();
		}
	}
}
