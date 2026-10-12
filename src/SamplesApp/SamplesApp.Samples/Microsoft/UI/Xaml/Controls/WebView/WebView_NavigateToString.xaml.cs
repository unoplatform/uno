using SamplesApp.Windows_UI_Xaml_Controls.WebView;
using Uno.UI.Samples.Controls;
using Microsoft.UI.Xaml.Controls;

namespace Uno.UI.Samples.Content.UITests.WebView
{
	[Sample("WebView", Name = "WebView_NavigateToString", ViewModelType = typeof(WebViewViewModel), Description = "WebView demonstrating NavigateToString method")]
	public sealed partial class WebView_NavigateToString : UserControl
	{
		public WebView_NavigateToString()
		{
			this.InitializeComponent();
		}
	}
}
