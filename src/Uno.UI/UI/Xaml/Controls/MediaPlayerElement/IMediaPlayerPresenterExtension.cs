#if __ANDROID__ || __IOS__ || __MACOS__ || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__

using Windows.UI.Xaml.Media;

namespace Windows.UI.Xaml.Controls
{
	public interface IMediaPlayerPresenterExtension
	{
		void MediaPlayerChanged();

		void StretchChanged();

		void RequestFullScreen();

		void ExitFullScreen();
	}
}
#endif
