using System;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Uno.UI.Samples.Controls;
using Windows.Data.Pdf;
using Windows.Storage.Pickers;
using Windows.Storage.Streams;
using Windows.Storage;
using Windows.UI.Xaml.Media.Imaging;

namespace UITests.Shared.Windows_Data_Pdf
{
	[SampleControlInfo("Windows.Data.Pdf", "PdfDocument", description: "Demonstrates use of Windows.Data.Pdf.PdfDocument", ignoreInSnapshotTests: true)]
	public sealed partial class PdfDocumentRenderTest : UserControl
	{
		const int WrongPassword = unchecked((int)0x8007052b); // HRESULT_FROM_WIN32(ERROR_WRONG_PASSWORD)
		const int GenericFail = unchecked((int)0x80004005);   // E_FAIL
		private PdfDocument pdfDocument;

		public PdfDocumentRenderTest()
		{
			this.InitializeComponent();
		}

		private async void LoadDocument(object sender, RoutedEventArgs args)
		{
			LoadButton.IsEnabled = false;

			pdfDocument = null;
			Output.Source = null;
			PageNumberBox.Text = "1";
			RenderingPanel.Visibility = Visibility.Collapsed;

			var picker = new FileOpenPicker();
			picker.FileTypeFilter.Add(".pdf");
			StorageFile file = await picker.PickSingleFileAsync();
			if (file != null)
			{
				ProgressControl.Visibility = Visibility.Visible;
				try
				{
					var stream = await file.OpenAsync(FileAccessMode.Read);
					pdfDocument = await PdfDocument.LoadFromStreamAsync(stream, PasswordBox.Password);
				}
				catch (Exception ex)
				{
					Windows.UI.Popups.MessageDialog dialog = ex.HResult switch
					{
						WrongPassword => new("Document is password-protected and password is incorrect.", "Error"),
						GenericFail => new("Document is not a valid PDF.", "Error"),
						_ => new(ex.Message, "Error")
					};
					await dialog.ShowAsync();
				}

				if (pdfDocument != null)
				{
					RenderingPanel.Visibility = Visibility.Visible;
					if (pdfDocument.IsPasswordProtected)
					{
						Protected.Visibility = Visibility.Visible;
					}
					else
					{
						Protected.Visibility = Visibility.Collapsed;
					}
					PageCountText.Text = pdfDocument.PageCount.ToString();
				}
				ProgressControl.Visibility = Visibility.Collapsed;
			}
			LoadButton.IsEnabled = true;
		}

		private async void ViewPage(object sender, RoutedEventArgs args)
		{
			uint pageNumber;
			if (!uint.TryParse(PageNumberBox.Text, out pageNumber) || (pageNumber < 1) || (pageNumber > pdfDocument.PageCount))
			{
				Windows.UI.Popups.MessageDialog dialog = new("Invalid page number.", "Error");
				await dialog.ShowAsync();
				return;
			}

			Output.Source = null;
			ProgressControl.Visibility = Visibility.Visible;

			// Convert from 1-based page number to 0-based page index.
			uint pageIndex = pageNumber - 1;

			using (PdfPage page = pdfDocument.GetPage(pageIndex))
			{
				var stream = new InMemoryRandomAccessStream();
				switch (Options.SelectedIndex)
				{
					// Arrange size to viewport with
					case 0:
						await page.RenderToStreamAsync(stream, new() { DestinationWidth = (uint)(viewer.ViewportWidth - 20) });
						break;
					// View actual size.
					case 1:
						await page.RenderToStreamAsync(stream);
						break;

					// View half size on beige background.
					case 2:
						var options1 = new PdfPageRenderOptions();
						options1.BackgroundColor = Windows.UI.Colors.Beige;
						options1.DestinationHeight = (uint)(page.Size.Height / 2);
						options1.DestinationWidth = (uint)(page.Size.Width / 2);
						await page.RenderToStreamAsync(stream, options1);
						break;

					// Crop to center.
					case 3:
						var options2 = new PdfPageRenderOptions();
						var rect = page.Dimensions.TrimBox;
						options2.SourceRect = new Rect(rect.X + rect.Width / 4, rect.Y + rect.Height / 4, rect.Width / 2, rect.Height / 2);
						await page.RenderToStreamAsync(stream, options2);
						break;
				}
				BitmapImage src = new BitmapImage();
				await src.SetSourceAsync(stream);
				Output.Source = src;
			}
			ProgressControl.Visibility = Visibility.Collapsed;
		}
	}
}

