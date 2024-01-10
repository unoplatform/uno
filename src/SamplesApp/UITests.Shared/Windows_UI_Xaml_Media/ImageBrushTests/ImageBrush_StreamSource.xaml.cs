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
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Navigation;

namespace Uno.UI.Samples.UITests.ImageBrushTestControl
{
	[Sample("Brushes")]
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
				using var httpClient = new HttpClient();
				const string imageUrl = "https://uno-assets.platform.uno/tests/images/uno-overalls.jpg";
				var data = await httpClient.GetByteArrayAsync(imageUrl);

				BitmapImage bitmapImage;
				MySource = bitmapImage = new BitmapImage();

#if WINAPPSDK
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
