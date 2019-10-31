  
using SamplesApp.Windows_UI_Xaml_Controls.WebView;
using Uno.UI.Samples.Controls;
using Windows.UI.Xaml.Controls;

namespace UITests.Shared.Windows_UI_Xaml_Controls.WebView
{
	[SampleControlInfo("WebView", "WebView_NavigateToString2", typeof(WebViewViewModel), description: "WebView testing NavigateToString method with long string")]
	public sealed partial class WebView_NavigateToString2 : UserControl
	{
		public WebView_NavigateToString2()
		{
			this.InitializeComponent();
		}
    
 		private async void generateLong_Click(object sender, object e)
		{
			WebView_NavigateToStringResult.Text = "waiting for NavigationCompleted";

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
				WebView_NavigateToStringResult.Text = ((int)(i * 100 / linesCount)).ToString();  // percentage, as generating string takes loooong
			}
			longString += "</body></html>";
			webViewControl.NavigationCompleted += webViewControl_NavigationCompleted;
			webViewControl.NavigateToString(longString);
		}

        private void webViewControl_NavigationCompleted(object sender, object args)
        {
            WebView_NavigateToStringResult.Text = "success";
        }

	}
}
