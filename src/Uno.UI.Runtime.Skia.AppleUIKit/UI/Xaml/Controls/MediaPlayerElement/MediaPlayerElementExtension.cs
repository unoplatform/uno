using System;
using AVFoundation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Uno.Media.Playback;

namespace Uno.UI.Runtime.Skia.AppleUIKit;

internal class MediaPlayerPresenterExtension : IMediaPlayerPresenterExtension
{
	private readonly MediaPlayerPresenter _presenter;

	public uint NaturalVideoWidth => _presenter.MediaPlayer?.NaturalVideoWidth ?? 0;

	public uint NaturalVideoHeight => _presenter.MediaPlayer?.NaturalVideoHeight ?? 0;

	public MediaPlayerPresenterExtension(MediaPlayerPresenter presenter) => _presenter = presenter;

	private void SetVideoSurface(IVideoSurface videoSurface)
	{
		_presenter.Child = new ContentPresenter()
		{
			Content = videoSurface
		};
	}

	public void MediaPlayerChanged() => SetVideoSurface(_presenter.MediaPlayer.RenderSurface);

	public void StretchChanged() => ApplyStretch();

	internal void ApplyStretch()
	{
		if (_presenter.MediaPlayer == null)
		{
			return;
		}

		switch (_presenter.Stretch)
		{
			case Stretch.Uniform:
				_presenter.MediaPlayer.UpdateVideoGravity(AVLayerVideoGravity.ResizeAspect);
				break;

			case Stretch.Fill:
				_presenter.MediaPlayer.UpdateVideoGravity(AVLayerVideoGravity.Resize);
				break;

			case Stretch.None:
			case Stretch.UniformToFill:
				_presenter.MediaPlayer.UpdateVideoGravity(AVLayerVideoGravity.ResizeAspectFill);
				break;

			default:
				throw new NotSupportedException($"Stretch mode {_presenter.Stretch} is not supported");
		}
	}

	public void RequestFullScreen() { }

	public void ExitFullScreen() { }

	public void RequestCompactOverlay() { }

	public void ExitCompactOverlay() { }
}
