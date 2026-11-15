using Microsoft.UI.Xaml.Controls;
using Uno.UI.Samples.Controls;

namespace UITests.Windows_UI_Xaml_Controls.WebView;

[Sample("WebView", IsManualTest = true, IgnoreInSnapshotTests = true,
	Description = "WebView2 swallows arrow keys - Click WebView, then TextBox, and verify arrow keys work (issue #22819)")]
public sealed partial class WebView2_ArrowKeys : Page
{
	public WebView2_ArrowKeys()
	{
		this.InitializeComponent();
	}
}
