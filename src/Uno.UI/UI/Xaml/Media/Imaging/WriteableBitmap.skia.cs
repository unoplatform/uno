using System;
using System.IO;
using System.Runtime.InteropServices;
using Windows.Foundation;
using Windows.Storage.Streams;
using Uno.Foundation;
using Windows.UI.Composition;

namespace Windows.UI.Xaml.Media.Imaging
{
	partial class WriteableBitmap
	{
		private SkiaCompositionSurface _surface;

		private protected override bool TryOpenSourceSync(int? targetWidth, int? targetHeight, out ImageData image)
		{
			var handle = GCHandle.Alloc(_buffer.Data, GCHandleType.Pinned);

			_surface ??= new SkiaCompositionSurface();

			_surface.SetPixels(PixelWidth, PixelHeight, _buffer.Data);

			image = new ImageData {
				Value = _surface
			};

			return true;
		}
	}
}
