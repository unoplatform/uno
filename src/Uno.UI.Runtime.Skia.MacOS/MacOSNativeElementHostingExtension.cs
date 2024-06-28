#nullable enable

using Windows.Foundation;
using Windows.UI.Core;
using Microsoft.UI.Xaml.Controls;

using Uno.Foundation.Extensibility;
using Uno.Foundation.Logging;

namespace Uno.UI.Runtime.Skia.MacOS;

internal class MacOSNativeElement
{
	internal MacOSNativeElement(nint handle)
	{
		Handle = handle;
	}

	public nint Handle { get; private set; }
}

internal class MacOSNativeElementHostingExtension : ContentPresenter.INativeElementHostingExtension
{
	private readonly ContentPresenter _presenter;
	private readonly MacOSWindowNative? _window;

	private MacOSNativeElementHostingExtension(ContentPresenter contentPresenter)
	{
		_presenter = contentPresenter;
		_window = _presenter.XamlRoot?.HostWindow?.NativeWindow as MacOSWindowNative;
	}

	public static void Register() => ApiExtensibility.Register<ContentPresenter>(typeof(ContentPresenter.INativeElementHostingExtension), o => new MacOSNativeElementHostingExtension(o));

	public void ArrangeNativeElement(object content, Rect arrangeRect, Rect clipRect)
	{
		if (content is MacOSNativeElement element)
		{
			// TODO uno_native_arrange(element.Handle, arrangeRect.Left, arrangeRect.Top, arrangeRect.Width, arrangeRect.Height, clipRect.Left, clipRect.Top, clipRect.Width, clipRect.Height);
		}
		else
		{
			if (this.Log().IsEnabled(LogLevel.Warning))
			{
				this.Log().Warn($"Object `{nameof(content)}` is a {content.GetType().FullName} and not a MacOSNativeElement subclass.");
			}
		}
	}

	public void AttachNativeElement(object content)
	{
		if (content is MacOSNativeElement element)
		{
			// TODO uno_native_attach(element.Handle);
		}
		else
		{
			if (this.Log().IsEnabled(LogLevel.Warning))
			{
				this.Log().Warn($"Object `{nameof(content)}` is a {content.GetType().FullName} and not a MacOSNativeElement subclass.");
			}
		}
	}

	public void ChangeNativeElementOpacity(object content, double opacity)
	{
		if (content is MacOSNativeElement element)
		{
			// https://developer.apple.com/documentation/appkit/nsview/1483560-alphavalue?language=objc
			// note: no marshaling needed as CGFloat is double for 64bits apps
			NativeUno.uno_native_set_opacity(element.Handle, opacity);
		}
		else
		{
			if (this.Log().IsEnabled(LogLevel.Warning))
			{
				this.Log().Warn($"Object `{nameof(content)}` is a {content.GetType().FullName} and not a MacOSNativeElement subclass.");
			}
		}
	}

	public void ChangeNativeElementVisibility(object content, bool visible)
	{
		if (content is MacOSNativeElement element)
		{
			// https://developer.apple.com/documentation/appkit/nsview/1483369-hidden?language=objc
			NativeUno.uno_native_set_visibility(element.Handle, visible);
		}
		else
		{
			if (this.Log().IsEnabled(LogLevel.Warning))
			{
				this.Log().Warn($"Object `{nameof(content)}` is a {content.GetType().FullName} and not a MacOSNativeElement subclass.");
			}
		}
	}

	public object? CreateSampleComponent(string text)
	{
		var handle = NativeUno.uno_native_create_sample(_window!.Handle, text);
		return new MacOSNativeElement(handle);
	}

	public void DetachNativeElement(object content)
	{
		if (content is MacOSNativeElement element)
		{
			// TODO uno_native_detach(element.Handle);
		}
		else
		{
			if (this.Log().IsEnabled(LogLevel.Warning))
			{
				this.Log().Warn($"Object `{nameof(content)}` is a {content.GetType().FullName} and not a MacOSNativeElement subclass.");
			}
		}
	}

	public bool IsNativeElement(object content) => content is MacOSNativeElement;

	public bool IsNativeElementAttached(object owner, object nativeElement)
	{
		if (nativeElement is MacOSNativeElement element)
		{
			// TODO uno_native_is_attached(element.Handle);
			return false;
		}
		else
		{
			if (this.Log().IsEnabled(LogLevel.Warning))
			{
				this.Log().Warn($"Object `{nameof(owner)}` is a {owner.GetType().FullName} and not a MacOSNativeElement subclass.");
			}
			return false;
		}
	}

	public Size MeasureNativeElement(object content, Size childMeasuredSize, Size availableSize)
	{
		if (content is MacOSNativeElement element)
		{
			NativeUno.uno_native_measure(element.Handle, childMeasuredSize.Width, childMeasuredSize.Height, availableSize.Width, availableSize.Height, out var width, out var height);
			return new Size(width, height);
		}
		else
		{
			if (this.Log().IsEnabled(LogLevel.Warning))
			{
				this.Log().Warn($"Object `{nameof(content)}` is a {content.GetType().FullName} and not a MacOSNativeElement subclass.");
			}
			return Size.Empty;
		}
	}
}
