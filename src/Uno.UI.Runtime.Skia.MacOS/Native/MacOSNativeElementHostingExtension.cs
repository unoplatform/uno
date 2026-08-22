#nullable enable

using Windows.Foundation;
using Windows.UI.Core;
using Microsoft.UI.Xaml.Controls;

using Uno.Foundation.Extensibility;
using Uno.Foundation.Logging;

namespace Uno.UI.Runtime.Skia.MacOS;

internal class MacOSNativeElement : Microsoft.UI.Xaml.FrameworkElement
{
	public MacOSNativeElement()
	{
		Unloaded += (s, e) => DisposeNativePeer();
	}

	public nint NativeHandle { get; internal set; }

	internal bool Detached { get; set; }

	/// <summary>
	/// Set once the native peer has been released, at which point <see cref="NativeHandle"/> is zeroed.
	/// </summary>
	/// <remarks>
	/// <c>uno_native_dispose</c> drops the last strong reference the native side holds on the NSView, so
	/// the object is deallocated. Any later call handing the stale handle back to native code would make
	/// ARC retain freed memory, which crashes the process rather than failing the current operation.
	/// </remarks>
	internal bool Disposed { get; private set; }

	internal void DisposeNativePeer()
	{
		if (Disposed)
		{
			return;
		}

		var handle = NativeHandle;

		Disposed = true;
		NativeHandle = 0;

		if (handle != 0)
		{
			NativeUno.uno_native_dispose(handle);
		}
	}
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

	/// <summary>
	/// Resolves <paramref name="content"/> to a native element that is still safe to hand to native code.
	/// A disposed element keeps its managed wrapper alive but its NSView is gone, and the framework can
	/// still re-enter it into the visual tree (a reparent raises Unloaded then Loaded).
	/// </summary>
	private bool TryGetLiveElement(object content, string operation, out MacOSNativeElement element)
	{
		if (content is not MacOSNativeElement nativeElement)
		{
			if (this.Log().IsEnabled(LogLevel.Debug))
			{
				this.Log().Debug($"Object `content` is a {content?.GetType().FullName ?? "null"} and not a MacOSNativeElement subclass.");
			}

			element = null!;
			return false;
		}

		if (nativeElement.Disposed || nativeElement.NativeHandle == 0)
		{
			if (this.Log().IsEnabled(LogLevel.Warning))
			{
				this.Log().Warn($"Cannot {operation} a {content.GetType().FullName} whose native peer was already disposed.");
			}

			element = null!;
			return false;
		}

		element = nativeElement;
		return true;
	}

	public void ArrangeNativeElement(object content, Rect arrangeRect)
	{
		if (TryGetLiveElement(content, "arrange", out var element))
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
	}

	public void AttachNativeElement(object content)
	{
		if (TryGetLiveElement(content, "attach", out var element))
		{
			NativeUno.uno_native_attach(element.NativeHandle);
			element.Detached = false;
		}
	}

	public void ChangeNativeElementOpacity(object content, double opacity)
	{
		if (TryGetLiveElement(content, "change the opacity of", out var element))
		{
			// https://developer.apple.com/documentation/appkit/nsview/1483560-alphavalue?language=objc
			// note: no marshaling needed as CGFloat is double for 64bits apps
			NativeUno.uno_native_set_opacity(element.NativeHandle, opacity);
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
		if (TryGetLiveElement(content, "detach", out var element))
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
	}

	public bool IsNativeElement(object content) => content is MacOSNativeElement;

	public bool IsNativeElementAttached(object owner, object nativeElement)
	{
		if (TryGetLiveElement(nativeElement, "query the attached state of", out var element))
		{
			return NativeUno.uno_native_is_attached(element.NativeHandle);
		}
		return false;
	}

	public Size MeasureNativeElement(object content, Size childMeasuredSize, Size availableSize)
	{
		if (TryGetLiveElement(content, "measure", out var element))
		{
			NativeUno.uno_native_measure(element.NativeHandle, childMeasuredSize.Width, childMeasuredSize.Height, availableSize.Width, availableSize.Height, out var width, out var height);
			return new Size(width, height);
		}

		// Not Size.Empty: that is (-∞, -∞), which ContentPresenter does not clamp and Layouter rejects.
		return new Size(0, 0);
	}
}
