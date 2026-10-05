#nullable enable

using System;
using System.Diagnostics;
using System.Threading;
using Android.Views;

namespace Uno.UI.Runtime.Android;

/// <summary>
/// Releases at most one frame per display vsync, from the main looper's <see cref="Choreographer"/>. For render
/// loops whose present never blocks (a Vulkan MAILBOX swapchain), which would otherwise present as fast as frames
/// can be recorded, far above the refresh rate.
/// </summary>
internal sealed class ChoreographerFramePacer : Java.Lang.Object, Choreographer.IFrameCallback
{
	private readonly Choreographer _choreographer;
	private readonly Action _onVsync;
	private int _callbackPosted;
	private long _vsyncTimestamp;

	/// <param name="onVsync">Invoked on the main thread once the frame's vsync time is available from <see cref="TakeVsyncTimestamp"/>.</param>
	/// <remarks>Must be created on the main thread: <see cref="Choreographer.Instance"/> is per looper.</remarks>
	public ChoreographerFramePacer(Action onVsync)
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

	/// <summary>
	/// The <see cref="Stopwatch.GetTimestamp"/> time of the latest vsync, or null if none arrived since the last call
	/// (a frame the host drew on its own, e.g. after a surface change, has no vsync of its own).
	/// </summary>
	public long? TakeVsyncTimestamp()
	{
		var timestamp = Interlocked.Exchange(ref _vsyncTimestamp, 0);
		return timestamp == 0 ? null : timestamp;
	}

	public void DoFrame(long frameTimeNanos)
	{
		// Converted through its age, so nothing assumes the Choreographer clock matches Stopwatch's.
		var ageInNanos = Math.Max(0, Java.Lang.JavaSystem.NanoTime() - frameTimeNanos);
		var timestamp = Stopwatch.GetTimestamp() - (long)(ageInNanos * (Stopwatch.Frequency / 1_000_000_000d));
		Interlocked.Exchange(ref _vsyncTimestamp, Math.Max(1, timestamp));

		Volatile.Write(ref _callbackPosted, 0);
		_onVsync();
	}

	protected override void Dispose(bool disposing)
	{
		if (disposing)
		{
			_choreographer.RemoveFrameCallback(this);
		}

		base.Dispose(disposing);
	}
}
