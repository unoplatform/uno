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

	public sealed partial class Image_Svg : Page
	{
		public Image_Svg()
		{
			this.InitializeComponent();
		}

		private async void LoadClicked(object sender, RoutedEventArgs e)
		{
			img1.Source = null;

			await Task.Yield();

			log.Text = "";
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
					bitmapSource.ImageFailed += (snd, evt) => Log($"ERROR: {evt.ErrorMessage}");
					bitmapSource.ImageOpened += (snd, evt) => Log($"LOADED from {evt.OriginalSource}");

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
					svgSource.OpenFailed += (snd, evt) => Log($"ERROR: {evt.Status}");
					svgSource.Opened += (snd, evt) => Log("LOADED");
					source = svgSource;
					break;
				}
				case "SvgImageSource2":
				{
					var border = img1.Parent as FrameworkElement;
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
					svgSource.OpenFailed += (snd, evt) => Log($"ERROR: {evt.Status}");
					svgSource.Opened += (snd, evt) => Log("LOADED");
					break;
				}
			}
			img1.Source = source;

			void Log(string msg)
			{
				log.Text += msg + "\n";
			}
		}

		private async Task<IRandomAccessStream> GetStream()
		{
#if __WASM__
			using var httpClient = new HttpClient(new Uno.UI.Wasm.WasmHttpHandler());
#else
			using var httpClient = new HttpClient();
#endif
			var data = await httpClient.GetByteArrayAsync(url.Text);

			return new MemoryStream(data).AsRandomAccessStream();

		}
	}
}
