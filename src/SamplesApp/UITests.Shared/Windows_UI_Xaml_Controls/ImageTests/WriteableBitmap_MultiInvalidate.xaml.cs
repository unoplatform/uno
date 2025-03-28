using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices.WindowsRuntime;
using Uno.UI.Samples.Controls;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Imaging;

namespace UITests.Windows_UI_Xaml_Controls.ImageTests
{
	[Sample("WriteableBitmap")]
	public sealed partial class WriteableBitmap_MultiInvalidate : Page
	{
		private readonly WriteableBitmap _bitmap;
		private int _seed = 42;

		public WriteableBitmap_MultiInvalidate()
		{
			this.InitializeComponent();

			_bitmap = new WriteableBitmap(200, 200);
			_image.Source = _bitmap;
		}

		private void UpdateSource(object sender, RoutedEventArgs e)
		{
			var randomizer = new Random(_seed++);
			var stream = _bitmap.PixelBuffer.AsStream();
			var length = _bitmap.PixelBuffer.Length;
			for (var i = 0; i < length; i++)
			{
				if (i % 4 == 3)
				{
					stream.WriteByte(255);
				}
				else
				{
					stream.WriteByte((byte)randomizer.Next(256));
				}
			}

			// This request to the image to redraw the buffer
			_bitmap.Invalidate();
		}
	}
}
