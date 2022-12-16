using System;
using Windows.Storage.Streams;
using UwpBuffer = Windows.Storage.Streams.Buffer;

namespace Windows.UI.Xaml.Media.Imaging
{
	public partial class WriteableBitmap : BitmapSource
	{
		internal event EventHandler Invalidated;

		private readonly UwpBuffer _buffer;

		public IBuffer PixelBuffer => _buffer;

		public WriteableBitmap(int pixelWidth, int pixelHeight) : base()
		{
			var pixelsBufferSize = (uint)(pixelWidth * pixelHeight * 4);
			_buffer = new UwpBuffer(pixelsBufferSize)
			{
				Length = pixelsBufferSize
			};

			PixelWidth = pixelWidth;
			PixelHeight = pixelHeight;
		}

		public void Invalidate()
		{
			OnInvalidateSource(); 
			Invalidated?.Invoke(this, EventArgs.Empty);
		} 
	}
}
