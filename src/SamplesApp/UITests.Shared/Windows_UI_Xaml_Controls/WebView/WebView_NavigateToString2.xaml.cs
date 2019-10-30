  
using SamplesApp.Windows_UI_Xaml_Controls.WebView;
using Uno.UI.Samples.Controls;
using Windows.UI.Xaml.Controls;

namespace Uno.UI.Samples.Content.UITests.WebView
{
	[SampleControlInfo("WebView", "WebView_NavigateToString2", typeof(WebViewViewModel), description: "WebView testing NavigateToString method with long string")]
	public sealed partial class WebView_NavigateToString : UserControl
	{
		public WebView_NavigateToString()
		{
			this.InitializeComponent();
		}
    
 		private async void generateLong_Click(object sender, object e)
		{
			int linesCount = 0;
			if(!int.TryParse(WebView_NavigateToStringSize.Text, out linesCount))
			{
				return;
			}

			string longString;
			longString = "<html><body>";
			for (int i = 0; i < linesCount; i++)
			{
				string line = "Linia " + i.ToString() + " ";
				line = line.PadRight(1000, 'x');
				longString = longString + "<p>" + line;
			}
			longString += "</body></html>";
			webViewControl.NavigateToString(longString);
		}

	}
}
