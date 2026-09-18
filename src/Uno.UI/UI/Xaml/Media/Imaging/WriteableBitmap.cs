using System;
using System.Threading.Tasks;
using Windows.Storage.Streams;
using UwpBuffer = Windows.Storage.Streams.Buffer;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.UI.Composition;
using Uno.UI.Xaml.Media;
using SkiaSharp;

namespace Microsoft.UI.Xaml.Media.Imaging
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
			if (_buffer?.Capacity != pixelsBufferSize)
			{
				_buffer = new UwpBuffer(pixelsBufferSize)
				{
					Length = pixelsBufferSize
				};
			}
		}

		public void Invalidate()
		{
#if __SKIA__
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

#if __SKIA__ // TODO: Other platforms.
			DecodeStreamIntoBuffer();
#endif
		}

		private SkiaCompositionSurface _surface;

		private protected override bool TryOpenSourceSync(int? targetWidth, int? targetHeight, out ImageData image)
		{
			_surface ??= new SkiaCompositionSurface();

			_surface.CopyPixels(PixelWidth, PixelHeight, _buffer.AsReadOnlyMemory());

			image = ImageData.FromCompositionSurface(_surface);

			return true;
		}

		private unsafe void DecodeStreamIntoBuffer()
		{
			using var img = SKImage.FromEncodedData(_stream.AsStream());
			var info = img.Info;

			fixed (byte* data = &MemoryMarshal.GetReference(_buffer.Span))
			{
				img.ReadPixels(info.WithColorType(SKColorType.Bgra8888), (nint)data, PixelWidth * 4);
			}
		}
	}
}
