using System;
using Uno.UI.Samples.Controls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace SamplesApp.Microsoft_UI_Xaml_Controls.WebView2Tests
{
	[Sample("WebView", Name = "WebView2_ContentDialogFocus",
		Description = "WebView2 hosted in a ContentDialog on Win32 Skia Desktop; the text input should focus, type, and show the I-beam cursor.")]
	public sealed partial class WebView2_ContentDialogFocus : UserControl
	{
		private const string ReproHtml = """
			<html>
			<body style="font-family:sans-serif;background:#f5f5f5;margin:0;padding:24px;">
			  <h2>Simulated SSO WebView2</h2>
			  <p>Click into the field below and start typing.</p>
			  <input id="inp" autofocus style="font-size:24px;width:90%;padding:8px;" placeholder="Type here..." />
			  <p id="status" style="font-size:20px;color:#555;">status: (waiting for focus/keydown)</p>
			  <script>
			    const inp = document.getElementById('inp');
			    const status = document.getElementById('status');
			    inp.addEventListener('focus', () => { status.textContent = 'status: FOCUSED'; document.body.style.background = '#d4f7d4'; });
			    inp.addEventListener('blur', () => { status.textContent = 'status: blurred'; document.body.style.background = '#f5f5f5'; });
			    inp.addEventListener('keydown', (e) => { status.textContent = 'status: key received -> ' + e.key; });
			  </script>
			</body>
			</html>
			""";

		public WebView2_ContentDialogFocus()
		{
			this.InitializeComponent();
		}

		private async void OpenDialogButton_Click(object sender, RoutedEventArgs e)
		{
			var webView = new WebView2 { MinWidth = 500, MinHeight = 300 };

			var webViewHost = new Border
			{
				BorderThickness = new Thickness(2),
				BorderBrush = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Red),
				MinWidth = 500,
				MinHeight = 300,
				Child = webView
			};

			var dialog = new ContentDialog
			{
				Title = "Sign in (simulated SSO modal)",
				PrimaryButtonText = "Close",
				DefaultButton = ContentDialogButton.Primary,
				XamlRoot = this.XamlRoot,
				Content = webViewHost
			};

			var dataUri = "data:text/html," + Uri.EscapeDataString(ReproHtml);
			webView.Source = new Uri(dataUri);

			await dialog.ShowAsync();
		}
	}
}
