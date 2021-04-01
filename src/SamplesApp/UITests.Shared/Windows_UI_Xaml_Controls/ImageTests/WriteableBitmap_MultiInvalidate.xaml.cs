using System;
using System.Reflection;
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
			if (_bitmap.PixelBuffer is Windows.Storage.Streams.Buffer buffer
				&& buffer.GetType().GetField("_data", BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(buffer) is Memory<byte> data)
			{
				var span = data.Span;
				for (var i = 0; i < data.Length; i++)
				{
					if (i % 4 == 3)
					{
						// Alpha channel
						span[i] = 255;
					}
					else
					{
						span[i] = (byte)randomizer.Next(256);
					}
				}
			}
			else
			{
				throw new InvalidOperationException("Could not access _data field in Buffer type.");
			}

			// This request to the image to redraw the buffer
			_bitmap.Invalidate();
		}
	}
}
