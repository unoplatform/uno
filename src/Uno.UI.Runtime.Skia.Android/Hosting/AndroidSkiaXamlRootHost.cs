using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Uno.Foundation.Extensibility;
using Uno.UI.Hosting;
using Uno.UI.Xaml.Controls;

using Choreographer = Android.Views.Choreographer;

namespace Uno.UI.Runtime.Skia.Android;

internal class AndroidSkiaXamlRootHost : IXamlRootHost
{
	private readonly Choreographer _choreographer;
	private readonly VsyncCallback _vsyncCallback;
	private Action? _pendingVsyncAction;
	private bool _vsyncPosted;

	/// <summary>
	/// The Choreographer's frameTimeNanos from the most recent VSync callback.
	/// On .NET Android, this is nanoseconds from CLOCK_MONOTONIC — the same clock
	/// as <see cref="Stopwatch.GetTimestamp"/> (Stopwatch.Frequency == 1_000_000_000).
	/// </summary>
	private long _lastFrameTimeNanos;

	public AndroidSkiaXamlRootHost(XamlRoot xamlRoot)
	{
		_choreographer = Choreographer.Instance!;
		_vsyncCallback = new VsyncCallback(this);

		XamlRootMap.Register(xamlRoot, this);
	}

	void IXamlRootHost.InvalidateRender()
	{
		ApplicationActivity.Instance?.InvalidateRender();
	}

	// Choreographer-based ScheduleFrameCallback already limits FrameTick to 1 per vsync,
	// so the render throttle is unnecessary. Disabling it avoids an extra round-trip
	// (OnFramePresented → ScheduleFrameTick) that halves the effective frame rate.
	bool IXamlRootHost.SupportsRenderThrottle => false;

	UIElement? IXamlRootHost.RootElement => Window.Current!.RootElement;

	long IXamlRootHost.FrameVsyncTimestamp => _lastFrameTimeNanos;

	void IXamlRootHost.ScheduleFrameCallback(Action callback)
	{
		Debug.Assert(
			_pendingVsyncAction is null || _pendingVsyncAction == callback,
			"ScheduleFrameCallback called with a different callback while one is already pending. " +
			"Only one FrameTick should be scheduled per VSync interval.");

		_pendingVsyncAction = callback;
		if (!_vsyncPosted)
		{
			_vsyncPosted = true;
			_choreographer.PostFrameCallback(_vsyncCallback);
		}
	}

	private void OnVsyncFrame(long frameTimeNanos)
	{
		_lastFrameTimeNanos = frameTimeNanos;
		_vsyncPosted = false;
		var action = _pendingVsyncAction;
		_pendingVsyncAction = null;
		action?.Invoke();
	}

	private sealed class VsyncCallback : Java.Lang.Object, Choreographer.IFrameCallback
	{
		private readonly AndroidSkiaXamlRootHost _host;

		public VsyncCallback(AndroidSkiaXamlRootHost host)
		{
			_host = host;
		}

		public void DoFrame(long frameTimeNanos)
		{
			_host.OnVsyncFrame(frameTimeNanos);
		}
	}
}
