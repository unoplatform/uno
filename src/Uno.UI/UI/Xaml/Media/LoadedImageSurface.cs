#nullable enable
#pragma warning disable 649 // Field is never assigned to
#pragma warning disable 67 // The event is never used

using System;
using Windows.Foundation;
using Windows.UI.Composition;

namespace Windows.UI.Xaml.Media
{
	public partial class LoadedImageSurface : IDisposable, ICompositionSurface
	{
		private Size _decodedPhysicalSize;
		private Size _decodedSize;
		private Size _naturalPhysicalSize;

#if HAS_UNO_WINUI
		internal LoadedImageSurface() { }
#else
		public LoadedImageSurface() { }
#endif

		public Size DecodedPhysicalSize { get => _decodedPhysicalSize; }
		public Size DecodedSize { get => _decodedSize; }
		public Size NaturalSize { get => _naturalPhysicalSize; }

		public event TypedEventHandler<LoadedImageSurface, LoadedImageSourceLoadCompletedEventArgs>? LoadCompleted;
	}
}
