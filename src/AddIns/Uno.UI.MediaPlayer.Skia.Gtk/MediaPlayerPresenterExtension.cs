#nullable enable

using System;
using Atk;
using Cairo;
using LibVLCSharp.Shared;
using Pango;
using Uno.Foundation.Extensibility;
using Uno.Foundation.Logging;
using Uno.Media.Playback;
using Windows.Foundation;
using Windows.Media.Playback;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.Xaml;
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

		var ContentView = new ContentControl();
		ContentView.Content = _player;

		ContentView.VerticalContentAlignment = Windows.UI.Xaml.VerticalAlignment.Stretch;
		ContentView.HorizontalContentAlignment = Windows.UI.Xaml.HorizontalAlignment.Stretch;
		ContentView.Background = new SolidColorBrush(Colors.Yellow);
		_owner.Child = ContentView;

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

	public void ExitFullScreen()
	{
		_player.ExitFullScreen();
	}
	public void RequestFullScreen()
	{
		_player.RequestFullScreen();
	}
	public void StretchChanged()
	{
		if (_owner is not null)
		{
			_player.UpdateVideoStretch(_owner.Stretch);
		}
	}
}
