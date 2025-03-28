using System;
using System.Linq;
using System.Reflection;
using Windows.Storage.Streams;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Imaging;
using Uno.UI.Samples.Controls;
using System.Runtime.InteropServices.WindowsRuntime;

namespace UITests.Shared.Windows_UI_Xaml_Controls.ImageTests
{
	[Sample("WriteableBitmap", IgnoreInSnapshotTests = true, IsManualTest = true)]
	public sealed partial class ImageSourceWriteableBitmapInvalidate_Stretch : Page
	{
		private WriteableBitmap _bitmap;

		public ImageSourceWriteableBitmapInvalidate_Stretch()
		{
			this.InitializeComponent();

			_bitmap = new WriteableBitmap(2000, 2000);
			_image.Source = _bitmap;
		}


		private void UpdateSource(object sender, RoutedEventArgs e)
		{
			using (var data = _bitmap.PixelBuffer.AsStream())
			{
				var random = new Random();

				for (var i = 1; i < data.Length; i += 4)
				{
					// Half of the image in green, alpha 100% (bgra buffer)
					var pixel = new byte[] {
						(byte)random.Next(256)
						, (byte)random.Next(256)
						, (byte)random.Next(256)
						, 255
					};

					data.Write(pixel, 0, 4);
				}
				data.Flush();
			}

			// This request to the image to redraw the buffer
			_bitmap.Invalidate();
		}
	}
}
