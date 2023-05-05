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
	private GTKMediaPlayer _player;

	public MediaPlayerPresenterExtension(object owner)
	{
		if (owner is not MediaPlayerPresenter presenter)
		{
			throw new InvalidOperationException($"MediaPlayerPresenterExtension must be initialized with a MediaPlayer instance");
		}

		_owner = presenter;
		_owner.Child = _player = new GTKMediaPlayer();
	}


	public void MediaPlayerChanged()
	{
		//if (this.Log().IsEnabled(LogLevel.Debug))
		//{
		//	this.Log().LogDebug("Enter MediaPlayerPresenterExtension.MediaPlayerChanged().");
		//}
		if (_owner is not null
			&& MediaPlayerExtension.GetByMediaPlayer(_owner.MediaPlayer) is { } extension)
		{
			extension._player = _player;
		}
		else
		{
			//if (this.Log().IsEnabled(LogLevel.Debug))
			//{
			//	this.Log().LogDebug($"MediaPlayerPresenter.OnMediaPlayerChanged: Unable to find associated MediaPlayerExtension");
			//}
		}
	}

	public void ExitFullScreen() => throw new NotImplementedException();
	public void RequestFullScreen() => throw new NotImplementedException();
	public void StretchChanged() => throw new NotImplementedException();
}
