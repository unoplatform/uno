#nullable enable

using System.Runtime.CompilerServices;

using Windows.Foundation;
using Windows.Media.Core;
using Windows.Media.Playback;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI.Core;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

using Uno.Foundation.Extensibility;
using Uno.Foundation.Logging;
using Uno.Media.Playback;

namespace Uno.UI.Runtime.Skia.MacOS;

internal class MacOSMediaPlayerPresenterExtension : IMediaPlayerPresenterExtension
{
	private readonly MediaPlayerPresenter _presenter;
	private MacOSMediaPlayerExtension? _playerExtension;
	private readonly nint _nativeView;

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
			// FIXME: on a fullscreen view `SetOwner` is called later ?!? which means we can't get the `nativeWindow` and move the view :(
			var nativeWindow = (_presenter.XamlRoot?.HostWindow?.NativeWindow as MacOSWindowNative);
			if (nativeWindow is not null)
			{
				NativeUno.uno_mediaplayer_set_view(_playerExtension._nativePlayer, _nativeView, nativeWindow.Handle);
			}
		}
	}

	public void StretchChanged()
	{
		if (_playerExtension is not null)
		{
			NativeUno.uno_mediaplayer_set_stretch(_playerExtension._nativePlayer, _presenter.Stretch);
		}
	}

	// multiple window could be fullscreen on different screens
	static HashSet<MediaPlayerPresenter> fullscreen = new();

	public void RequestFullScreen()
	{
		fullscreen.Add(_presenter);
	}

	public void ExitFullScreen()
	{
		fullscreen.Remove(_presenter);
	}

	// called when `Esc` is used to get out of fullscreen mode
	internal static void OnEscapingFullScreen()
	{
		foreach (var presenter in fullscreen)
		{
			presenter.IsFullWindow = false;
			if (presenter.GetLayoutOwner() is MediaPlayerElement mpe)
			{
				mpe.IsFullWindow = false;
			}
		}
		fullscreen.Clear();
	}

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
