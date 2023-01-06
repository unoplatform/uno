using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage.Streams;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Uno.UI.Samples.Controls;

namespace UITests.Windows_UI_Xaml_Controls.ImageTests
{
	[Sample("Image")]

	public sealed partial class BitmapImage_vs_SvgImageSource : Page
	{
		public BitmapImage_vs_SvgImageSource()
		{
			this.InitializeComponent();

			img.ImageOpened += (snd, evt) => imgIsLoaded.IsChecked = true;
			img.ImageFailed += (snd, evt) => imgIsError.IsChecked = true;
		}

		private async void LoadClicked(object sender, RoutedEventArgs e)
		{
			img.Source = null;

			await Task.Yield();

			log.Text = "";

			isLoaded.IsChecked = false;
			isError.IsChecked = false;
			imgIsLoaded.IsChecked = false;
			imgIsError.IsChecked = false;

			var uri = new Uri(url.Text);
			ImageSource source = null;

			await Task.Yield();

			var stream = (streamMode.IsChecked ?? false) ? await GetStream() : null;

			var mode = (sender as FrameworkElement)?.Tag as string;
			Log($"Loading {url.Text} using {mode}...");
			switch (mode)
			{
				case "BitmapImage":
					{
						BitmapImage bitmapSource;
						if (stream == null)
						{
							bitmapSource = new BitmapImage(uri);
						}
						else
						{
							bitmapSource = new BitmapImage();
							await bitmapSource.SetSourceAsync(stream);
						}
						bitmapSource.DownloadProgress += (snd, evt) => Log($"Downloadind... {evt.Progress}%");
						bitmapSource.ImageFailed += (snd, evt) =>
						{
							isError.IsChecked = true;
							Log($"ERROR: {evt.ErrorMessage}");
						};
						bitmapSource.ImageOpened += (snd, evt) =>
						{
							isLoaded.IsChecked = true;
							Log($"LOADED from {evt.OriginalSource}");
						};

						source = bitmapSource;
						break;
					}
				case "SvgImageSource":
					{
						SvgImageSource svgSource;
						if (stream == null)
						{
							svgSource = new SvgImageSource(uri);
						}
						else
						{
							svgSource = new SvgImageSource();
							await svgSource.SetSourceAsync(stream);
						}
						svgSource.OpenFailed += (snd, evt) =>
						{
							isError.IsChecked = true;
							Log($"ERROR: {evt.Status}");
						};
						svgSource.Opened += (snd, evt) =>
						{
							isLoaded.IsChecked = true;
							Log("LOADED");
						};
						source = svgSource;
						break;
					}
				case "SvgImageSource2":
					{
						var border = img.Parent as FrameworkElement;
						SvgImageSource svgSource;
						if (stream == null)
						{
							svgSource = new SvgImageSource(uri);
						}
						else
						{
							svgSource = new SvgImageSource();
							await svgSource.SetSourceAsync(stream);
						}
						source = svgSource;
						Log($"RasterizePixelWidth/Height: {border.ActualWidth}x{border.ActualHeight}");
						svgSource.RasterizePixelWidth = border.ActualWidth;
						svgSource.RasterizePixelHeight = border.ActualHeight;
						svgSource.OpenFailed += (snd, evt) =>
						{
							isError.IsChecked = true;
							Log($"ERROR: {evt.Status}");
						};
						svgSource.Opened += (snd, evt) =>
						{
							isLoaded.IsChecked = true;
							Log("LOADED");
						};
						break;
					}
			}
			img.Source = source;

			void Log(string msg)
			{
				log.Text += msg + "\n";
			}
		}

		private async Task<IRandomAccessStream> GetStream()
		{
			using var httpClient = new HttpClient();
			var data = await httpClient.GetByteArrayAsync(url.Text);

			return new MemoryStream(data).AsRandomAccessStream();

		}
	}
}
