#nullable enable

using System;
using System.Diagnostics;
using System.Threading;
using Android.Views;

namespace Uno.UI.Runtime.Android;

/// <summary>
/// Starts at most one frame per display vsync, from the main looper's <see cref="Choreographer"/>, and hands the
/// render thread the time of the vsync that started it. Idle costs nothing: a callback is only posted on request.
/// </summary>
internal sealed class ChoreographerFramePacer : Java.Lang.Object, Choreographer.IFrameCallback
{
	private readonly Choreographer _choreographer;
	private readonly Action _onVsync;
	private int _callbackPosted;
	private long _vsyncTimestamp;
	private long _lastFrameTimeNanos;
	private long _lastConvertedTimestamp;

	// A frame that starts later than this after its vsync (e.g. a request latched while the surface was gone) is
	// not that vsync's frame, and back-dating it would make the frame clock jump the whole gap.
	private static readonly long MaxVsyncAge = Stopwatch.Frequency / 10;

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
	/// or it is too old to be this frame's (a frame the host drew on its own has no vsync of its own).
	/// </summary>
	public long? TakeVsyncTimestamp()
	{
		var timestamp = Interlocked.Exchange(ref _vsyncTimestamp, 0);
		return timestamp == 0 || Stopwatch.GetTimestamp() - timestamp > MaxVsyncAge ? null : timestamp;
	}

	public void DoFrame(long frameTimeNanos)
	{
		if (frameTimeNanos != _lastFrameTimeNanos)
		{
			// Converted through its age, so nothing assumes the Choreographer clock matches Stopwatch's.
			var ageInNanos = Math.Max(0, Java.Lang.JavaSystem.NanoTime() - frameTimeNanos);
			var timestamp = Stopwatch.GetTimestamp() - (long)(ageInNanos * (Stopwatch.Frequency / 1_000_000_000d));
			_lastConvertedTimestamp = Math.Max(1, timestamp);
			_lastFrameTimeNanos = frameTimeNanos;
		}

		Interlocked.Exchange(ref _vsyncTimestamp, _lastConvertedTimestamp);

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
