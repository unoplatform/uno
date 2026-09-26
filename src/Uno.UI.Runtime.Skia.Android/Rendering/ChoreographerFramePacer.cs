#nullable enable

using System;
using System.Threading;
using Android.Views;

namespace Uno.UI.Runtime.Skia.Android;

/// <summary>
/// Releases at most one frame per display vsync, from the main looper's <see cref="Choreographer"/>. For render
/// loops whose present never blocks (a Vulkan MAILBOX swapchain), which would otherwise present as fast as frames
/// can be recorded, far above the refresh rate.
/// </summary>
internal sealed class ChoreographerFramePacer : Java.Lang.Object, Choreographer.IFrameCallback
{
	private readonly Choreographer _choreographer;
	private readonly Action<long> _onVsync;
	private int _callbackPosted;

	/// <param name="onVsync">Invoked on the main thread with the frame's vsync time (<see cref="Java.Lang.JavaSystem.NanoTime"/> base).</param>
	/// <remarks>Must be created on the main thread: <see cref="Choreographer.Instance"/> is per looper.</remarks>
	public ChoreographerFramePacer(Action<long> onVsync)
	{
		_choreographer = Choreographer.Instance ?? throw new InvalidOperationException("No Choreographer on this thread.");
		_onVsync = onVsync;
	}

	/// <summary>Asks for a callback at the next vsync; thread safe, and repeated requests before it fires coalesce.</summary>
	public void RequestFrame()
	{
		if (Interlocked.Exchange(ref _callbackPosted, 1) == 0)
		{
			_choreographer.PostFrameCallback(this);
		}
	}

	public void DoFrame(long frameTimeNanos)
	{
		Volatile.Write(ref _callbackPosted, 0);
		_onVsync(frameTimeNanos);
	}
}
