using System;
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

	public AndroidSkiaXamlRootHost(XamlRoot xamlRoot)
	{
		_choreographer = Choreographer.Instance!;
		_vsyncCallback = new VsyncCallback(OnVsyncFrame);

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

	void IXamlRootHost.ScheduleFrameCallback(Action callback)
	{
		_pendingVsyncAction = callback;
		if (!_vsyncPosted)
		{
			_vsyncPosted = true;
			_choreographer.PostFrameCallback(_vsyncCallback);
		}
	}

	private void OnVsyncFrame()
	{
		_vsyncPosted = false;
		var action = _pendingVsyncAction;
		_pendingVsyncAction = null;
		action?.Invoke();
	}

	private sealed class VsyncCallback : Java.Lang.Object, Choreographer.IFrameCallback
	{
		private readonly Action _action;

		public VsyncCallback(Action action)
		{
			_action = action;
		}

		public void DoFrame(long frameTimeNanos)
		{
			_action();
		}
	}
}
