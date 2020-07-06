using System;
using Windows.Storage.Streams;
using UwpBuffer = Windows.Storage.Streams.Buffer;


namespace Windows.UI.Xaml.Media.Imaging
{
	public partial class WriteableBitmap : BitmapSource
	{
		internal event EventHandler Invalidated;

		public IBuffer PixelBuffer { get; }

		public WriteableBitmap(int pixelWidth, int pixelHeight) : base()
		{
			PixelBuffer = new UwpBuffer((uint)(pixelWidth * pixelHeight * 4));

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
