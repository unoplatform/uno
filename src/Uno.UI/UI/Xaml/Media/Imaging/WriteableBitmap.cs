using System;
using System.Threading.Tasks;
using Windows.Storage.Streams;
using UwpBuffer = Windows.Storage.Streams.Buffer;
using System.IO;
using Microsoft.UI.Composition;
using Uno.UI.Composition.Drawing;
using Uno.UI.Xaml.Media;

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

		private CompositionImageSurface _surface;

		private protected override bool TryOpenSourceSync(int? targetWidth, int? targetHeight, out ImageData image)
		{
			_surface ??= new CompositionImageSurface();

			_surface.CopyPixels(PixelWidth, PixelHeight, _buffer.AsReadOnlyMemory());

			image = ImageData.FromCompositionSurface(_surface);

			return true;
		}

		private void DecodeStreamIntoBuffer()
		{
			// Decode the encoded stream to BGRA (premultiplied) pixels through the neutral backend decoder.
			if (ImageEncoderDecoder.Current.TryDecode(_stream.AsStream(), null, null, out var frames))
			{
				using (frames)
				{
					frames.Frames[0].CopyPixels(_buffer.Span);
				}
			}
		}
	}
}
