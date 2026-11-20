using Microsoft.UI.Composition;
using Microsoft.UI.Xaml.Media.Imaging;
using Uno.Disposables;
using Uno.UI.Xaml.Media;

namespace Microsoft.UI.Xaml.Media
{
	public partial class ImageBrush
	{
		private readonly SerialDisposable _sourceSubscription = new SerialDisposable();

		// Changes on every ImageSource change, so a Subscribe that the app superseded meanwhile is noticed.
		private int _subscriptionVersion;

		// The SVG source ImageOpened was raised for: an SVG source raises it on a brush once, however often it loads.
		private SvgImageSource _svgImageOpenedFor;

		internal ImageData? ImageDataCache;

		partial void OnSourceChangedPartial(ImageSource newValue, ImageSource oldValue)
		{
			// The new source draws nothing until it opens, as on WinUI, where a brush draws its source's surface. The
			// composition brush lets go of the old image before the subscription can release it.
			ImageDataCache = default;
			if (_compositionBrush is CompositionSurfaceBrush surfaceBrush)
			{
				surfaceBrush.Surface = null;
			}

			if (!ReferenceEquals(newValue, _svgImageOpenedFor))
			{
				_svgImageOpenedFor = null;
			}

			var version = ++_subscriptionVersion;
			var subscription = newValue?.SubscribeWeakly(OnSourceOpened);

			// Subscribing ran app code (an ImageOpened handler) that may have set another ImageSource, subscribed to by now.
			if (version == _subscriptionVersion)
			{
				_sourceSubscription.Disposable = subscription;
			}
			else
			{
				subscription?.Dispose();
			}
		}

		private void OnSourceOpened(ImageData image)
		{
			ImageDataCache = image;

			if (image.Kind == ImageDataKind.Error)
			{
				// Note: On WASM, images are loaded by the platform (we only propagate the url),
				//		 so the ImageFailed won't be raised if the url is invalid ...
				OnImageFailed();
			}
			else if (ImageSource is not SvgImageSource svgImageSource)
			{
				OnImageOpened();
			}
			else if (image.HasData && !ReferenceEquals(_svgImageOpenedFor, svgImageSource))
			{
				_svgImageOpenedFor = svgImageSource;
				OnImageOpened();
			}

			OnInvalidateRender();
		}
	}
}
