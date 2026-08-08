using System;
using System.IO;
using System.Runtime.InteropServices.WindowsRuntime;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Web.WebView2.Core;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.Storage.Streams;

namespace UITests.Microsoft_UI_Xaml_Controls.WebView2Tests
{
	[Uno.UI.Samples.Controls.Sample("WebView", IsManualTest = true, IgnoreInSnapshotTests = true,
		Description = "Exports the page to PDF via PrintToPdfStreamAsync or opens the system print UI.")]
	public sealed partial class WebView2_Print : Page
	{
		public WebView2_Print()
		{
			this.InitializeComponent();
			this.Loaded += async (_, _) => await WebView.EnsureCoreWebView2Async();
		}

		private async void OnPrintPdfClick(object sender, RoutedEventArgs e)
		{
			if (WebView.CoreWebView2 is null)
			{
				return;
			}

			try
			{
				StatusText.Text = "Generating PDF...";
				var settings = WebView.CoreWebView2.Environment.CreatePrintSettings();
				settings.Orientation = CoreWebView2PrintOrientation.Portrait;
				settings.ScaleFactor = 1.0;
				using var pdf = await WebView.CoreWebView2.PrintToPdfStreamAsync(settings);
				using var input = pdf.AsStreamForRead();

				var folder = ApplicationData.Current.TemporaryFolder;
				var file = await folder.CreateFileAsync($"uno-webview-{DateTime.Now:yyyyMMddHHmmss}.pdf", CreationCollisionOption.ReplaceExisting);
				using (var output = await file.OpenStreamForWriteAsync())
				{
					await input.CopyToAsync(output);
				}

				StatusText.Text = $"Saved {file.Path}";
			}
			catch (Exception ex)
			{
				StatusText.Text = $"PrintToPdf failed: {ex.Message}";
			}
		}

		private void OnShowPrintClick(object sender, RoutedEventArgs e)
		{
			if (WebView.CoreWebView2 is null)
			{
				return;
			}

			try
			{
				WebView.CoreWebView2.ShowPrintUI(CoreWebView2PrintDialogKind.System);
				StatusText.Text = "Print UI opened.";
			}
			catch (Exception ex)
			{
				StatusText.Text = $"ShowPrintUI failed: {ex.Message}";
			}
		}
	}
}
