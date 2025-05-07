#nullable enable

using System;
using System.Collections.Concurrent;
using System.Runtime.InteropServices.JavaScript;
using Windows.Foundation;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Uno.UI.NativeElementHosting;

namespace Uno.UI.Runtime.Skia;

internal partial class BrowserMediaPlayerPresenterExtension : IMediaPlayerPresenterExtension
{
	private static readonly ConcurrentDictionary<string, BrowserMediaPlayerPresenterExtension> _elementIdToMediaPlayerPresenter = new();

	private readonly MediaPlayerPresenter _presenter;
	private BrowserHtmlElement? _htmlElement;
	private BrowserMediaPlayerExtension? _playerExtension;

	public BrowserMediaPlayerPresenterExtension(MediaPlayerPresenter presenter)
	{
		NativeMethods.BuildImports();
		_presenter = presenter;
	}

	~BrowserMediaPlayerPresenterExtension()
	{
		if (_htmlElement is { })
		{
			_elementIdToMediaPlayerPresenter.TryRemove(_htmlElement.ElementId, out _);
		}
	}

	public void MediaPlayerChanged()
	{
		if (BrowserMediaPlayerExtension.GetByMediaPlayer(_presenter.MediaPlayer) is { } extension)
		{
			if (_playerExtension is { })
			{
				_playerExtension.IsVideoChanged -= OnExtensionOnIsVideoChanged;
			}
			_playerExtension = extension;
			extension.IsVideoChanged += OnExtensionOnIsVideoChanged;

			_presenter.Child = new ContentPresenter
			{
				Content = _htmlElement = extension.HtmlElement
			};

			NativeMethods.SetupEvents(_htmlElement.ElementId);

			_elementIdToMediaPlayerPresenter.TryAdd(_htmlElement.ElementId, this);
		}
	}

	private void OnExtensionOnIsVideoChanged(object? sender, bool? args)
	{
		_presenter.Child.Visibility = args ?? false ? Visibility.Visible : Visibility.Collapsed;
	}

	public void StretchChanged()
	{
		if (_htmlElement is { })
		{
			NativeMethods.UpdateStretch(_htmlElement.ElementId, _presenter.Stretch.ToString());
		}
	}

	public void RequestFullScreen()
	{
		if (_htmlElement is { })
		{
			NativeMethods.RequestFullscreen(_htmlElement.ElementId);
		}
	}

	public void ExitFullScreen()
	{
		if (_htmlElement is { })
		{
			NativeMethods.ExitFullscreen(_htmlElement.ElementId);
		}
	}

	public void RequestCompactOverlay()
	{
		if (_htmlElement is { })
		{
			NativeMethods.RequestPictureInPicture(_htmlElement.ElementId);
		}
	}

	public void ExitCompactOverlay()
	{
		if (_htmlElement is { })
		{
			NativeMethods.ExitPictureInPicture(_htmlElement.ElementId);
		}
	}

	[JSExport]
	private static void OnExitFullscreen(string id)
	{
		if (_elementIdToMediaPlayerPresenter.TryGetValue(id, out var @this))
		{
			if (@this._presenter.TemplatedParent is MediaPlayerElement mpe)
			{
				mpe.IsFullWindow = false;
			}
		}
	}

	public uint NaturalVideoHeight => _htmlElement is { } ? (uint)NativeMethods.GetVideoNaturalHeight(_htmlElement.ElementId) : 0;

	public uint NaturalVideoWidth => _htmlElement is { } ? (uint)NativeMethods.GetVideoNaturalWidth(_htmlElement.ElementId) : 0;

	private static partial class NativeMethods
	{
		[JSImport($"globalThis.Uno.UI.Runtime.Skia.{nameof(BrowserMediaPlayerPresenterExtension)}.getVideoNaturalHeight")]
		public static partial int GetVideoNaturalHeight(string elementId);
		[JSImport($"globalThis.Uno.UI.Runtime.Skia.{nameof(BrowserMediaPlayerPresenterExtension)}.getVideoNaturalWidth")]
		public static partial int GetVideoNaturalWidth(string elementId);
		[JSImport($"globalThis.Uno.UI.Runtime.Skia.{nameof(BrowserMediaPlayerPresenterExtension)}.requestFullscreen")]
		public static partial void RequestFullscreen(string elementId);
		[JSImport($"globalThis.Uno.UI.Runtime.Skia.{nameof(BrowserMediaPlayerPresenterExtension)}.exitFullscreen")]
		public static partial void ExitFullscreen(string elementId);
		[JSImport($"globalThis.Uno.UI.Runtime.Skia.{nameof(BrowserMediaPlayerPresenterExtension)}.requestPictureInPicture")]
		public static partial void RequestPictureInPicture(string elementId);
		[JSImport($"globalThis.Uno.UI.Runtime.Skia.{nameof(BrowserMediaPlayerPresenterExtension)}.exitPictureInPicture")]
		public static partial void ExitPictureInPicture(string elementId);
		[JSImport($"globalThis.Uno.UI.Runtime.Skia.{nameof(BrowserMediaPlayerPresenterExtension)}.updateStretch")]
		public static partial void UpdateStretch(string elementId, string stretch);
		[JSImport($"globalThis.Uno.UI.Runtime.Skia.{nameof(BrowserMediaPlayerPresenterExtension)}.setupEvents")]
		public static partial void SetupEvents(string elementId);
		[JSImport($"globalThis.Uno.UI.Runtime.Skia.{nameof(BrowserMediaPlayerPresenterExtension)}.buildImports")]
		public static partial void BuildImports();
	}
}
