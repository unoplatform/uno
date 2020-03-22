using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Uno.UI.Samples.Controls;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Imaging;

namespace Uno.UI.Samples.UITests.ImageTestsControl
{
	[SampleControlInfo("Image", "LoadFromBytes")]
	public sealed partial class LoadFromBytes : UserControl
	{
		private readonly string _imageUrl = "https://www.spriters-resource.com/resources/sheet_icons/30/32521.png";

		public LoadFromBytes()
		{
			InitializeComponent();
			Loaded += OnLoaded;
		}

		private async void OnLoaded(object sender, RoutedEventArgs e)
		{
			var client = new HttpClient(new CorsByPassHandler());

			var result = await client.GetAsync(_imageUrl);
			var stream = await result.Content.ReadAsStreamAsync();


			var bitmap = new BitmapImage();
#if !NETFX_CORE
			await bitmap.SetSourceAsync(stream);
#else
				var memStream = new MemoryStream();
				await stream.CopyToAsync(memStream);
				memStream.Position = 0;
				using (memStream)
				{
					await bitmap.SetSourceAsync(memStream.AsRandomAccessStream());
				}
#endif
			MyImage.Source = bitmap;

		}

		public class CorsByPassHandler : DelegatingHandler
		{
			public CorsByPassHandler()
			{
				InnerHandler = new HttpClientHandler();
			}

			protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
			{
				var builder = new UriBuilder(request.RequestUri)
				{
					Host = "cors-anywhere.herokuapp.com",
				};

				builder.Path = request.RequestUri.Host + builder.Path;

				return base.SendAsync(new HttpRequestMessage(request.Method, builder.Uri)
				{
					Headers = { { "origin", "" } },
				}, cancellationToken);
			}
		}
	}
}
