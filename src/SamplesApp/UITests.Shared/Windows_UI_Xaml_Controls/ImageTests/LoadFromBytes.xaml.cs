using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using System.Threading.Tasks;
using Uno.UI.Samples.Controls;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;

namespace Uno.UI.Samples.UITests.ImageTestsControl
{
	[SampleControlInfo("Image", "LoadFromBytes")]
	public sealed partial class LoadFromBytes : UserControl
	{
		private string _imageUrl =
			"http://lh5.ggpht.com/lxBMauupBiLIpgOgu5apeiX_YStXeHRLK1oneS4NfwwNt7fGDKMP0KpQIMwfjfL9GdHRVEavmg7gOrj5RYC4qwrjh3Y0jCWFDj83jzg";

		public LoadFromBytes()
		{
			InitializeComponent();
			Loaded += OnLoaded;
		}

		private async void OnLoaded(object sender, RoutedEventArgs e)
		{
			Stream stream;

			var client = new HttpClient(new CorsByPassHandler());

			var result = await client.GetAsync(_imageUrl);
			stream = await result.Content.ReadAsStreamAsync();


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
