#if UNO_REFERENCE_API
#nullable enable
using System;
using System.Threading;
using Uno.Foundation.Extensibility;
using Uno.UI.Dispatching;

namespace Uno.UI.NativeElementHosting;

/// <summary>
/// Wraps an AppKit <c>NSView</c> so it can be hosted as the <c>Content</c> of a <c>ContentControl</c> on Skia Desktop macOS.
/// </summary>
/// <remarks>
/// The wrapper keeps a strong reference on the native view until it is disposed or garbage-collected.
/// It must be created on the UI thread. Only Skia Desktop macOS hosts it: elsewhere, a <c>ContentControl</c> shows it like
/// any other non-UI content.
/// </remarks>
public sealed class MacOSNativeView : IDisposable
{
	private int _disposed;

	/// <summary>
	/// Creates a wrapper for the given <c>NSView</c>.
	/// </summary>
	/// <param name="handle">The native <c>NSView*</c> pointer, e.g. <c>NSView.Handle</c>.</param>
	/// <exception cref="ArgumentException"><paramref name="handle"/> is zero, or is not an <c>NSView</c>.</exception>
	/// <exception cref="InvalidOperationException">The constructor is not called on the UI thread.</exception>
	public MacOSNativeView(IntPtr handle)
	{
		if (handle == IntPtr.Zero)
		{
			throw new ArgumentException("The NSView handle cannot be zero.", nameof(handle));
		}

		// Wrapping takes the view out of its current parent: AppKit is main-thread only.
		NativeDispatcher.CheckThreadAccess();

		Handle = handle;

		if (ApiExtensibility.CreateInstance<IMacOSNativeViewPeer>(this, out var peer))
		{
			if (!peer.IsHosting)
			{
				peer.Dispose();
				throw new ArgumentException("The handle is not an NSView.", nameof(handle));
			}

			Peer = peer;
		}
	}

	/// <summary>
	/// The native <c>NSView*</c> pointer that was provided at construction.
	/// </summary>
	public IntPtr Handle { get; }

	internal IMacOSNativeViewPeer? Peer { get; }

	/// <summary>
	/// Releases the strong reference held on the native view and removes it from its host.
	/// </summary>
	/// <remarks>
	/// The view is returned to the app with the frame and autoresizing mask it had when it was wrapped, unless it was
	/// moved to another parent in the meantime.
	/// </remarks>
	public void Dispose()
	{
		if (Interlocked.Exchange(ref _disposed, 1) == 0)
		{
			Peer?.Dispose();
		}
	}
}

internal interface IMacOSNativeViewPeer : IDisposable
{
	/// <summary>
	/// Whether the native host was created, which fails when the handle is not an <c>NSView</c>.
	/// </summary>
	bool IsHosting { get; }
}
#endif
