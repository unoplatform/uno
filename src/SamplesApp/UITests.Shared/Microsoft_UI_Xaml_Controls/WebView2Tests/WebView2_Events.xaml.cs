using SamplesApp.Microsoft_UI_Xaml_Controls.WebView2Tests;
using Uno.UI.Samples.Controls;
using Microsoft.UI.Xaml.Controls;

namespace SamplesApp.Microsoft_UI_Xaml_Controls.WebView2Tests
{
	[Sample("WebView", ViewModelType = typeof(WebView2ViewModel), IgnoreInSnapshotTests = true /*WebView2 may or may not have fully loaded*/)]
	public sealed partial class WebView2_Events : UserControl
	{
		public WebView2_Events()
		{
			this.InitializeComponent();
		}
	}
}
