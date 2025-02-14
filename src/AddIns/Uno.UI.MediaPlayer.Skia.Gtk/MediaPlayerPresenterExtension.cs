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
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Maps;
using Windows.UI.Xaml.Media;

#if HAS_UNO_WINUI
using Microsoft.UI;
#else
using Windows.UI;
#endif

[assembly: ApiExtension(typeof(IMediaPlayerPresenterExtension), typeof(Uno.UI.Media.MediaPlayerPresenterExtension))]

namespace Uno.UI.Media;

public class MediaPlayerPresenterExtension : IMediaPlayerPresenterExtension
{
	private MediaPlayerPresenter _owner;
	private GtkMediaPlayer _player;

	public uint NaturalVideoHeight => _player.NaturalVideoHeight;
	public uint NaturalVideoWidth => _player.NaturalVideoWidth;

	public MediaPlayerPresenterExtension(object owner)
	{
		if (owner is not MediaPlayerPresenter presenter)
		{
			throw new InvalidOperationException($"MediaPlayerPresenterExtension must be initialized with a MediaPlayer instance");
		}
		_owner = presenter;
		_player = new GtkMediaPlayer(_owner);
		var contentView = new ContentControl();

		contentView.EffectiveViewportChanged += (_, _) => _player.UpdateVideoLayout();

		contentView.Content = _player;
		contentView.VerticalContentAlignment = Windows.UI.Xaml.VerticalAlignment.Stretch;
		contentView.HorizontalContentAlignment = Windows.UI.Xaml.HorizontalAlignment.Stretch;
		contentView.Background = new SolidColorBrush(Colors.Yellow);
		_owner.Child = contentView;
	}

	public void MediaPlayerChanged()
	{
		if (_owner is not null
			&& MediaPlayerExtension.GetByMediaPlayer(_owner.MediaPlayer) is { } extension)
		{
			extension.GtkMediaPlayer = _player;
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

	public void ExitCompactOverlay()
	{
		_player.ExitCompactOverlay();
	}

	public void RequestCompactOverlay()
	{
		_player.RequestCompactOverlay();
	}
}
