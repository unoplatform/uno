using System;
using System.Linq;
using System.Reflection;
using Windows.Storage.Streams;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Imaging;
using Uno.UI.Samples.Controls;

#if NETFX_CORE
using System.Runtime.InteropServices.WindowsRuntime;
#endif

namespace UITests.Shared.Windows_UI_Xaml_Controls.ImageTests
{
	[SampleControlInfo(category: "Image", controlName: nameof(ImageSourceWriteableBitmapInvalidate))]
	public sealed partial class ImageSourceWriteableBitmapInvalidate : Page
	{
		private WriteableBitmap _bitmap;

		public ImageSourceWriteableBitmapInvalidate()
		{
			this.InitializeComponent();

			_bitmap = new WriteableBitmap(200, 200);
			_image.Source = _bitmap;
		}

		private void UpdateSource(object sender, RoutedEventArgs e)
		{
#if NETFX_CORE
			using (var data = _bitmap.PixelBuffer.AsStream())
			{
				// Half of the image in green, alpha 100% (bgra buffer)
				var pixel = new byte[] {0, 255, 0, 255};
				for (var i = 1; i < data.Length / 2; i += 4)
				{
					data.Write(pixel, 0, 4);
				}
				data.Flush();
			}
#else
			if (_bitmap.PixelBuffer is Windows.Storage.Streams.Buffer buffer
				&& buffer.GetType().GetProperty("Data", BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(buffer) is byte[] data)
			{
				// Half of the image in green, alpha 100% (bgra buffer)
				for (var i = 1; i < data.Length / 2; i += 2)
				{
					data[i] = byte.MaxValue;
				}
			}
#endif

			// This request to the image to redraw the buffer
			_bitmap.Invalidate();
		}
	}
}
