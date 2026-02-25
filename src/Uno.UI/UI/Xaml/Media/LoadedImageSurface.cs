#nullable enable
#pragma warning disable 649 // Field is never assigned to
#pragma warning disable 67 // The event is never used

using System;
using Windows.Foundation;
using Microsoft.UI.Composition;

namespace Microsoft.UI.Xaml.Media
{
	public partial class LoadedImageSurface : IDisposable, ICompositionSurface
	{
		private Size _decodedPhysicalSize;
		private Size _decodedSize;
		private Size _naturalPhysicalSize;

		internal LoadedImageSurface() { }

		public Size DecodedPhysicalSize { get => _decodedPhysicalSize; }
		public Size DecodedSize { get => _decodedSize; }
		public Size NaturalSize { get => _naturalPhysicalSize; }

		public event TypedEventHandler<LoadedImageSurface, LoadedImageSourceLoadCompletedEventArgs>? LoadCompleted;
	}
}
