using Windows.UI.Xaml.Media;

namespace Windows.UI.Xaml.Controls
{
	public interface IMediaPlayerPresenterExtension
	{
		void MediaPlayerChanged();

		void StretchChanged();

		void RequestFullScreen();

		void ExitFullScreen();

		void RequestCompactOverlay();

		void ExitCompactOverlay();

		uint NaturalVideoHeight { get; }

		uint NaturalVideoWidth { get; }
	}
}
