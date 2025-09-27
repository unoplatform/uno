using System;
using Android.App;
using Android.Views;
using Android.Widget;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Uno.Media.Playback;
namespace Uno.UI.Runtime.Skia.Android;

internal class AndroidSkiaMediaPlayerPresenterExtension : IMediaPlayerPresenterExtension
{
	private readonly MediaPlayerPresenter _presenter;

	public uint NaturalVideoWidth => _presenter.MediaPlayer?.NaturalVideoWidth ?? 0;
	public uint NaturalVideoHeight => _presenter.MediaPlayer?.NaturalVideoHeight ?? 0;

	public AndroidSkiaMediaPlayerPresenterExtension(MediaPlayerPresenter presenter)
	{
		_presenter = presenter;
	}

	private void SetVideoSurface(IVideoSurface videoSurface)
	{
		if (videoSurface is View view)
		{
			// We wrap the video view in a FrameLayout because
			// _presenter.MediaPlayer.UpdateVideoStretch uses the
			// layout dimensions of the parent to stretch the video
			// view within that parent. Without the FrameLayout,
			// the parent is UnoSKSurfaceView which covers the whole
			// screen. This way, the parent has the correct size that
			// the video view can stretch within.
			var layout = new FrameLayout(Application.Context);
			layout.AddView(view);
			_presenter.Child = new ContentPresenter
			{
				Content = layout
			};
		}
	}

	internal void ApplyStretch()
	{
		if (_presenter.MediaPlayer == null)
		{
			return;
		}

		switch (_presenter.Stretch)
		{
			case Stretch.Uniform:
				_presenter.MediaPlayer.UpdateVideoStretch(global::Windows.Media.Playback.MediaPlayer.VideoStretch.Uniform);
				break;

			case Stretch.Fill:
				_presenter.MediaPlayer.UpdateVideoStretch(global::Windows.Media.Playback.MediaPlayer.VideoStretch.Fill);
				break;

			case Stretch.None:
				_presenter.MediaPlayer.UpdateVideoStretch(global::Windows.Media.Playback.MediaPlayer.VideoStretch.None);
				break;

			case Stretch.UniformToFill:
				_presenter.MediaPlayer.UpdateVideoStretch(global::Windows.Media.Playback.MediaPlayer.VideoStretch.UniformToFill);
				break;

			default:
				throw new NotSupportedException($"Stretch mode {_presenter.Stretch} is not supported");
		}
	}

	public void MediaPlayerChanged() => SetVideoSurface(_presenter.MediaPlayer.RenderSurface);

	public void StretchChanged() => ApplyStretch();
	public void RequestFullScreen() { }
	public void ExitFullScreen() { }
	public void RequestCompactOverlay() { }
	public void ExitCompactOverlay() { }
}
