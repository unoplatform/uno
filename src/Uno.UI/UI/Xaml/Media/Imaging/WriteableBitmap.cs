#nullable disable

using System;
using System.IO;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Storage.Streams;
using UwpBuffer = Windows.Storage.Streams.Buffer;

namespace Windows.UI.Xaml.Media.Imaging
{
	public partial class WriteableBitmap : BitmapSource
	{
		internal event EventHandler Invalidated;

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
			Invalidated?.Invoke(this, EventArgs.Empty);
		}

		private protected override void OnSetSource()
		{
			UpdateBuffer();
#if __NETSTD__
			_stream.AsStream().CopyTo(_buffer.AsStream());
#else
			Stream.CopyTo(_buffer.AsStream());
			Stream.Position = 0;
#endif
		}
	}
}
