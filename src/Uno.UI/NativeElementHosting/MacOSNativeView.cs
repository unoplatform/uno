#if UNO_REFERENCE_API
#nullable enable
using System;
using Uno.Foundation.Extensibility;

namespace Uno.UI.NativeElementHosting;

/// <summary>
/// Wraps an AppKit <c>NSView</c> so it can be hosted as the <c>Content</c> of a <c>ContentControl</c> on Skia Desktop macOS.
/// </summary>
/// <remarks>
/// The wrapper keeps a strong reference on the native view until it is disposed or garbage-collected.
/// It must be created on the UI thread, and is ignored on platforms other than macOS.
/// </remarks>
public sealed class MacOSNativeView : IDisposable
{
	/// <summary>
	/// Creates a wrapper for the given <c>NSView</c>.
	/// </summary>
	/// <param name="handle">The native <c>NSView*</c> pointer, e.g. <c>NSView.Handle</c>.</param>
	public MacOSNativeView(IntPtr handle)
	{
		if (handle == IntPtr.Zero)
		{
			throw new ArgumentException("The NSView handle cannot be zero.", nameof(handle));
		}

		Handle = handle;

		if (ApiExtensibility.CreateInstance<IMacOSNativeViewPeer>(this, out var peer))
		{
			Peer = peer;
		}
	}

	/// <summary>
	/// The native <c>NSView*</c> pointer that was provided at construction.
	/// </summary>
	public IntPtr Handle { get; }

	/// <summary>
	/// Whether <see cref="Dispose"/> was called. A disposed wrapper can no longer be hosted.
	/// </summary>
	public bool IsDisposed { get; private set; }

	internal IMacOSNativeViewPeer? Peer { get; }

	/// <summary>
	/// Releases the strong reference held on the native view and removes it from its host.
	/// </summary>
	public void Dispose()
	{
		if (!IsDisposed)
		{
			IsDisposed = true;
			Peer?.Dispose();
		}
	}
}

internal interface IMacOSNativeViewPeer : IDisposable
{
}
#endif
