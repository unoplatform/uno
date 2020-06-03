using System;
using System.IO;
using Windows.Foundation;
using Windows.Storage.Streams;

namespace Windows.UI.Xaml.Media.Imaging
{
	public partial class WriteableBitmap : BitmapSource
	{
		internal event EventHandler Invalidated;

		private InMemoryBuffer _buffer;

		public IBuffer PixelBuffer => _buffer;

		public WriteableBitmap(int pixelWidth, int pixelHeight) : base()
		{
			_buffer = new InMemoryBuffer(pixelWidth * pixelHeight * 4);

			PixelWidth = pixelWidth;
			PixelHeight = pixelHeight;
		}

		public void Invalidate()
		{
#if __WASM__
			InvalidateSource();
#endif
			Invalidated?.Invoke(this, EventArgs.Empty);
		}
	}
}
