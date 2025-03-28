#nullable enable

using System;
using Uno.Foundation.Extensibility;
using Uno.Foundation.Logging;
using Uno.Media.Playback;
using Windows.Foundation;
using Windows.Media.Playback;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Maps;
using Windows.UI.Xaml.Media;

[assembly: ApiExtension(typeof(IMediaPlayerPresenterExtension), typeof(Uno.UI.Media.MediaPlayerPresenterExtension))]

namespace Uno.UI.Media;

public class MediaPlayerPresenterExtension : IMediaPlayerPresenterExtension
{
	private MediaPlayerPresenter? _owner;
	private HtmlMediaPlayer _htmlPlayer;

	public uint NaturalVideoHeight => (uint)_htmlPlayer.VideoHeight;
	public uint NaturalVideoWidth => (uint)_htmlPlayer.VideoWidth;

	public MediaPlayerPresenterExtension(object owner)
	{
		if (owner is not MediaPlayerPresenter presenter)
		{
			throw new InvalidOperationException($"MediaPlayerPresenterExtension must be initialized with a MediaPlayer instance");
		}

		_owner = presenter;
		_owner.Child = _htmlPlayer = new HtmlMediaPlayer();
	}

	public void MediaPlayerChanged()
	{
		if (this.Log().IsEnabled(LogLevel.Debug))
		{
			this.Log().LogDebug("Enter MediaPlayerPresenterExtension.MediaPlayerChanged().");
		}
		if (_owner is not null
			&& MediaPlayerExtension.GetByMediaPlayer(_owner.MediaPlayer) is { } extension)
		{
			extension.HtmlPlayer = _htmlPlayer;
		}
		else
		{
			if (this.Log().IsEnabled(LogLevel.Debug))
			{
				this.Log().LogDebug($"MediaPlayerPresenter.OnMediaPlayerChanged: Unable to find associated MediaPlayerExtension");
			}
		}
	}
	public void ExitFullScreen()
	{
		_htmlPlayer.ExitFullScreen();
	}

	public void RequestFullScreen()
	{
		_htmlPlayer.RequestFullScreen();
	}
	public void ExitCompactOverlay()
	{
		_htmlPlayer.ExitCompactOverlay();
	}

	public void RequestCompactOverlay()
	{
		_htmlPlayer.RequestCompactOverlay();
	}
	public void StretchChanged()
	{
		if (_owner is not null)
		{
			_htmlPlayer.UpdateVideoStretch(_owner.Stretch);
		}
	}
}
