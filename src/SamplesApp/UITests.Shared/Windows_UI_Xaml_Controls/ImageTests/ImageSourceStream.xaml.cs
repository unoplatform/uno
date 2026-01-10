using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Windows.Storage.Streams;
using Uno.UI.Samples.Controls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;


namespace Uno.UI.Samples.UITests.ImageTestsControl
{
	[Sample("Image", Name = nameof(ImageSourceStream))]
	public sealed partial class ImageSourceStream : Page
	{
		public ImageSourceStream()
		{
			this.InitializeComponent();

			this.Loaded += OnLoaded;
		}

		private const string _imageUrl = "https://via.placeholder.com/420x220.png?text=Super+Image";

		private async void OnLoaded(object sender, RoutedEventArgs e)
		{
			using var stream = await GetStream(_imageUrl);

			var bitmap = new BitmapImage();
			await bitmap.SetSourceAsync(stream).AsTask();
			MyImage.Source = bitmap;
		}

		private static async Task<IRandomAccessStream> GetStream(string url)
		{
			using var httpClient = new HttpClient();
			var data = await httpClient.GetByteArrayAsync(url);

			return new MemoryStream(data).AsRandomAccessStream();

		}
	}
}
