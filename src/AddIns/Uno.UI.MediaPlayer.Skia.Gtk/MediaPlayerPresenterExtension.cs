#nullable enable

using System;
using Atk;
using Pango;
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


		_player = new GTKMediaPlayer();

		var ContentView = new Button();
		ContentView.Content = _player.VideoView;
		_player.VideoView?.SizeAllocate(new(0, 0, (int)_owner.ActualWidth, (int)_owner.ActualHeight));
		_player.VideoView?.SizeAllocate(new(0, 0, 800, 640));
		_owner.Child = ContentView;
		//_owner.Child = _player = new GTKMediaPlayer();
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
			extension.GTKMediaPlayer = _player;
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
	public void StretchChanged()
	{
		if (_owner is not null)
		{
			_player.UpdateVideoStretch(_owner.Stretch);
		}
	}
}
