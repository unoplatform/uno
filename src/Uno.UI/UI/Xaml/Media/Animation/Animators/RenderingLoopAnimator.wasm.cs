using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.InteropServices.JavaScript;
using System.Threading;
using Uno.Disposables;
using Uno.Foundation;
using Uno.Foundation.Interop;
using Uno.Foundation.Logging;
using Uno.UI.__Resources;
using Windows.UI.Core;
using Windows.System;

namespace Microsoft.UI.Xaml.Media.Animation;

internal abstract class RenderingLoopAnimator<T> : CPUBoundAnimator<T> where T : struct
{
	private bool _isEnabled;
	private IDisposable _frameEvent;
	private DispatcherQueueTimer _delayRequest;

	protected RenderingLoopAnimator(T from, T to)
		: base(from, to)
	{
	}

	protected override void EnableFrameReporting()
	{
		if (this.Log().IsEnabled(LogLevel.Trace))
		{
			this.Log().Trace("EnableFrameReporting");
		}

		if (_isEnabled)
		{
			return;
		}

		_isEnabled = true;

		RegisterFrameEvent();
	}

	protected override void DisableFrameReporting()
	{
		if (this.Log().IsEnabled(LogLevel.Trace))
		{
			this.Log().Trace("DisableFrameReporting");
		}


		_isEnabled = false;
		UnscheduleFrame();
	}

	protected override void SetStartFrameDelay(long delayMs)
	{
		if (this.Log().IsEnabled(LogLevel.Trace))
		{
			this.Log().Trace($"SetStartFrameDelay: {delayMs}");
		}

		UnscheduleFrame();

		if (_isEnabled)
		{
			_delayRequest = new DispatcherQueueTimer()
			{
				Interval = TimeSpan.FromMilliseconds(delayMs),
				IsRepeating = false
			};

			_delayRequest.Tick += (s, e) =>
			{
				_delayRequest = null;

				OnFrame();

				// Restore the render loop now that we have completed the delay
				RegisterFrameEvent();
			};

			_delayRequest.Start();
		}
	}

	protected override void SetAnimationFramesInterval()
	{
		if (this.Log().IsEnabled(LogLevel.Trace))
		{
			this.Log().Trace("SetAnimationFramesInterval");
		}

		UnscheduleFrame();

		if (_isEnabled)
		{
			OnFrame();
		}
	}

	private void UnscheduleFrame()
	{
		if (_delayRequest != null)
		{
			_delayRequest.Stop();
			_delayRequest = null;
		}

		_frameEvent?.Dispose();
		_frameEvent = null;
	}

	private void RegisterFrameEvent()
	{
		_frameEvent?.Dispose();
		_frameEvent = RenderingLoopAnimator.RegisterFrameEvent(OnFrame);
	}

	private void OnFrame()
		=> OnFrame(null, null);
}
