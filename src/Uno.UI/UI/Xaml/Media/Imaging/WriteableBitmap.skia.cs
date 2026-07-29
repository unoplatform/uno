using System.IO;
using Microsoft.UI.Composition;
using Uno.UI.Composition.Drawing;
using Uno.UI.Xaml.Media;

namespace Microsoft.UI.Xaml.Media.Imaging
{
	partial class WriteableBitmap
	{
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
			if (DrawingBackend.Current.TryDecodeImage(_stream.AsStream(), null, null, out var frames))
			{
				using (frames)
				{
					frames.Frames[0].CopyPixels(_buffer.Span);
				}
			}
		}
	}
}
