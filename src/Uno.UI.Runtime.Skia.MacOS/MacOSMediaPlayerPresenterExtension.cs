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
				// TODO unhook
			}
			_playerExtension = extension;
			// TODO hook
		}
	}

	public void StretchChanged()
	{
		if (_playerExtension is not null)
		{
			NativeUno.uno_mediaplayer_set_stretch(_playerExtension._nativePlayer, _presenter.Stretch);
		}
	}

	public void RequestFullScreen() => NotImplemented();

	public void ExitFullScreen() => NotImplemented();

	public void RequestCompactOverlay() => NotImplemented();

	public void ExitCompactOverlay() => NotImplemented();

	public uint NaturalVideoWidth => 0;

	public uint NaturalVideoHeight => 0;

	public void NotImplemented([CallerMemberName] string name = "unknown")
	{
		if (this.Log().IsEnabled(LogLevel.Debug))
		{
			this.Log().Debug($"Member {name} is not implemented");
		}
	}
}
