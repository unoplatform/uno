using Microsoft.UI.Xaml.Media;

namespace Microsoft.UI.Xaml.Controls
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
