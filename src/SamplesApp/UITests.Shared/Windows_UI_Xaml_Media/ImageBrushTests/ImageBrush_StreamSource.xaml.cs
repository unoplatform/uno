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
using UITests.Shared.Helpers;
using System.Threading.Tasks;

namespace Uno.UI.Samples.UITests.ImageBrushTestControl
{
	[Sample("Brushes")]
	public sealed partial class ImageBrush_StreamSource : UserControl, IWaitableSample
	{
		private int _imageLoadCount;
		private TaskCompletionSource _tcs = new();

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

				imageBrush1.ImageOpened += ImageOpened;
				imageBrush1.ImageFailed += ImageFailed;
				imageBrush2.ImageOpened += ImageOpened;
				imageBrush2.ImageFailed += ImageFailed;
				imageBrush3.ImageOpened += ImageOpened;
				imageBrush3.ImageFailed += ImageFailed;
				imageBrush4.ImageOpened += ImageOpened;
				imageBrush4.ImageFailed += ImageFailed;
				imageBrush5.ImageOpened += ImageOpened;
				imageBrush5.ImageFailed += ImageFailed;
				imageBrush6.ImageOpened += ImageOpened;
				imageBrush6.ImageFailed += ImageFailed;
			});
		}

		public Task SamplePreparedTask => _tcs.Task;

		private void ImageOpened(object sender, RoutedEventArgs e)
			=> ImageOpenedOrFailed();

		private void ImageFailed(object sender, ExceptionRoutedEventArgs e)
			=> ImageOpenedOrFailed();

		private void ImageOpenedOrFailed()
		{
			_imageLoadCount++;
			if (_imageLoadCount == 6)
			{
				_tcs.SetResult();
			}
		}
	}
}
