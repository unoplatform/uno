using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices.WindowsRuntime;
using Uno.UI.Samples.Controls;
using Uno.UI.Samples.Helper;
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

namespace Uno.UI.Samples.UITests.ImageBrushTestControl
{
	[Sample]
	public sealed partial class ImageBrush_StreamSource : UserControl
	{
		public static DependencyProperty MySourceProperty { get; } =
			DependencyProperty.Register("MySource", typeof(ImageSource), typeof(ImageBrush_StreamSource), new PropertyMetadata(null));

		public ImageSource MySource
		{
			get => (ImageSource)GetValue(MySourceProperty);
			set => SetValue(MySourceProperty, value);
		}

		public ImageBrush_StreamSource()
		{
			this.InitializeComponent();

			this.RunWhileLoaded(async ct =>
			{
#if __WASM__
				using var httpClient = new HttpClient(new Uno.UI.Wasm.WasmHttpHandler());
#else
				using var httpClient = new HttpClient();
#endif
				const string imageUrl = "https://nv-assets.azurewebsites.net/tests/images/uno-overalls.jpg";
				var data = await httpClient.GetByteArrayAsync(imageUrl);

				BitmapImage bitmapImage;
				MySource = bitmapImage = new BitmapImage();

#if NETFX_CORE
				using var stream = new MemoryStream(data).AsRandomAccessStream();
#else
				using var stream = new MemoryStream(data);
#endif
				await bitmapImage.SetSourceAsync(stream);
				MySource = bitmapImage;
			});
		}
	}
}
