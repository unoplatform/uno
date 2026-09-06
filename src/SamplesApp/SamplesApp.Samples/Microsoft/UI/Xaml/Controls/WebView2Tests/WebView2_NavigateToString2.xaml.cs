
using SamplesApp.Microsoft_UI_Xaml_Controls.WebView2Tests;
using Uno.UI.Samples.Controls;
using Microsoft.UI.Xaml.Controls;

namespace UITests.Shared.Microsoft_UI_Xaml_Controls.WebView2Tests
{
	[Sample("WebView", Name = "WebView2_NavigateToString2", Description = "Testing a NavigateToString with a very long string")]
	public sealed partial class WebView2_NavigateToString2 : UserControl
	{
		public WebView2_NavigateToString2()
		{
			this.InitializeComponent();
		}

		string longString = "";

		private void generateLong_Click(object sender, object e)
		{

			if (longString.Length < 10)
			{
				// generate string
				WebView2_NavigateToStringResult.Text = "generating string";


				int linesCount = 0;
				if (!int.TryParse(WebView2_NavigateToStringSize.Text, out linesCount))
				{
					return;
				}

				longString = "<html><body>";
				for (int i = 0; i < linesCount; i++)
				{
					string line = "Linia " + i.ToString() + " ";
					line = line.PadRight(1000, 'x');
					longString = longString + "<p>" + line;
					WebView2_NavigateToStringResult.Text = ((int)(i * 100 / linesCount)).ToString();  // percentage, as generating string takes loooong
				}
				longString += "</body></html>";

				WebView2_NavigateToStringResult.Text = "string ready";
				startButton.Content = "NavigateTo";
			}
			else
			{
				// NavigateTo
				WebView2_NavigateToStringResult.Text = "waiting for NavigationCompleted";
				webViewControl.NavigationCompleted += webViewControl_NavigationCompleted;
				webViewControl.NavigateToString(longString);
			}
		}
		private void webViewControl_NavigationCompleted(object sender, object args)
		{
			WebView2_NavigateToStringResult.Text = "success";
		}
	}
}
