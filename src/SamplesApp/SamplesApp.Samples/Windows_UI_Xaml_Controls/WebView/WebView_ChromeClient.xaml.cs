using SamplesApp.Windows_UI_Xaml_Controls.WebView;
using Uno.UI.Samples.Controls;
using Microsoft.UI.Xaml.Controls;

namespace Uno.UI.Samples.Content.UITests.WebView
{
	[Sample("WebView", Name = "WebView_ChromeClient", ViewModelType = typeof(WebViewStaticViewModel))]
	public sealed partial class WebView_ChromeClient : UserControl
	{
		public WebView_ChromeClient()
		{
			this.InitializeComponent();
		}
	}
}
