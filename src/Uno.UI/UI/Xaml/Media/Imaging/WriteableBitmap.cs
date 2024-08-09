using System;
using System.Threading.Tasks;
using Windows.Storage.Streams;
using UwpBuffer = Windows.Storage.Streams.Buffer;

namespace Windows.UI.Xaml.Media.Imaging
{
	public partial class WriteableBitmap : BitmapSource
	{
		private UwpBuffer _buffer;

		public IBuffer PixelBuffer => _buffer;

		public WriteableBitmap(int pixelWidth, int pixelHeight) : base()
		{
			PixelWidth = pixelWidth;
			PixelHeight = pixelHeight;
			UpdateBuffer();
		}

		private void UpdateBuffer()
		{
			var pixelsBufferSize = (uint)(PixelWidth * PixelHeight * 4);
			_buffer = new UwpBuffer(pixelsBufferSize)
			{
				Length = pixelsBufferSize
			};
		}

		public void Invalidate()
		{
#if __WASM__ || __SKIA__
			InvalidateSource();
#endif
			InvalidateImageSource();
		}

		private protected
#if __SKIA__
			unsafe
#endif
			override void OnSetSource()
		{
			UpdateBuffer();

#if __ANDROID__ || __SKIA__ // TODO: Other platforms.
			DecodeStreamIntoBuffer();
#endif
		}
	}
}
