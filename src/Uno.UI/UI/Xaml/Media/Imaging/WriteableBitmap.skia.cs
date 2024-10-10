using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Windows.UI.Composition;
using Uno.UI.Xaml.Media;
using SkiaSharp;

namespace Windows.UI.Xaml.Media.Imaging
{
	partial class WriteableBitmap
	{
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
