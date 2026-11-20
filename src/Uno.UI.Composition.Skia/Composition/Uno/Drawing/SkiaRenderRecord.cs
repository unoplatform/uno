#nullable enable

using System;
using System.Collections.Concurrent;
using System.Threading;
using SkiaSharp;

namespace Uno.UI.Composition.Drawing;

/// <summary>SkiaSharp-backed <see cref="IRenderRecord"/> holding a native <c>SKPicture</c> handle.</summary>
internal sealed class SkiaRenderRecord : IRenderRecord
{
	public SkiaRenderRecord(IntPtr picture) => _picture = picture;

	/// <summary>
	/// Keeps the managed picture alive so it can be handed out as frame data. SkiaSharp has no public way to wrap
	/// an existing handle, so the managed object has to be the one EndRecording produced.
	/// </summary>
	public SkiaRenderRecord(SKPicture picture)
	{
		_managed = picture;
		_picture = picture.Handle;
	}

	// The owned native SKPicture handle; IntPtr.Zero once disposed, or if nothing was recorded.
	private IntPtr _picture;
	private SKPicture? _managed;

	public object? FrameData => _managed;

	// Backend-bound: an SKPicture only replays onto an SKCanvas, so `into` must be a Skia session (guaranteed by
	// the single-registered-backend invariant). The cast is the backend recognizing its own session type.
	public void Replay(IDrawingSession into)
	{
		if (_picture != IntPtr.Zero && into is SkiaDrawingSession session)
		{
			unsafe
			{
				UnoSkiaApi.sk_canvas_draw_picture(session.Canvas.Handle, _picture, null, IntPtr.Zero);
			}
		}
	}

	public void Dispose()
	{
		if (_managed is { } managed)
		{
			// The managed wrapper owns the same handle, so releasing it twice would over-unref.
			_managed = null;
			_picture = IntPtr.Zero;
			managed.Dispose();
			return;
		}

		if (_picture != IntPtr.Zero)
		{
			ReleaseLater(_picture);
			_picture = IntPtr.Zero;
		}
	}

	// Destroying a picture frees its whole recording, which costs layout a lot when resizes invalidate many visuals at
	// once. Pictures are immutable, atomically ref-counted and hold no GPU resources, and frames are already released
	// from the render thread, so the last reference is dropped on a thread of its own. While releases keep coming it
	// polls, so the releasing thread pays no signal; only an idle drain sleeps on the event and has to be woken.
	private static readonly ConcurrentQueue<IntPtr> _pendingReleases = new();
	private static readonly AutoResetEvent _releasesPending = new(false);
	private static int _releaseThreadStarted;
	private static int _releaseThreadSleeping;

	private static void ReleaseLater(IntPtr picture)
	{
		if (OperatingSystem.IsBrowser())
		{
			UnoSkiaApi.sk_refcnt_safe_unref(picture);
			return;
		}

		_pendingReleases.Enqueue(picture);
		if (Interlocked.Exchange(ref _releaseThreadStarted, 1) == 0)
		{
			new Thread(DrainReleases) { IsBackground = true, Name = "Uno picture release" }.Start();
		}
		else if (Volatile.Read(ref _releaseThreadSleeping) == 1 && Interlocked.Exchange(ref _releaseThreadSleeping, 0) == 1)
		{
			_releasesPending.Set();
		}
	}

	private static void DrainReleases()
	{
		while (true)
		{
			while (_pendingReleases.TryDequeue(out var picture))
			{
				UnoSkiaApi.sk_refcnt_safe_unref(picture);
			}

			Thread.Sleep(8);
			if (!_pendingReleases.IsEmpty)
			{
				continue;
			}

			// Announce the sleep before the last look, so a release enqueued in between either is seen here or wakes us.
			Interlocked.Exchange(ref _releaseThreadSleeping, 1);
			if (!_pendingReleases.IsEmpty && Interlocked.Exchange(ref _releaseThreadSleeping, 0) == 1)
			{
				continue;
			}

			_releasesPending.WaitOne();
		}
	}
}
