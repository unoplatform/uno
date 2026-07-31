#nullable enable

using System;
using System.Threading;
using Android.OS;
using Android.Views;
using Uno.Foundation.Logging;

namespace Uno.UI.Runtime.Skia.Android;

/// <summary>
/// Blocks a render thread until the next display vsync, the Android counterpart of
/// <c>Win32RenderPacer</c>.
/// </summary>
/// <remarks>
/// The Vulkan swapchain uses <c>VK_PRESENT_MODE_MAILBOX_KHR</c>, whose present returns without
/// waiting so that latency stays low and the frame is never torn. That leaves the render thread with
/// nothing pacing it, so it free-runs and presentation lands at uneven times even when production is
/// healthy. Win32 solves this with <c>DwmFlush</c>; Choreographer is the equivalent signal here.
/// <para>
/// Choreographer is per-thread and needs a Looper, which a bare render thread does not have, so the
/// pacer owns a small Looper thread of its own. Frame callbacks are posted only while someone is
/// waiting, so an idle app does not wake once per vsync.
/// </para>
/// </remarks>
internal sealed class ChoreographerFramePacer : IDisposable
{
	// A vsync should never be more than a frame or two away; past this the app is not being composited
	// (backgrounded, surface gone) and the render thread must not be held hostage.
	private static readonly TimeSpan MaxWait = TimeSpan.FromMilliseconds(100);

	private readonly AutoResetEvent _vsync = new(false);
	private readonly ManualResetEventSlim _ready = new(false);
	private readonly Thread _thread;

	private Handler? _handler;
	private Choreographer? _choreographer;
	private FrameCallback? _callback;
	private volatile bool _disposed;

	public ChoreographerFramePacer()
	{
		_thread = new Thread(Run) { Name = "UnoVsyncPacer", IsBackground = true };
		_thread.Start();
	}

	private void Run()
	{
		try
		{
			Looper.Prepare();
			_handler = new Handler(Looper.MyLooper()!);
			_choreographer = Choreographer.Instance;
			_callback = new FrameCallback(() => _vsync.Set());
			_ready.Set();
			Looper.Loop();
		}
		catch (Exception e)
		{
			this.LogError()?.Error($"Vsync pacer thread failed; rendering will not be paced: {e}");
			_ready.Set();
		}
	}

	/// <summary>Blocks until the next vsync, or returns after <see cref="MaxWait"/> if none arrives.</summary>
	public void WaitForNextFrame()
	{
		if (_disposed || !_ready.Wait(MaxWait) || _handler is null)
		{
			return;
		}

		// PostFrameCallback must run on the Choreographer's own thread.
		_handler.Post(() => _choreographer?.PostFrameCallback(_callback!));
		_vsync.WaitOne(MaxWait);
	}

	public void Dispose()
	{
		if (_disposed)
		{
			return;
		}

		_disposed = true;
		_vsync.Set();
		_handler?.Post(() =>
		{
			_choreographer?.RemoveFrameCallback(_callback!);
			Looper.MyLooper()?.Quit();
		});

		_vsync.Dispose();
		_ready.Dispose();
	}

	private sealed class FrameCallback(Action onFrame) : Java.Lang.Object, Choreographer.IFrameCallback
	{
		public void DoFrame(long frameTimeNanos) => onFrame();
	}
}
