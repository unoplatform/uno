#nullable enable

using System;
using Windows.Foundation;
using Windows.Storage.Streams;
using Windows.UI.Composition;

namespace Windows.UI.Xaml.Media
{
	public partial class LoadedImageSurface : IDisposable, ICompositionSurface
	{
		private Size? _decodedPhysicalSize;
		private Size? _decodedSize;
		private Size? _naturalPhysicalSize;

		public Size? DecodedPhysicalSize { get => _decodedPhysicalSize; }
		public Size? DecodedSize { get => _decodedSize; }
		public Size? NaturalSize { get => _naturalPhysicalSize; }

		public static partial LoadedImageSurface StartLoadFromUri(Uri uri);
		public static partial LoadedImageSurface StartLoadFromUri(Uri uri, Size desiredMaxSize);
		public static partial LoadedImageSurface StartLoadFromStream(IRandomAccessStream stream);
		public static partial LoadedImageSurface StartLoadFromStream(IRandomAccessStream stream, Size desiredMaxSize);

		public event TypedEventHandler<LoadedImageSurface, LoadedImageSourceLoadCompletedEventArgs>? LoadCompleted;

		public partial void Dispose();
	}
}
