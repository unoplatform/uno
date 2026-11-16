#nullable enable

using System;
using System.Threading;
using Windows.Foundation;
using Windows.UI.Core;
using Microsoft.UI.Xaml.Controls;

using Uno.Foundation.Extensibility;
using Uno.Foundation.Logging;
using Uno.UI.Dispatching;
using Uno.UI.NativeElementHosting;

namespace Uno.UI.Runtime.MacOS;

/// <summary>
/// The managed side of a native element: an NSView conforming to <c>UNONativeElement</c>.
/// </summary>
internal interface IMacOSNativeElement
{
	nint NativeHandle { get; }

	bool Detached { get; set; }

	/// <summary>
	/// Set once the native peer has been released, at which point <see cref="NativeHandle"/> is zeroed.
	/// </summary>
	/// <remarks>
	/// <c>uno_native_dispose</c> drops the last strong reference the native side holds on the NSView, so
	/// the object is deallocated. Any later call handing the stale handle back to native code would make
	/// ARC retain freed memory, which crashes the process rather than failing the current operation.
	/// </remarks>
	bool Disposed { get; }
}

internal class MacOSNativeElement : Microsoft.UI.Xaml.FrameworkElement, IMacOSNativeElement
{
	// The peer is tied to the managed wrapper, not to the visual tree: `Unloaded` also fires on a reparent,
	// after which the framework re-enters the very same element through `AttachNativeElement`.
	~MacOSNativeElement() => DisposeNativePeer();

	public nint NativeHandle { get; internal set; }

	public bool Detached { get; set; }

	public bool Disposed { get; private set; }

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
			// AppKit is main-thread only and this runs on the finalizer thread.
			MacOSDispatcher.DispatchNativeSingle(() => NativeUno.uno_native_dispose(handle), NativeDispatcherPriority.Normal);
		}
	}
}

/// <summary>
/// Native container backing a public <see cref="MacOSNativeView"/>, which owns it.
/// </summary>
/// <remarks>
/// Not a <see cref="MacOSNativeElement"/>: it never enters the visual tree, so it has no use for a <c>FrameworkElement</c>.
/// </remarks>
internal sealed class MacOSNativeViewPeer : IMacOSNativeElement, IMacOSNativeViewPeer
{
	private int _disposed;

	private MacOSNativeViewPeer(MacOSNativeView owner)
	{
		NativeHandle = NativeUno.uno_native_create_host(owner.Handle);
	}

	// The wrapper was collected without being disposed.
	~MacOSNativeViewPeer()
	{
		try
		{
			Release();
		}
		catch (Exception e)
		{
			// An exception escaping a finalizer would take the process down.
			if (this.Log().IsEnabled(LogLevel.Error))
			{
				this.Log().Error($"Failed to release the native peer of a {nameof(MacOSNativeView)}.", e);
			}
		}
	}

	public static void Register() => ApiExtensibility.Register<MacOSNativeView>(typeof(IMacOSNativeViewPeer), o => new MacOSNativeViewPeer(o));

	public nint NativeHandle { get; private set; }

	public bool Detached { get; set; }

	public bool Disposed => Volatile.Read(ref _disposed) != 0;

	public bool IsHosting => NativeHandle != 0;

	public void Dispose()
	{
		Release();
		GC.SuppressFinalize(this);
	}

	private void Release()
	{
		if (Interlocked.Exchange(ref _disposed, 1) != 0)
		{
			return;
		}

		var handle = NativeHandle;
		NativeHandle = 0;

		if (handle == 0)
		{
			return;
		}

		// AppKit is main-thread only. Releasing at once when possible lets the app host the view again right away.
		if (NativeDispatcher.Main.HasThreadAccess)
		{
			NativeUno.uno_native_dispose(handle);
		}
		else
		{
			MacOSDispatcher.DispatchNativeSingle(() => NativeUno.uno_native_dispose(handle), NativeDispatcherPriority.Normal);
		}
	}
}

internal class MacOSNativeElementHostingExtension : ContentPresenter.INativeElementHostingExtension
{
	private readonly ContentPresenter _presenter;

	private MacOSNativeElementHostingExtension(ContentPresenter contentPresenter)
	{
		_presenter = contentPresenter;
	}

	public static void Register() => ApiExtensibility.Register<ContentPresenter>(typeof(ContentPresenter.INativeElementHostingExtension), o => new MacOSNativeElementHostingExtension(o));

	/// <summary>
	/// Resolves the window on each attach: the XamlRoot may not be set yet when the extension is created,
	/// and changes when the presenter moves to another window.
	/// </summary>
	private MacOSWindowNative? GetWindow(out string missing)
	{
		if (_presenter.XamlRoot is not { } xamlRoot)
		{
			missing = "its ContentPresenter has no XamlRoot";
			return null;
		}

		if (xamlRoot.HostWindow is not { } hostWindow)
		{
			missing = "its XamlRoot has no host window";
			return null;
		}

		if (hostWindow.NativeWindow is not MacOSWindowNative window)
		{
			missing = $"its window is backed by a {hostWindow.NativeWindow?.GetType().FullName ?? "null"}, not a {nameof(MacOSWindowNative)}";
			return null;
		}

		if (window.Handle == 0)
		{
			missing = "its window was closed";
			return null;
		}

		missing = "";
		return window;
	}

	/// <summary>
	/// Resolves <paramref name="content"/> to a native element that is still safe to hand to native code.
	/// A disposed element keeps its managed wrapper alive but its NSView is gone, so every operation has
	/// to fail on its own rather than hand the stale handle back to ARC.
	/// </summary>
	/// <param name="warn">
	/// Whether a disposed element is worth a warning. Only attaching is: measure, arrange and opacity run on every layout or
	/// <c>Opacity</c> change.
	/// </param>
	private bool TryGetLiveElement(object content, string operation, out IMacOSNativeElement element, bool warn = false)
	{
		var nativeElement = content switch
		{
			MacOSNativeView view => view.Peer as IMacOSNativeElement,
			IMacOSNativeElement e => e,
			_ => null
		};

		if (nativeElement is null)
		{
			if (this.Log().IsEnabled(LogLevel.Debug))
			{
				this.Log().Debug(content is MacOSNativeView
					? $"Cannot {operation} a {nameof(MacOSNativeView)} that has no native peer."
					: $"Cannot {operation} a {content?.GetType().FullName ?? "null"}, which is not a native element.");
			}

			element = null!;
			return false;
		}

		if (nativeElement.Disposed || nativeElement.NativeHandle == 0)
		{
			var level = warn ? LogLevel.Warning : LogLevel.Debug;
			if (this.Log().IsEnabled(level))
			{
				this.Log().Log(level, $"Cannot {operation} a {content.GetType().FullName} whose native peer was already disposed.");
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
		if (TryGetLiveElement(content, "attach", out var element, warn: true))
		{
			if (GetWindow(out var missing) is not { } window)
			{
				if (this.Log().IsEnabled(LogLevel.Warning))
				{
					this.Log().Warn($"Cannot attach a {content.GetType().FullName} as {missing}.");
				}

				return;
			}

			NativeUno.uno_native_attach(element.NativeHandle, window.Handle);
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

	// The view is autoreleased: the wrapper has to take it before the pool drains.
	public object? CreateSampleComponent(string text) => new MacOSNativeView(NativeUno.uno_native_create_sample(text));

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

	public bool IsNativeElement(object content) => content is MacOSNativeElement or MacOSNativeView;

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
