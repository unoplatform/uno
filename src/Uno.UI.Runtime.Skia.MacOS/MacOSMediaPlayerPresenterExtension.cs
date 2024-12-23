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
	private nint _nativeView;

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

		_nativeView = NativeUno.uno_mediaplayer_create_view();
		var native = new MacOSNativeElement()
		{
			NativeHandle = _nativeView
		};

		var cp = new ContentPresenter() { Content = native };
		_presenter.Child = cp;
		cp.SizeChanged += (_, _) => StretchChanged();
	}

	public static void Register() => ApiExtensibility.Register(typeof(IMediaPlayerPresenterExtension), o => new MacOSMediaPlayerPresenterExtension(o));

	public void MediaPlayerChanged()
	{
		if (MacOSMediaPlayerExtension.GetByMediaPlayer(_presenter.MediaPlayer) is { } extension)
		{
			extension._presenter = this;
			if (_playerExtension is { })
			{
				// TODO unhook
			}
			_playerExtension = extension;
			// set native drawing destination
			var nativeWindow = (_presenter.XamlRoot?.HostWindow?.NativeWindow as MacOSWindowNative);
			NativeUno.uno_mediaplayer_set_view(_playerExtension._nativePlayer, _nativeView, nativeWindow is null ? 0 : nativeWindow.Handle);
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

	public uint NaturalVideoWidth { get; set; }

	public uint NaturalVideoHeight { get; set; }

	public void NotImplemented([CallerMemberName] string name = "unknown")
	{
		if (this.Log().IsEnabled(LogLevel.Debug))
		{
			this.Log().Debug($"Member {name} is not implemented");
		}
	}
}
