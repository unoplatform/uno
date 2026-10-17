using Uno.Disposables;
using Microsoft.UI.Xaml.Media.Imaging;
using Uno.UI.Xaml.Media;

namespace Microsoft.UI.Xaml.Media
{
	public partial class ImageBrush
	{
		private readonly SerialDisposable _sourceSubscription = new SerialDisposable();

		// The SVG source ImageOpened was raised for: WinUI raises it together with the source's Opened, so an SVG source
		// raises it on a brush once.
		private SvgImageSource _svgImageOpenedFor;

		internal ImageData? ImageDataCache;

		partial void OnSourceChangedPartial(ImageSource newValue, ImageSource oldValue)
		{
			ImageDataCache = default;

			if (!ReferenceEquals(newValue, _svgImageOpenedFor))
			{
				_svgImageOpenedFor = null;
			}

			_sourceSubscription.Disposable = newValue?.Subscribe(image => OnSourceOpened(newValue, image));
		}

		private void OnSourceOpened(ImageSource source, ImageData image)
		{
			// A delivery already under way when the brush changed source is for the previous source.
			if (!ReferenceEquals(source, ImageSource))
			{
				return;
			}

			ImageDataCache = image;

			if (image.Kind == ImageDataKind.Error)
			{
				// Note: On WASM, images are loaded by the platform (we only propagate the url),
				//		 so the ImageFailed won't be raised if the url is invalid ...
				OnImageFailed();
			}
			else if (source is not SvgImageSource svgImageSource)
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
