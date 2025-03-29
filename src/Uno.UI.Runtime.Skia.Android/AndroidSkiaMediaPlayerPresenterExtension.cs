using System;
using Windows.Foundation;
using Android.Views;
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
		_presenter.Child = new ContentPresenter()
		{
			Content = videoSurface
		};
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
