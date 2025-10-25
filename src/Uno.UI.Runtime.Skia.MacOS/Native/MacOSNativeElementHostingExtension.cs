#nullable enable

using Windows.Foundation;
using Windows.UI.Core;
using Microsoft.UI.Xaml.Controls;

using Uno.Foundation.Extensibility;
using Uno.Foundation.Logging;

namespace Uno.UI.Runtime.Skia.MacOS;

internal class MacOSNativeElement : Microsoft.UI.Xaml.FrameworkElement
{
	public nint NativeHandle { get; internal set; }

	internal bool Detached { get; set; }
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

	public void ArrangeNativeElement(object content, Rect arrangeRect)
	{
		if (content is MacOSNativeElement element)
		{
			if (element.Detached)
			{
				this.Log().Debug($"Cannot arrange element `{nameof(content)}` of type {content.GetType().FullName} since it was detached.");
			}
			else
			{
				NativeUno.uno_native_arrange(element.NativeHandle, arrangeRect.Left, arrangeRect.Top, arrangeRect.Width, arrangeRect.Height);
			}
		}
		else if (this.Log().IsEnabled(LogLevel.Debug))
		{
			this.Log().Debug($"Object `{nameof(content)}` is a {content.GetType().FullName} and not a MacOSNativeElement subclass.");
		}
	}

	public void AttachNativeElement(object content)
	{
		if (content is MacOSNativeElement element)
		{
			NativeUno.uno_native_attach(element.NativeHandle);
			element.Detached = false;
		}
		else if (this.Log().IsEnabled(LogLevel.Debug))
		{
			this.Log().Debug($"Object `{nameof(content)}` is a {content.GetType().FullName} and not a MacOSNativeElement subclass.");
		}
	}

	public void ChangeNativeElementOpacity(object content, double opacity)
	{
		if (content is MacOSNativeElement element)
		{
			// https://developer.apple.com/documentation/appkit/nsview/1483560-alphavalue?language=objc
			// note: no marshaling needed as CGFloat is double for 64bits apps
			NativeUno.uno_native_set_opacity(element.NativeHandle, opacity);
		}
		else if (this.Log().IsEnabled(LogLevel.Debug))
		{
			this.Log().Debug($"Object `{nameof(content)}` is a {content.GetType().FullName} and not a MacOSNativeElement subclass.");
		}
	}

	public object? CreateSampleComponent(string text)
	{
		if (_window is null)
		{
			if (this.Log().IsEnabled(LogLevel.Debug))
			{
				this.Log().Debug($"CreateSampleComponent failed as no MacOSWindowNative instance could be found.");
			}
			return null;
		}

		var handle = NativeUno.uno_native_create_sample(_window.Handle, text);
		return new MacOSNativeElement()
		{
			NativeHandle = handle,
			AccessKey = text // FIXME: debug helper, to be removed
		};
	}

	public void DetachNativeElement(object content)
	{
		if (content is MacOSNativeElement element)
		{
			if (element.Detached)
			{
				this.Log().Debug($"Object `{nameof(content)}` of type {content.GetType().FullName} was already detached.");
			}
			else
			{
				NativeUno.uno_native_detach(element.NativeHandle);
				element.Detached = true;
			}
		}
		else if (this.Log().IsEnabled(LogLevel.Debug))
		{
			this.Log().Debug($"Object `{nameof(content)}` is a {content.GetType().FullName} and not a MacOSNativeElement subclass.");
		}
	}

	public bool IsNativeElement(object content) => content is MacOSNativeElement;

	public bool IsNativeElementAttached(object owner, object nativeElement)
	{
		if (nativeElement is MacOSNativeElement element)
		{
			return NativeUno.uno_native_is_attached(element.NativeHandle);
		}
		else if (this.Log().IsEnabled(LogLevel.Debug))
		{
			this.Log().Debug($"Object `{nameof(owner)}` is a {owner.GetType().FullName} and not a MacOSNativeElement subclass.");
		}
		return false;
	}

	public Size MeasureNativeElement(object content, Size childMeasuredSize, Size availableSize)
	{
		if (content is MacOSNativeElement element)
		{
			NativeUno.uno_native_measure(element.NativeHandle, childMeasuredSize.Width, childMeasuredSize.Height, availableSize.Width, availableSize.Height, out var width, out var height);
			return new Size(width, height);
		}
		else if (this.Log().IsEnabled(LogLevel.Debug))
		{
			this.Log().Debug($"Object `{nameof(content)}` is a {content.GetType().FullName} and not a MacOSNativeElement subclass.");
		}
		return Size.Empty;
	}
}
