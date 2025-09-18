#nullable enable
using System;
using System.Diagnostics;
using System.Threading;
using Windows.Foundation;
using SkiaSharp;
using Uno.Foundation.Logging;
using Uno.UI.Composition;
using Uno.UI.Dispatching;
using Uno.UI.Helpers;
using Uno.UI.Hosting;

namespace Microsoft.UI.Xaml.Media;

public partial class CompositionTarget
{
	//                      +---------+            +-------------------------------------------+                                                                                   +---------------+ +-----------------------------+        +-------------------+
	//                      | Visual  |            | CompositionTargetNotNecessarilyOnUIThread |                                                                                   | IXamlRootHost | | CompositionTargetOnUIThread |        | NativeDispatcher  |
	//                      +---------+            +-------------------------------------------+                                                                                   +---------------+ +-----------------------------+        +-------------------+
	// ------------------------\ |                                       |                                                                                                                 |                        |                                 |
	// | some property changes |-|                                       |                                                                                                                 |                        |                                 |
	// |-----------------------| |                                       |                                                                                                                 |                        |                                 |
	//                           |                                       |                                                                                                                 |                        |                                 |
	//                           | RequestNewFrame                       |                                                                                                                 |                        |                                 |
	//                           |-------------------------------------->|                                                                                                                 |                        |                                 |
	//                           |                                       |                                                                                                                 |                        |                                 |
	//                           |                                       | InvalidateRender (the native platform will later call us back likely on the next monitor VSync)                 |                        |                                 |
	//                           |                                       |---------------------------------------------------------------------------------------------------------------->|                        |                                 |
	// ------------------------\ |                                       |                                                                                                                 |                        |                                 |
	// | some property changes |-|                                       |                                                                                                                 |                        |                                 |
	// |-----------------------| |                                       |                                                                                                                 |                        |                                 |
	//                           |                                       |                                                                                                                 |                        |                                 |
	//                           | RequestNewFrame                       |                                                                                                                 |                        |                                 |
	//                           |-------------------------------------->|                                                                                                                 |                        |                                 |
	//                           |        -----------------------------\ |                                                                                                                 |                        |                                 |
	//                           |        | RequestNewFrame is ignored |-|                                                                                                                 |                        |                                 |
	//                           |        |----------------------------| |                                                                                                                 |                        |                                 |
	//                           |                                       |                                                                                                                 |                        |                                 |
	//                           | RequestNewFrame                       |                                                                                                                 |                        |                                 |
	//                           |-------------------------------------->|                                                                                                                 |                        |                                 |
	//                           |        -----------------------------\ |                                                                                                                 |                        |                                 |
	//                           |        | RequestNewFrame is ignored |-|                                                                                                                 |                        |                                 |
	//                           |        |----------------------------| |                                                                                                                 |                        |                                 |
	//                           |                                       |                           ------------------------------------------------------------------------------------\ |                        |                                 |
	//                           |                                       |                           | native platform render callback in response to the previous InvalidateRender call |-|                        |                                 |
	//                           |                                       |                           |-----------------------------------------------------------------------------------| |                        |                                 |
	//                           |                                       |                                                                                                                 |                        |                                 |
	//                           |                                       |                                                                                  OnNativePlatformFrameRequested |                        |                                 |
	//                           |                                       |<----------------------------------------------------------------------------------------------------------------|                        |                                 |
	//                           |                                       |                                                                                                                 |                        |                                 |
	//                           |                                       | Draw the pixels from the last SKPicture and returns native element clip path pair generated in Render()         |                        |                                 |
	//                           |                                       |---------------------------------------------------------------------------------------------------------------->|                        |                                 |
	//                           |                                       |                                                                                                                 |                        |                                 |
	//                           |                                       | EnqueueRender (the NativeDispatcher will call us back when it thinks it's the best time to do so)               |                        |                                 |
	//                           |                                       |--------------------------------------------------------------------------------------------------------------------------------------------------------------------------->|
	//                           |                                       |                                                                                                                 |                        |                                 |
	//                           |                                       |                                                                                                                 |                        |           EnqueueRenderCallback |
	//                           |                                       |                                                                                                                 |                        |<--------------------------------|
	//                           |                                       |                                                                                                                 |           -----------\ |                                 |
	//                           |                                       |                                                                                                                 |           | Render() |-|                                 |
	//                           |                                       |                                                                                                                 |           |----------| |                                 |
	// ------------------------\ |                                       |                                                                                                                 |                        |                                 |
	// | some property changes |-|                                       |                                                                                                                 |                        |                                 |
	// |-----------------------| |                                       |                                                                                                                 |                        |                                 |
	//             ------------\ |                                       |                                                                                                                 |                        |                                 |
	//             | Repeat... |-|                                       |                                                                                                                 |                        |                                 |
	//             |-----------| |                                       |                                                                                                                 |                        |                                 |
	//                           |                                       |                                                                                                                 |                        |                                 |
	private readonly object _renderingStateGate = new();

	private bool _renderRequested; // only set or read under _renderingStateGate
	private bool _renderedAheadOfTime; // only set or read under _renderingStateGate
	private bool _renderRequestedAfterAheadOfTimePaint; // only set or read under _renderingStateGate
	private bool _shouldEnqueueRenderOnNextNativePlatformFrameRequested = true; // only set from the UI thread, only reset from the rendering/gpu thread

	private bool RenderRequested
	{
		get => _renderRequested;
		set
		{
			_renderRequested = value;
			this.LogTrace()?.Trace($"CompositionTarget#{GetHashCode()} {nameof(_renderRequested)} = {_renderRequested}");
		}
	}

	void ICompositionTarget.RequestNewFrame()
	{
		var shouldEnqueue = false;
		lock (_renderingStateGate)
		{
			LogRenderState();
			AssertRenderStateMachine();
			if (!_renderedAheadOfTime && !RenderRequested)
			{
				RenderRequested = true;
				shouldEnqueue = true;
			}
			else if (_renderedAheadOfTime)
			{
				_renderRequestedAfterAheadOfTimePaint = true;
			}
			AssertRenderStateMachine();
			LogRenderState();
		}

		if (shouldEnqueue)
		{
			if (ContentRoot.XamlRoot is { } xamlRoot && XamlRootMap.GetHostForRoot(xamlRoot) is { } host)
			{
				host.InvalidateRender();
			}
			this.LogTrace()?.Trace($"CompositionTarget#{GetHashCode()}: {nameof(ICompositionTarget.RequestNewFrame)} invalidated render");
		}
		else
		{
			this.LogTrace()?.Trace($"CompositionTarget#{GetHashCode()}: {nameof(ICompositionTarget.RequestNewFrame)} found no need to invalidate render.");
		}
	}

	private void EnqueueRenderCallback()
	{
		this.LogTrace()?.Trace($"CompositionTarget#{GetHashCode()}: {nameof(EnqueueRenderCallback)}");
		NativeDispatcher.CheckThreadAccess();

		Interlocked.Exchange(ref _shouldEnqueueRenderOnNextNativePlatformFrameRequested, true);

		lock (_renderingStateGate)
		{
			LogRenderState();
			AssertRenderStateMachine();
			if (_renderedAheadOfTime)
			{
				_renderedAheadOfTime = false;
				if (_renderRequestedAfterAheadOfTimePaint)
				{
					_renderRequestedAfterAheadOfTimePaint = false;
					this.LogTrace()?.Trace($"CompositionTarget#{GetHashCode()}: {nameof(EnqueueRenderCallback)}: rendered ahead of time and got a new frame request since. Doing nothing this tick and rescheduling another tick");
					((ICompositionTarget)this).RequestNewFrame();
				}
				else
				{
					this.LogTrace()?.Trace($"{nameof(EnqueueRenderCallback)}: rendered ahead of time and no new frame was requested since.");
				}
			}
			else if (RenderRequested)
			{
				lock (_renderingStateGate)
				{
					RenderRequested = false;
				}
				this.LogTrace()?.Trace($"CompositionTarget#{GetHashCode()}: {nameof(Draw)} fired from {nameof(EnqueueRenderCallback)}");
				Render();
			}
			AssertRenderStateMachine();
			LogRenderState();
		}
	}

	internal SKPath OnNativePlatformFrameRequested(SKCanvas? canvas, Func<Size, SKCanvas> resizeFunc)
	{
		this.LogTrace()?.Trace($"CompositionTarget#{GetHashCode()}: {nameof(OnNativePlatformFrameRequested)}");

		if (Interlocked.Exchange(ref _shouldEnqueueRenderOnNextNativePlatformFrameRequested, false))
		{
			NativeDispatcher.Main.EnqueueRender(this, EnqueueRenderCallback);
		}

		return Draw(canvas, resizeFunc);
	}

	internal void OnRenderFrameOpportunity()
	{
		// If we get an opportunity to get call Render earlier than EnqueuePaintCallback, then we do that
		// but skip the Render call in the next EnqueuePaintCallback so that overall we're still keeping
		// the rate of Render calls the same.
		NativeDispatcher.CheckThreadAccess();

		if (SkiaRenderHelper.CanRecordPicture(ContentRoot.VisualTree.RootElement))
		{
			var shouldRender = false;
			lock (_renderingStateGate)
			{
				LogRenderState();
				AssertRenderStateMachine();
				if (RenderRequested && !_renderedAheadOfTime)
				{
					RenderRequested = false;
					_renderedAheadOfTime = true;
					shouldRender = true;
				}
				AssertRenderStateMachine();
				LogRenderState();
			}

			if (shouldRender)
			{
				this.LogTrace()?.Trace($"CompositionTarget#{GetHashCode()}: {nameof(OnRenderFrameOpportunity)}: Calling {nameof(Draw)} early ");
				Render();
			}
		}
	}

	[Conditional("DEBUG")]
	private void AssertRenderStateMachine()
	{
		lock (_renderingStateGate)
		{
			Debug.Assert(!_renderRequestedAfterAheadOfTimePaint || _renderedAheadOfTime);
			Debug.Assert(!_renderedAheadOfTime || !RenderRequested);
		}
	}

	private void LogRenderState()
	{
		if (this.Log().IsEnabled(LogLevel.Trace))
		{
			lock (_renderingStateGate)
			{
				this.Log().Trace($"CompositionTarget#{GetHashCode()}: Render state machine: {nameof(_renderRequested)} = {_renderRequested}, {nameof(_renderedAheadOfTime)} = {_renderedAheadOfTime}, {nameof(_renderRequestedAfterAheadOfTimePaint)}={_renderRequestedAfterAheadOfTimePaint}");
			}
		}
	}
}
