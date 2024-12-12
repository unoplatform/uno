#nullable enable

using System.Runtime.CompilerServices;

using Windows.Foundation;
using Windows.Media.Core;
using Windows.Media.Playback;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI.Core;
using Microsoft.UI.Xaml.Controls;

using Uno.Foundation.Extensibility;
using Uno.Foundation.Logging;
using Uno.Media.Playback;

namespace Uno.UI.Runtime.Skia.MacOS;

internal class MacOSMediaPlayerPresenterExtension : IMediaPlayerPresenterExtension
{
	private MediaPlayerPresenter _presenter;
	private MacOSMediaPlayerExtension? _playerExtension;

	private MacOSMediaPlayerPresenterExtension(object owner)
	{
		if (owner is MediaPlayerPresenter presenter)
		{
			_presenter = presenter;
		}
		else
		{
			throw new InvalidOperationException($"MacOSMediaPlayerPresenterExtension must be initialized with a MediaPlayerPresenter instance");
		}
	}

	public static void Register() => ApiExtensibility.Register(typeof(IMediaPlayerPresenterExtension), o => new MacOSMediaPlayerPresenterExtension(o));

	public void MediaPlayerChanged()
	{
		if (MacOSMediaPlayerExtension.GetByMediaPlayer(_presenter.MediaPlayer) is { } extension)
		{
			if (_playerExtension is { })
			{
				// _playerExtension.VlcPlayer.XWindow = 0;
				// _playerExtension.IsVideoChanged -= OnExtensionOnIsVideoChanged;
				// _playerExtension.Player.PlaybackSession.PlaybackStateChanged -= OnPlaybackStateChanged;
			}
			_playerExtension = extension;
			// _playerExtension.VlcPlayer.XWindow = (uint)(_x11Window.Window);
			// _playerExtension.IsVideoChanged += OnExtensionOnIsVideoChanged;
			// _playerExtension.Player.PlaybackSession.PlaybackStateChanged += OnPlaybackStateChanged;
		}
	}

	public void StretchChanged() => NotImplemented();

	public void RequestFullScreen() => NotImplemented();

	public void ExitFullScreen() => NotImplemented();

	public void RequestCompactOverlay() => NotImplemented();

	public void ExitCompactOverlay() => NotImplemented();

	public uint NaturalVideoHeight { get; }

	public uint NaturalVideoWidth { get; }

	public void NotImplemented([CallerMemberName] string name = "unknown")
	{
		if (this.Log().IsEnabled(LogLevel.Debug))
		{
			this.Log().Debug($"Member {name} is not implemented");
		}
	}
}
