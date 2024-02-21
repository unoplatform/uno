#nullable enable

using System;
using Microsoft.UI.Composition;
using Uno.UI.Xaml.Media;

using WinUICoreServices = Uno.UI.Xaml.Core.CoreServices;

namespace Microsoft.UI.Xaml.Media.Imaging
{
	internal unsafe class GLImageSource(uint width, uint height, void* pixels) : ImageSource
	{
		private SkiaCompositionSurface _surface = new SkiaCompositionSurface();

		private protected override bool TryOpenSourceSync(int? targetWidth, int? targetHeight, out ImageData image)
		{
			_surface.CopyPixels((int)width, (int)height, (IntPtr)pixels);
			image = ImageData.FromCompositionSurface(_surface);
			InvalidateImageSource();
			return image.HasData;
		}

		public void Render() { InvalidateSource(); }
	}
}
