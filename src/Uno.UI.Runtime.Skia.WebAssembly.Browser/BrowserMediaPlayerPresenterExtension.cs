#nullable enable

using System;
using System.Runtime.InteropServices.JavaScript;
using Windows.Foundation;
using Microsoft.UI.Xaml.Controls;

namespace Uno.UI.Runtime.Skia;

internal partial class BrowserMediaPlayerPresenterExtension : IMediaPlayerPresenterExtension
{
	private readonly MediaPlayerPresenter _presenter;
	private SkiaWasmHtmlElement? _htmlElement;

	public BrowserMediaPlayerPresenterExtension(MediaPlayerPresenter presenter)
	{
		_presenter = presenter;
	}

	public void MediaPlayerChanged()
	{
		if (BrowserMediaPlayerExtension.GetByMediaPlayer(_presenter.MediaPlayer) is { } extension)
		{
			_presenter.Child = new MyClass
			{
				Content = _htmlElement = extension.HtmlElement
			};
		}
	}
	
	private class MyClass : ContentControl
	{
		protected override Size MeasureOverride(Size availableSize)
		{
			var @out = base.MeasureOverride(availableSize);
			Console.WriteLine($"ramez MeasureOverride {availableSize} {@out}");
			return @out;
		}

		protected override Size ArrangeOverride(Size finalSize)
		{
			var @out = base.ArrangeOverride(finalSize);
			Console.WriteLine($"ramez ArrangeOverride {finalSize} {@out}");
			return @out;
		}
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
	}
}
